using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Handles the player's control over multiple shadows/ghosts.
/// Manages input, spawning, switching between shadows, round events, and appearance updates.
/// </summary>
public class ShadowPlayerController : PlayerController
{
    #region Inspector Fields
    [Header("Shadow Settings")]
    [SerializeField]
    private GameObject _shadowPrefab;

    [SerializeField]
    private int _shadowCount = 3;

    [SerializeField]
    private float _spawnDelay = 1f;

    [SerializeField]
    private Material _laserMaterial;
    #endregion

    #region Private Fields
    private const string MoveActionName = "Move";
    private const string SwitchClydeActionName = "SwitchClyde";
    private const string SwitchInkyActionName = "SwitchInky";
    private const string PauseActionName = "Pause";
    private const string ShowMyPlayerActionName = "ShowMyPlayer";

    private List<ShadowType> _roundShadowTypes;
    private readonly List<GameObject> _shadows = new();
    private readonly List<GameObject> _nextActiveCharacters = new();
    private PlayerInput _playerInput;
    private int _activeShadowIndex;
    private bool _canSwitch = true;
    private bool _isRoundStarted = false;

    [SerializeField]
    private Color _aSwitchColor;
    #endregion

    #region Events
    public event System.Action OnLBSwitchEvent;
    public event System.Action OnRBSwitchEvent;
    public event System.Action OnActiveRandomSwitch;
    #endregion

    #region Public Fields
    public IReadOnlyList<GameObject> Shadows => _shadows;
    public Color ASwitchColor => _aSwitchColor;
    public bool CanMove = true;
    #endregion

    #region VFX Cleanup
    private readonly List<GameObject> _roundVfxObjects = new();
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes player input, subscribes to timer events, and starts spawning shadows.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        Color color;
        ColorUtility.TryParseHtmlString("#FF2222", out color);

        _playerNameObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = color;

        _playerInput = GetComponentInParent<PlayerInput>();

        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed += OnMove;
            _playerInput.actions[PauseActionName].performed += OnPause;

            _playerInput.actions[SwitchClydeActionName].performed += OnLBSwitch;
            _playerInput.actions[SwitchInkyActionName].performed += OnRBSwitch;

