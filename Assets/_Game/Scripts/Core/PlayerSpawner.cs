using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.Universal;

public class PlayerSpawner : MonoBehaviour
{
    #region Events
    public class RoleSelectionEventArgs : EventArgs
    {
        public PlayerManager.PlayerIndex PlayerIndex;
        public PlayerManager.PlayerRole PlayerRole;
        public float HoldProgress; // 0 to 1 inclusive
        public bool IsConfirmed;
        public bool IsReady;
    }

    public class PlayerNameChangeEventArgs : EventArgs
    {
        public PlayerManager.PlayerIndex PlayerIndex;
        public string NewName;
    }

    public class ControllerTypeDetectedEventArgs : EventArgs
    {
        public PlayerManager.PlayerIndex PlayerIndex;
        public ControllerType ControllerType;
    }

    public class PlayerNameConflictEventArgs : EventArgs
    {
        public PlayerManager.PlayerIndex PlayerIndex;
    }

    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionStarted;
    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionChanged;
    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionReleased;
    public static event EventHandler<PlayerNameConflictEventArgs> OnPlayerNameConflict;
    public static event EventHandler OnPlayersReadyToSpawn;

    public static event EventHandler<PlayerNameChangeEventArgs> OnPlayerNameChanged;
    public static event EventHandler<ControllerTypeDetectedEventArgs> OnControllerTypeDetected;
    #endregion

    #region Fields
    [Header("References")]
    [SerializeField]
    private PlayerInputManager _playerInputManager;

    [SerializeField]
    GridManager _gridManager;

    private Grid _grid;

    [Header("Prefabs & Positions")]
    [SerializeField]
    private GameObject _ghostPrefab;

    [SerializeField]
    private GameObject _showLightPlayerPrefab;

    [SerializeField]
    private RuntimeAnimatorController animationController;

    [SerializeField]
    private Material _laserMaterial;

    [Header("Role Selection Settings")]
    [SerializeField]
    private float _holdDuration = 1f;

    [Tooltip("Cooldown between confirmation attempts in seconds (should match shake duration)")]
    [SerializeField]
    private float _confirmationCooldown = 0.5f;

    [Header("D-Pad Input Settings")]
    [SerializeField]
    [Tooltip("Initial delay before first repeat (seconds)")]
    private float _dpadInitialDelay = 0.25f;

    [SerializeField]
    [Tooltip("Starting repeat interval when held (seconds)")]
    private float _dpadInitialInterval = 0.2f;

    [SerializeField]
    [Tooltip("Minimum repeat interval at max speed (seconds)")]
    private float _dpadMinInterval = 0.25f;

    [SerializeField]
    [Tooltip("Time to reach max speed (seconds)")]
    private float _dpadAccelerationTime = 1.0f;

    public static readonly Dictionary<PlayerManager.PlayerRole, Color> RoleColors = new Dictionary<
        PlayerManager.PlayerRole,
        Color
    >
    {
        { PlayerManager.PlayerRole.Light, Color.yellow },
        { PlayerManager.PlayerRole.Skull, Color.red },
    };

    public enum ControllerType
    {
        PlayStation,
        Xbox,
        Unknown,
    }

    private class JoinRequest
    {
        public Gamepad Gamepad;
        public PlayerManager.PlayerRole SelectedRole;
        public float HoldTime;
        public bool IsConfirmed;
        public bool IsReady = false;
        public PlayerManager.PlayerIndex PlayerIndex;
        public string PlayerName = "";
        public ControllerType ControllerType = ControllerType.Unknown;
        public float LastConfirmAttemptTime = -999f;
    }

    private class DPadInputState
    {
        public Gamepad Gamepad;
        public bool WasUpPressed;
        public bool WasDownPressed;
        public bool WasLeftPressed;
        public bool WasRightPressed;
        public float UpHoldTime;
        public float DownHoldTime;
        public float UpNextTriggerTime;
        public float DownNextTriggerTime;
    }

    private List<JoinRequest> _activeRequests = new List<JoinRequest>();
    private List<DPadInputState> _dpadStates = new List<DPadInputState>();