            _playerInput.actions[ShowMyPlayerActionName].performed += OnShowMyPlayer;
        }

        GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnded += HandleRoundEnd;

        GameManager.Instance.Timer.OnMatchPause += Shadow_OnMatchPause;
        GameManager.Instance.Timer.OnMatchResume += Shadow_OnMatchResume;

        PrepareRoundShadowTypes();

        StartCoroutine(SpawnFirstShadow());
    }

    /// Handles cleanup and unsubscribes from events.
    /// </summary>
    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed -= OnMove;

            _playerInput.actions[SwitchClydeActionName].performed -= OnLBSwitch;
            _playerInput.actions[SwitchInkyActionName].performed -= OnRBSwitch;

            _playerInput.actions[ShowMyPlayerActionName].performed -= OnShowMyPlayer;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnded -= HandleRoundEnd;

            GameManager.Instance.Timer.OnMatchPause -= Shadow_OnMatchPause;
            GameManager.Instance.Timer.OnMatchResume -= Shadow_OnMatchResume;
        }
    }

    private void Update()
    {
        UpdateInspectorDebug();
    }
    #endregion

    #region Input Handling
    /// <summary>
    /// Processes movement input and moves the currently active shadow.
    /// </summary>
    private void OnMove(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted)
            return;
        if (_shadows.Count == 0)
            return;
        if (AreAllShadowsEaten())
            return;
        if(!CanMove) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        var activeGhost = _shadows[_activeShadowIndex];
        activeGhost.GetComponent<ShadowController>().Move(moveInput);
    }

    /// <summary>
    /// Returns true if all shadows have left the base (none are in ShadowExitBaseState).
    /// </summary>
    private bool HaveShadowsLeftBase()
    {
        foreach (var shadow in _shadows)
        {
            var controller = shadow.GetComponent<ShadowController>();
            if (controller.CurrentState is ShadowExitBaseState)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Switches control to the previous available shadow in the cycle (to the left).
    /// </summary>
    private void OnLBSwitch(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted || _shadows.Count == 0)
            return;
        if (AreAllShadowsEaten())
            return;
        if (!_canSwitch)
            return;
        if (!HaveShadowsLeftBase())
            return;

        OnLBSwitchEvent?.Invoke();

        SwitchLeft();
    }

    /// <summary>
    /// Switches control to the next available shadow in the cycle (to the right).
    /// </summary>
    private void OnRBSwitch(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted || _shadows.Count == 0)
            return;
        if (AreAllShadowsEaten())
            return;
        if (!_canSwitch)
            return;
        if (!HaveShadowsLeftBase())
            return;

        OnRBSwitchEvent?.Invoke();

        SwitchRight();
    }

    /// <summary>
    /// Triggers a visual highlight on the currently active shadow,
    /// making it easier for the player to locate their controlled character.
    /// </summary>
    private void OnShowMyPlayer(InputAction.CallbackContext context)
    {
        ShadowAppearanceManager shadowAppearanceManager = _shadows[_activeShadowIndex]
            .GetComponent<ShadowAppearanceManager>();

        shadowAppearanceManager.FlashLight();
    }
    #endregion

    #region Shadow Spawning
    /// <summary>
    /// Spawns the first shadow and sets it active.
    /// </summary>
    private IEnumerator SpawnFirstShadow()
    {
        var spawnPosition = new Vector3(-0.5f, -1.5f, 0f);
        var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
        ghost.transform.SetParent(transform);
        ghost.name = "Ghost_1";

        var controller = ghost.GetComponent<ShadowController>();
        controller.Owner = this;
        controller.SetShadowType(_roundShadowTypes[0]);
        controller.IsShadowActive = true;
        // Enable easy movement for active ghost
        controller.Movement.SetEasyMovement(true);

        ShadowAppearanceManager appearanceManager =
            controller.GetComponent<ShadowAppearanceManager>();

        appearanceManager.colorBeforeFrightened = appearanceManager.blinkyColor;
        appearanceManager.SetCustomColor(appearanceManager.blinkyColor);

        _shadows.Add(ghost);
        _nextActiveCharacters.Add(ghost);
        _activeShadowIndex = 0;
        UpdateASwitchColor();

        yield return new WaitUntil(() => _isRoundStarted);

        controller.SetState(new ShadowActiveState());

        StartCoroutine(SpawnRemainingShadows());
    }

    /// <summary>
    /// Spawns remaining shadows after the first with delay.
    /// </summary>
    private IEnumerator SpawnRemainingShadows()
    {
        for (int i = 1; i < _shadowCount; i++)
        {
            var spawnPosition = new Vector3(-0.5f, -0.5f, 0f);
            var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
            ghost.transform.SetParent(transform);
            ghost.name = $"Ghost_{i + 1}";

            var controller = ghost.GetComponent<ShadowController>();
            controller.Owner = this;
            controller.SetShadowType(_roundShadowTypes[i]);
            controller.IsShadowActive = false;
            // Disable easy movement for newly spawned ghosts (AI managed) until they become active
            controller.Movement.SetEasyMovement(false);
            controller.SetState(new ShadowExitBaseState());

            ShadowAppearanceManager appearanceManager =
                controller.GetComponent<ShadowAppearanceManager>();
            ShadowType type = controller.Type;

            if (i == 1)
            {
                appearanceManager.colorBeforeFrightened = appearanceManager.inkyColor;
                appearanceManager.SetCustomColor(appearanceManager.inkyColor);
            }

            if (i == 2)
            {
                appearanceManager.colorBeforeFrightened = appearanceManager.clydeColor;
                appearanceManager.SetCustomColor(appearanceManager.clydeColor);
            }

            _shadows.Add(ghost);
            _nextActiveCharacters.Add(ghost);

            yield return new WaitForSeconds(_spawnDelay);
        }
    }
    #endregion

    #region Pause Handling
    /// <summary>
    /// Handles the pause input from the player. Toggles game pause state and loads/unloads the pause menu scene.
    /// </summary>
    public virtual void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (GameManager.Instance?.Timer != null)
        {
            if (
                GameManager.Instance.Timer.IsGamePaused
                && MenuManager.Instance.IsLastMenuOfType(MenuManager.Menu.Pause)
            )
            {
                GameManager.Instance.Timer.Resume();
                MenuManager.Instance.CloseLastSubMenu();
            }
            else if (
                !GameManager.Instance.Timer.IsGamePaused
                && !MenuManager.Instance.IsLastMenuOfType(MenuManager.Menu.Pause)
            )
            {
                GameManager.Instance.Timer.Pause();
                MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Pause);
            }
        }
    }

    /// <summary>
    /// Called when the match is resumed. Unlocks movement for all shadow objects.
    /// </summary>
    private void Shadow_OnMatchResume(object sender, System.EventArgs e)
    {
        _shadows.ForEach(shadow =>
        {
            shadow.GetComponent<ShadowController>().Movement.IsLocked = false;
        });
    }

    /// <summary>
    /// Called when the match is paused. Locks movement for all shadow objects.
    /// </summary>
    private void Shadow_OnMatchPause(object sender, System.EventArgs e)
    {
        _shadows.ForEach(shadow =>
        {
            shadow.GetComponent<ShadowController>().Movement.IsLocked = true;
        });
    }

    /// <summary>
    /// Called when a new round starts. Flags the round as started, enabling shadow input and behavior.
    /// </summary>
    private void Timer_OnRoundStart(object sender, System.EventArgs e)
    {
        CanMove = true;
        _isRoundStarted = true;
    }
    #endregion

    #region Switching Logic
    /// <summary>
    /// Attempts to switch control to the next valid shadow in the right (forward) direction.
    /// </summary>
    private void SwitchRight()
    {
        int nextIndex = GetNextValidIndex(+1);
        if (nextIndex != -1)
            StartCoroutine(SwitchToIndexCoroutine(nextIndex));
    }

    /// <summary>
    /// Attempts to switch control to the next valid shadow in the left (backward) direction.
    /// </summary>
    private void SwitchLeft()
    {
        int nextIndex = GetNextValidIndex(-1);
        if (nextIndex != -1)
            StartCoroutine(SwitchToIndexCoroutine(nextIndex));
    }

    /// <summary>
    /// Finds the next valid shadow index in the specified direction, skipping any that are "Eaten".
    /// </summary>
    /// <param name="direction">+1 to search forward/right, -1 to search backward/left.</param>
    /// <returns>The index of the next valid shadow, or -1 if none are available.</returns>
    private int GetNextValidIndex(int direction)
    {
        if (_shadows.Count <= 1)
            return -1;

        int index = _activeShadowIndex;

        for (int i = 0; i < _shadows.Count; i++)
        {
            index = (index + direction + _shadows.Count) % _shadows.Count;

            var controller = _shadows[index].GetComponent<ShadowController>();
            if (controller.CurrentState.State != ShadowState.Eaten)
                return index;
        }

        return -1;
    }

    /// <summary>
    /// Coroutine that handles switching control to a shadow at a given index.
    /// Updates states, activates the new shadow, and triggers switch particles.
    /// </summary>
    private IEnumerator SwitchToIndexCoroutine(int newIndex)
    {
        _canSwitch = false;

        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.EnemySwap);

        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var old = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        old.IsShadowActive = false;
        // Disable easy movement for old shadow since it's no longer player controlled
        old.Movement.SetEasyMovement(false);
        old.SetState(isFrightened ? new ShadowFrightenedState() : new ShadowScatterState());

        _activeShadowIndex = newIndex;
        UpdateASwitchColor();

        var next = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        next.IsShadowActive = true;
        // Enable easy movement for new shadow since it's now player controlled
        next.Movement.SetEasyMovement(true);
        next.SetState(new ShadowActiveState());

        StartCoroutine(PlaySwitchLaser(old, next));

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }

    /// <summary>
    /// Plays a visual laser effect between two shadows to indicate switching.
    /// Creates a temporary LineRenderer and 2D light, animates the laser with a small wiggle,
    /// and cleans up the temporary objects after the effect completes.
    /// </summary>
    /// <param name="from">The shadow that the laser starts from.</param>
    /// <param name="to">The shadow that the laser points to.</param>
    private IEnumerator PlaySwitchLaser(ShadowController from, ShadowController to)
    {
        GameObject laserObj = new GameObject("SwitchLaser");
        _roundVfxObjects.Add(laserObj);
        laserObj.transform.position = from.transform.position;
        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        lr.sortingLayerName = "Player";

        lr.positionCount = 2;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;

        lr.material = _laserMaterial;
        lr.material.color = Color.softRed;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.softRed, 0f),
                new GradientColorKey(Color.softRed, 1f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        lr.colorGradient = gradient;

        Vector3 startPos = from.transform.position;
        Vector3 endPos = to.transform.position;

        GameObject lightObj = new GameObject("Laser2DLight");
        _roundVfxObjects.Add(lightObj);
        lightObj.transform.position = startPos;
        Light2D light2D = lightObj.AddComponent<Light2D>();
        light2D.lightType = Light2D.LightType.Point;
        light2D.color = Color.softRed;
        light2D.intensity = 12f;
        light2D.pointLightOuterRadius = 1f;

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 wiggle = new Vector3(
                Mathf.Sin(t * 20f) * 0.02f,
                Mathf.Cos(t * 25f) * 0.02f,
                0f
            );

            lr.SetPosition(1, Vector3.Lerp(startPos, endPos, t) + wiggle);
            lr.SetPosition(0, Vector3.Lerp(startPos, endPos, t * 0.5f));

            if (light2D != null)
                light2D.transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        lr.SetPosition(0, endPos);
        lr.SetPosition(1, endPos);

        yield return new WaitForSeconds(0.05f);

        CleanupVfxObject(laserObj);
        CleanupVfxObject(lightObj);
    }

    /// <summary>
    /// Safely removes a temporary VFX GameObject from tracking and destroys it.
    /// </summary>
    /// <param name="obj">The GameObject to clean up.</param>
    private void CleanupVfxObject(GameObject obj)
    {
        if (obj == null)
            return;

        _roundVfxObjects.Remove(obj);
        Destroy(obj);
    }

    /// <summary>
    /// Randomly switches control to another shadow. Ensures it is not the current active shadow
    /// and that the shadow is not in the 'Eaten' state.
    /// </summary>
    public IEnumerator SwitchShadowsRandomCoroutine()
    {
        yield return null;
        if (AreAllShadowsEaten())
            yield break;

        Debug.LogWarning("SwitchShadowsRandomCoroutine");

        _canSwitch = false;

        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.EnemySwap);

        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var oldController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        oldController.IsShadowActive = false;
        // Disable easy movement for old shadow since it's no longer player controlled
        oldController.Movement.SetEasyMovement(false);

        int randomIndex = _activeShadowIndex;
        if (_shadows.Count > 1)
        {
            int attempts = 0;
            do
            {
                randomIndex = Random.Range(0, _shadows.Count);
                attempts++;
                if (attempts > 10)
                    break;
            } while (
                randomIndex == _activeShadowIndex
                || _shadows[randomIndex].GetComponent<ShadowController>().CurrentState.State
                    == ShadowState.Eaten
            );
        }

        _activeShadowIndex = randomIndex;

        UpdateASwitchColor();

        var newController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newController.IsShadowActive = true;
        // Enable easy movement for new shadow since it's now player controlled
        newController.Movement.SetEasyMovement(true);
        newController.SetState(new ShadowActiveState());

        ShadowAppearanceManager shadowAppearanceManager =
            newController.GetComponent<ShadowAppearanceManager>();
        shadowAppearanceManager.SetActive();

        OnActiveRandomSwitch?.Invoke();

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }
    #endregion

    #region Round Reset
    /// <summary>
    /// Handles the end of the round by resetting the shadow system:
    /// stops coroutines, clears active particles, destroys existing shadows,
    /// resets indices and flags, and starts respawning shadows after a short delay.
    /// </summary>
    private void HandleRoundEnd(object sender, System.EventArgs e)
    {
        StopAllCoroutines();
        _canSwitch = true;
        _isRoundStarted = false;
        GameManager.Instance.IsFrightenedShadowState = false;

        foreach (var vfx in _roundVfxObjects)
        {
            if (vfx != null)
                Destroy(vfx);
        }

        _roundVfxObjects.Clear();

        foreach (var ghost in _shadows)
        {
            if (ghost != null)
                Destroy(ghost);
        }

        _shadows.Clear();
        _nextActiveCharacters.Clear();
        _activeShadowIndex = 0;
        UpdateASwitchColor();

        PrepareRoundShadowTypes();
        StartCoroutine(RespawnAfterDelay(0.25f));
    }

    /// <summary>
    /// Coroutine that waits for a specified delay and then starts spawning the first shadow.
    /// </summary>
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnFirstShadow());
    }

    #endregion

    #region Getters
    /// <summary>
    /// Returns the currently active shadow's controller.
    /// </summary>
    public ShadowController GetActiveGhostController()
    {
        var activeGhost = _shadows[_activeShadowIndex];
        return activeGhost.GetComponent<ShadowController>();
    }
    #endregion

    #region Setters
    /// <summary>
    /// Sets the material used for the switch laser effect.
    /// </summary>
    /// <param name="material">The Material to assign to the laser.</param>
    public void SetLaserMaterial(Material material)
    {
        _laserMaterial = material;
    }

    /// <summary>
    /// Sets the prefab to use when spawning new shadows.
    /// </summary>
    /// <param name="gameObject">The shadow prefab GameObject.</param>
    public void SetShadowPrefab(GameObject gameObject)
    {
        _shadowPrefab = gameObject;
    }

    /// <summary>
    /// Updates the color used to indicate the currently active shadow in the switch UI.
    /// Retrieves the color from the ShadowAppearanceManager of the active shadow.
    /// </summary>
    private void UpdateASwitchColor()
    {
        if (_shadows.Count == 0)
            return;

        var appearance = _shadows[_activeShadowIndex].GetComponent<ShadowAppearanceManager>();

        _aSwitchColor = appearance.GetCurrentColor();
    }
    #endregion

    #region Inspector Debug

    [System.Serializable]
    public class ShadowDebugInfo
    {
        public string name;
        public ShadowType type;
        public ShadowState state;
        public bool isActive;
        public Color color;
    }

    [Header("Debug Info (Inspector)")]
    [SerializeField]
    private bool _enableInspectorDebug = true;

    [SerializeField]
    private List<ShadowDebugInfo> _shadowDebugInfos = new();

    /// <summary>
    /// Update inspector debug info
    /// </summary>
    private void UpdateInspectorDebug()
    {
        if (_shadows.Count <= 1)
            return;

        if (!_enableInspectorDebug)
            return;

        _shadowDebugInfos.Clear();

        for (int i = 0; i < _shadows.Count; i++)
        {
            var shadowObj = _shadows[i];
            if (shadowObj == null)
                continue;

            var controller = shadowObj.GetComponent<ShadowController>();
            var appearance = shadowObj.GetComponent<ShadowAppearanceManager>();

            _shadowDebugInfos.Add(
                new ShadowDebugInfo
                {
                    name = shadowObj.name,
                    type = controller.Type,
                    state = controller.CurrentState.State,
                    isActive = controller.IsShadowActive,
                    color = appearance?.GetCurrentColor() ?? Color.white,
                }
            );
        }
    }

    #endregion

    /// <summary>
    /// Generates a randomized list of unique shadow types for the current round.
    /// </summary>
    private void PrepareRoundShadowTypes()
    {
        List<ShadowType> availableTypes = new List<ShadowType>
        {
            ShadowType.Blinky,
            ShadowType.Inky,
            ShadowType.Clyde,
            ShadowType.Pinky,
        };

        for (int i = 0; i < availableTypes.Count; i++)
        {
            int randomIndex = Random.Range(i, availableTypes.Count);
            (availableTypes[i], availableTypes[randomIndex]) = (
                availableTypes[randomIndex],
                availableTypes[i]
            );
        }

        _roundShadowTypes = availableTypes.GetRange(0, _shadowCount);
    }

    public bool AreAllShadowsEaten()
    {
        foreach (var shadow in _shadows)
        {
            if (shadow == null)
                continue;
            var controller = shadow.GetComponent<ShadowController>();
            if (controller.CurrentState.State != ShadowState.Eaten)
                return false;
        }
        return true;
    }

    public void SetActiveShadow(ShadowController shadow)
    {
        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var old = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        old.IsShadowActive = false;
        // Disable easy movement for old shadow since it's no longer player controlled
        old.Movement.SetEasyMovement(false);

        _activeShadowIndex = _shadows.IndexOf(shadow.gameObject);
        UpdateASwitchColor();

        var next = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        next.IsShadowActive = true;
        // Enable easy movement for new shadow since it's now player controlled
        next.Movement.SetEasyMovement(true);
        next.SetState(new ShadowActiveState());

        OnActiveRandomSwitch?.Invoke();
    }
}