    private bool _playersSpawned = false;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (_playerInputManager != null)
        {
            // Disable automatic joining, handle manually
            _playerInputManager.DisableJoining();
        }
    }

    private void Awake()
    {
        _gridManager.OnGridChanged += GridManager_OnGridChanged;
    }

    private void GridManager_OnGridChanged(int obj)
    {
        _grid = _gridManager.GetGridById(obj);
    }

    private void Update()
    {
        if (_playersSpawned)
            return;

        var gamepads = Gamepad.all;

        DetectGamepadInput(gamepads);
        UpdateRoleSelection();
    }
    #endregion

    #region Player Join Logic

    /// <summary>
    /// Initial detection of gamepad input for joining players.
    /// </summary>
    /// <param name="gamepads"></param>
    private void DetectGamepadInput(ReadOnlyArray<Gamepad> gamepads)
    {
        foreach (var gamepad in gamepads)
        {
            float leftTrigger = gamepad.leftTrigger.ReadValue();
            float rightTrigger = gamepad.rightTrigger.ReadValue();

            // Skip if no input detected
            if (
                !(
                    leftTrigger <= 0.5f && rightTrigger <= 0.5f
                    || _activeRequests.Any(r => r.Gamepad == gamepad)
                    || _activeRequests.Count >= 2
                )
            )
                ProcessGamepadJoin(leftTrigger, rightTrigger, gamepad);

            bool confirmName = gamepad.buttonSouth.wasPressedThisFrame;

            // Handle name confirmation (button South - A on Xbox, X on PS5)
            if (confirmName)
            {
                ProcessNameConfirmation(gamepad);
            }

            // Handle D-pad with delay and acceleration
            bool shouldProcessUp = false;
            bool shouldProcessDown = false;
            bool shouldProcessLeft = false;
            bool shouldProcessRight = false;
            ProcessDPadInput(
                gamepad,
                out shouldProcessUp,
                out shouldProcessDown,
                out shouldProcessLeft,
                out shouldProcessRight
            );

            if (shouldProcessUp || shouldProcessDown || shouldProcessLeft || shouldProcessRight)
            {
                ProcessGamepadNameChange(
                    shouldProcessUp,
                    shouldProcessLeft,
                    shouldProcessRight,
                    gamepad
                );
            }
        }
    }

    /// <summary>
    /// Handles D-pad input with initial delay and acceleration.
    /// </summary>
    private void ProcessDPadInput(
        Gamepad gamepad,
        out bool shouldProcessUp,
        out bool shouldProcessDown,
        out bool shouldProcessLeft,
        out bool shouldProcessRight
    )
    {
        shouldProcessUp = false;
        shouldProcessDown = false;
        shouldProcessLeft = false;
        shouldProcessRight = false;

        bool dpadUp = gamepad.dpad.up.isPressed;
        bool dpadDown = gamepad.dpad.down.isPressed;
        bool dpadLeft = gamepad.dpad.left.isPressed;
        bool dpadRight = gamepad.dpad.right.isPressed;

        // Find or create state for this gamepad
        DPadInputState state = _dpadStates.FirstOrDefault(s => s.Gamepad == gamepad);
        if (state == null)
        {
            state = new DPadInputState { Gamepad = gamepad };
            _dpadStates.Add(state);
        }

        float currentTime = Time.time;

        // Handle Up button (cycle character forward)
        if (dpadUp)
        {
            if (!state.WasUpPressed)
            {
                // First press - trigger immediately and set next trigger time
                shouldProcessUp = true;
                state.UpHoldTime = 0f;
                state.UpNextTriggerTime = currentTime + _dpadInitialDelay;
            }
            else
            {
                // Button still held - check if we should trigger based on timing
                state.UpHoldTime += Time.deltaTime;
                if (currentTime >= state.UpNextTriggerTime)
                {
                    shouldProcessUp = true;

                    // Calculate interval based on hold duration (faster as held longer)
                    float t = Mathf.Clamp01(state.UpHoldTime / _dpadAccelerationTime);
                    float currentInterval = Mathf.Lerp(_dpadInitialInterval, _dpadMinInterval, t);
                    state.UpNextTriggerTime = currentTime + currentInterval;
                }
            }
            state.WasUpPressed = true;
        }
        else
        {
            state.WasUpPressed = false;
            state.UpHoldTime = 0f;
        }

        // Handle Down button (cycle character backward)
        if (dpadDown)
        {
            if (!state.WasDownPressed)
            {
                // First press - trigger immediately and set next trigger time
                shouldProcessDown = true;
                state.DownHoldTime = 0f;
                state.DownNextTriggerTime = currentTime + _dpadInitialDelay;
            }
            else
            {
                // Button still held - check if we should trigger based on timing
                state.DownHoldTime += Time.deltaTime;
                if (currentTime >= state.DownNextTriggerTime)
                {
                    shouldProcessDown = true;

                    // Calculate interval based on hold duration (faster as held longer)
                    float t = Mathf.Clamp01(state.DownHoldTime / _dpadAccelerationTime);
                    float currentInterval = Mathf.Lerp(_dpadInitialInterval, _dpadMinInterval, t);
                    state.DownNextTriggerTime = currentTime + currentInterval;
                }
            }
            state.WasDownPressed = true;
        }
        else
        {
            state.WasDownPressed = false;
            state.DownHoldTime = 0f;
        }

        // Handle Left button (remove character)
        if (dpadLeft && !state.WasLeftPressed)
        {
            shouldProcessLeft = true;
            state.WasLeftPressed = true;
        }
        else if (!dpadLeft)
        {
            state.WasLeftPressed = false;
        }

        // Handle Right button (add character)
        if (dpadRight && !state.WasRightPressed)
        {
            shouldProcessRight = true;
            state.WasRightPressed = true;
        }
        else if (!dpadRight)
        {
            state.WasRightPressed = false;
        }
    }

    /// <summary>
    /// Detects the controller type based on the gamepad device name.
    /// </summary>
    private ControllerType DetectControllerType(Gamepad gamepad)
    {
        string deviceName = gamepad.name.ToLower();
        string displayName = gamepad.displayName.ToLower();

        // Check for PlayStation controllers
        if (
            deviceName.Contains("dualshock")
            || deviceName.Contains("dualsense")
            || deviceName.Contains("playstation")
            || deviceName.Contains("ps4")
            || deviceName.Contains("ps5")
            || displayName.Contains("playstation")
            || displayName.Contains("dualshock")
            || displayName.Contains("dualsense")
        )
        {
            Debug.Log($"Detected PlayStation controller: {gamepad.displayName}");
            return ControllerType.PlayStation;
        }

        // Check for Xbox controllers
        if (
            deviceName.Contains("xbox")
            || displayName.Contains("xbox")
            || deviceName.Contains("xinput")
            || displayName.Contains("xinput")
        )
        {
            Debug.Log($"Detected Xbox controller: {gamepad.displayName}");
            return ControllerType.Xbox;
        }

        // Default to PlayStation for unknown controllers
        Debug.Log($"Unknown controller type '{gamepad.displayName}', defaulting to PlayStation");
        return ControllerType.PlayStation;
    }

    private void ProcessGamepadJoin(float leftTrigger, float rightTrigger, Gamepad gamepad)
    {
        // Determine available player index
        PlayerManager.PlayerIndex? availableIndex = null;

        if (!_activeRequests.Any(r => r.PlayerIndex == PlayerManager.PlayerIndex.P1))
            availableIndex = PlayerManager.PlayerIndex.P1;
        else if (!_activeRequests.Any(r => r.PlayerIndex == PlayerManager.PlayerIndex.P2))
            availableIndex = PlayerManager.PlayerIndex.P2;

        if (availableIndex == null)
            return;

        // Init new join request with gamepad assigned for given player index and empty role
        JoinRequest newRequest = new JoinRequest
        {
            Gamepad = gamepad,
            HoldTime = 0f,
            IsConfirmed = false,
            PlayerIndex = availableIndex.Value,
            SelectedRole = PlayerManager.PlayerRole.None,
            PlayerName = "A",
            ControllerType = DetectControllerType(gamepad),
        };

        if (
            leftTrigger > 0.5f
            && !_activeRequests.Any(r => r.SelectedRole == PlayerManager.PlayerRole.Light)
        )
        {
            // L2 held, light role available -> claim Light role, add request to active list and fire event
            newRequest.SelectedRole = PlayerManager.PlayerRole.Light;
            Debug.Log($"Player {newRequest.PlayerIndex} claimed Light role.");
        }
        else if (
            rightTrigger > 0.5f
            && !_activeRequests.Any(r => r.SelectedRole == PlayerManager.PlayerRole.Skull)
        )
        {
            // R2 held, skull role available -> claim Skull role, add request to active list and fire event
            newRequest.SelectedRole = PlayerManager.PlayerRole.Skull;
            Debug.Log($"Player {newRequest.PlayerIndex} claimed Skull role.");
        }
        else
        {
            // Desired role not available, skip
            Debug.Log($"Player {availableIndex.Value} tried to join but role not available.");
            return;
        }

        _activeRequests.Add(newRequest);
        OnRoleSelectionStarted?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = newRequest.PlayerIndex,
                PlayerRole = newRequest.SelectedRole,
                HoldProgress = Mathf.Clamp(newRequest.HoldTime / _holdDuration, 0f, 1f),
                IsConfirmed = newRequest.IsConfirmed,
                IsReady = newRequest.IsReady,
            }
        );

        // Fire controller type detected event
        OnControllerTypeDetected?.Invoke(
            this,
            new ControllerTypeDetectedEventArgs
            {
                PlayerIndex = newRequest.PlayerIndex,
                ControllerType = newRequest.ControllerType,
            }
        );
    }

    private void ProcessGamepadNameChange(
        bool cycleForward,
        bool removeChar,
        bool addChar,
        Gamepad gamepad
    )
    {
        var request = _activeRequests.FirstOrDefault(r => r.Gamepad == gamepad);
        if (request == null || !request.IsConfirmed || request.IsReady)
            return;

        if (request.PlayerName.Length == 0)
        {
            request.PlayerName = "A";
        }

        string previousName = request.PlayerName;

        // D-pad Up/Down: Cycle current character
        if (cycleForward || (!cycleForward && !removeChar && !addChar))
        {
            string name = request.PlayerName.Remove(request.PlayerName.Length - 1);
            int dir = cycleForward ? 1 : -1;
            request.PlayerName =
                name
                + (char)(
                    ((int)request.PlayerName[request.PlayerName.Length - 1] - 65 + dir + 26) % 26
                    + 65
                ); // Cycle last character
        }
        // D-pad Right: Add character
        else if (addChar && request.PlayerName.Length < 8)
        {
            request.PlayerName += "A";
        }
        // D-pad Left: Remove character
        else if (removeChar && request.PlayerName.Length > 1)
        {
            request.PlayerName = request.PlayerName.Remove(request.PlayerName.Length - 1);
        }

        // Check for nickname duplication
        var otherRequest = _activeRequests.FirstOrDefault(r => r != request && r.IsReady);
        if (
            otherRequest != null
            && request.PlayerName.Equals(
                otherRequest.PlayerName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            // Revert to previous name if it matches the other player's locked nickname
            request.PlayerName = previousName;
            Debug.Log(
                $"Player {request.PlayerIndex} tried to use nickname '{otherRequest.PlayerName}' but it's already taken by Player {otherRequest.PlayerIndex}"
            );
            return;
        }

        OnPlayerNameChanged?.Invoke(
            this,
            new PlayerNameChangeEventArgs
            {
                PlayerIndex = request.PlayerIndex,
                NewName = request.PlayerName,
            }
        );
    }

    private void ProcessNameConfirmation(Gamepad gamepad)
    {
        var request = _activeRequests.FirstOrDefault(r => r.Gamepad == gamepad);
        if (request == null || !request.IsConfirmed || request.IsReady)
            return;

        // Check cooldown to prevent spam
        float timeSinceLastAttempt = Time.time - request.LastConfirmAttemptTime;
        if (timeSinceLastAttempt < _confirmationCooldown)
        {
            return;
        }

        // Update last attempt time
        request.LastConfirmAttemptTime = Time.time;

        // Check for nickname duplication before confirming
        var otherRequest = _activeRequests.FirstOrDefault(r => r != request && r.IsReady);
        if (
            otherRequest != null
            && request.PlayerName.Equals(
                otherRequest.PlayerName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            Debug.Log(
                $"Player {request.PlayerIndex} cannot ready up with nickname '{request.PlayerName}' - already taken by Player {otherRequest.PlayerIndex}"
            );
            OnPlayerNameConflict?.Invoke(
                this,
                new PlayerNameConflictEventArgs { PlayerIndex = request.PlayerIndex }
            );
            return;
        }

        // Set IsReady to true when player confirms their name
        request.IsReady = true;
        Debug.Log($"Player {request.PlayerIndex} confirmed their name: {request.PlayerName}");

        // Fire event to update UI
        OnRoleSelectionChanged?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = request.PlayerIndex,
                PlayerRole = request.SelectedRole,
                HoldProgress = 1f,
                IsConfirmed = request.IsConfirmed,
                IsReady = request.IsReady,
            }
        );
    }

    private void UpdateRoleSelection()
    {
        // Update hold times for each role request
        var removeForPlayer = new List<PlayerManager.PlayerIndex>();

        for (int i = 0; i < _activeRequests.Count; i++)
        {
            // Skip requests that are already confirmed
            if (_activeRequests[i].IsConfirmed)
                continue;

            // If gamepad is null, mark for removal from active requests
            if (_activeRequests[i].Gamepad == null)
            {
                removeForPlayer.Add(_activeRequests[i].PlayerIndex);
                continue;
            }

            float leftTrigger = _activeRequests[i].Gamepad.leftTrigger.ReadValue();
            float rightTrigger = _activeRequests[i].Gamepad.rightTrigger.ReadValue();

            // Check if the player is still holding their claimed role's trigger
            bool isStillHoldingRole = _activeRequests[i].SelectedRole switch
            {
                PlayerManager.PlayerRole.Light => leftTrigger > 0.5f,
                PlayerManager.PlayerRole.Skull => rightTrigger > 0.5f,
                _ => false,
            };

            if (isStillHoldingRole)
            {
                _activeRequests[i].HoldTime += Time.deltaTime;

                if (_activeRequests[i].HoldTime >= _holdDuration && !_activeRequests[i].IsConfirmed)
                {
                    _activeRequests[i].IsConfirmed = true;
                    Debug.Log(
                        $"Player {_activeRequests[i].PlayerIndex} confirmed {_activeRequests[i].SelectedRole} role."
                    );
                    RaiseRoleSelectionChangedEvent(_activeRequests[i]);
                }
                else if (!_activeRequests[i].IsConfirmed)
                {
                    RaiseRoleSelectionChangedEvent(_activeRequests[i]);
                }
            }
            else
            {
                // Trigger was released - fire event and mark for removal
                Debug.Log(
                    $"Player {_activeRequests[i].PlayerIndex} released {_activeRequests[i].SelectedRole} role."
                );
                RaiseRoleSelectionReleasedEvent(_activeRequests[i]);
                removeForPlayer.Add(_activeRequests[i].PlayerIndex);
            }
        }

        // Remove released roles
        foreach (var playerIndex in removeForPlayer)
        {
            _activeRequests.RemoveAll(r => r.PlayerIndex == playerIndex);
        }

        // Check if both roles are claimed and confirmed
        if (_activeRequests.Count == 2)
        {
            bool lightReady = _activeRequests.Any(r =>
                r.SelectedRole == PlayerManager.PlayerRole.Light && r.IsReady
            );
            bool shadowReady = _activeRequests.Any(r =>
                r.SelectedRole == PlayerManager.PlayerRole.Skull && r.IsReady
            );

            if (lightReady && shadowReady)
            {
                _playersSpawned = true;
                Debug.Log("Both players confirmed roles. Ready to spawn.");
                OnPlayersReadyToSpawn?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void RaiseRoleSelectionChangedEvent(JoinRequest request)
    {
        OnRoleSelectionChanged?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = request.PlayerIndex,
                PlayerRole = request.SelectedRole,
                HoldProgress = Mathf.Clamp(request.HoldTime / _holdDuration, 0f, 1f),
                IsConfirmed = request.IsConfirmed,
                IsReady = request.IsReady,
            }
        );
    }

    private void RaiseRoleSelectionReleasedEvent(JoinRequest request)
    {
        OnRoleSelectionReleased?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = request.PlayerIndex,
                PlayerRole = request.SelectedRole,
                HoldProgress = Mathf.Clamp(request.HoldTime / _holdDuration, 0f, 1f),
                IsConfirmed = request.IsConfirmed,
                IsReady = request.IsReady,
            }
        );
    }

    public void ExecutePlayerSpawn()
    {
        // Enable joining and spawn players
        _playerInputManager.EnableJoining();

        foreach (var request in _activeRequests)
        {
            Debug.Log($"Spawning player {request.PlayerIndex} as {request.SelectedRole}");

            // Join the player using PlayerInputManager
            var input = _playerInputManager.JoinPlayer(
                (int)request.PlayerIndex - 1,
                -1,
                null,
                request.Gamepad
            );

            string childName =
                request.SelectedRole == PlayerManager.PlayerRole.Light
                    ? "LightAnchor"
                    : "ShadowAnchor";

            GameObject child = new GameObject(childName);
            child.transform.SetParent(input.transform);
            child.transform.localPosition = Vector3.zero;

            if (request.SelectedRole == PlayerManager.PlayerRole.Light)
            {
                SetupLightPlayer(input, child, request.PlayerIndex, request.PlayerName);
            }
            else if (request.SelectedRole == PlayerManager.PlayerRole.Skull)
            {
                SetupSkullPlayer(input, child, request.PlayerIndex, request.PlayerName);
            }
        }
    }

    private void SetupLightPlayer(
        PlayerInput input,
        GameObject child,
        PlayerManager.PlayerIndex playerIndex,
        string playerName
    )
    {
        input.name = "LightControllerRoot";

        GameObject playerNameObj = input.GetComponentInChildren<Canvas>().gameObject;

        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Light);
        input.gameObject.AddComponent<Animator>();
        input.gameObject.GetComponent<Animator>().runtimeAnimatorController = animationController;

        input.gameObject.tag = "PlayerLight";

        child.AddComponent<LightPlayerController>();

        PlayerController playerController = child.GetComponent<LightPlayerController>();
        if (playerController != null)
        {
            playerController.SetPlayerNameObj(input.transform.GetChild(0).gameObject);
            playerController.SetPlayerNick(playerName);
            playerController.SetPlayerScoreObj(input.transform.GetChild(1).gameObject);
        }

        var collider = child.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;

        if (_showLightPlayerPrefab != null)
        {
            GameObject lightPrefabChild = Instantiate(_showLightPlayerPrefab, child.transform);
            lightPrefabChild.name = _showLightPlayerPrefab.name;

            child
                .GetComponent<LightPlayerController>()
                .SetActiveLight(lightPrefabChild.GetComponent<Light2D>());
        }

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerRole.Light,
            playerIndex,
            input,
            playerName
        );

        Debug.Log($"Light player spawned for {playerIndex}.");
    }

    private void SetupSkullPlayer(
        PlayerInput input,
        GameObject child,
        PlayerManager.PlayerIndex playerIndex,
        string playerName
    )
    {
        input.name = "ShadowControllerRoot";

        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Shadow);

        Destroy(input.GetComponent<Renderer>());
        Destroy(input.GetComponent<CircleCollider2D>());

        input.gameObject.tag = "PlayerShadow";

        child.AddComponent<ShadowAppearanceManager>();
        var shadowController = child.AddComponent<ShadowPlayerController>();

        PlayerController playerController = shadowController;

        if (playerController != null)
        {
            playerController.SetPlayerNameObj(input.transform.GetChild(0).gameObject);
            playerController.SetPlayerNick(playerName);
            playerController.SetPlayerScoreObj(input.transform.GetChild(1).gameObject);
        }

        shadowController.SetShadowPrefab(_ghostPrefab);
        shadowController.SetLaserMaterial(_laserMaterial);

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerRole.Skull,
            playerIndex,
            input,
            playerName
        );

        Debug.Log($"Shadow player spawned for {playerIndex}.");
    }

    public PlayerManager.PlayerRole GetRoleForPlayer(PlayerManager.PlayerIndex index)
    {
        var request = _activeRequests.FirstOrDefault(r => r.PlayerIndex == index);

        return request != null ? request.SelectedRole : PlayerManager.PlayerRole.None;
    }

    #endregion
}
