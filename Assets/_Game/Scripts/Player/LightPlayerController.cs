using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class LightPlayerController : PlayerController
{
    private const string MoveActionName = "Move";
    private const string PauseActionName = "Pause";
    private const string ShowMyPlayerActionName = "ShowMyPlayer";

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Movement _movement;
    private PlayerInput _playerInput;
    private Light2D _activeLight;
    public Movement Movement => _movement;

    [Tooltip("Movement speed in units per second")]
    [SerializeField]
    protected float _speed = 10f;

    [Tooltip("How close to .5 before applying queued dir")]
    [SerializeField]
    private float _centerThreshold = 0.15f;

    [Tooltip("How fast to snap to center when switching axis (multiplier of normal speed)")]
    [SerializeField]
    private float _snapSpeedMultiplier = 1.5f;

    [SerializeField]
    private Animator _animator;

    private const string AnimatorBoolIsMovingBottom = "IsMovingBottom";
    private const string AnimatorBoolIsMovingUp = "IsMovingUp";
    private const string AnimatorBoolIsMovingRight = "IsMovingRight";
    private const string AnimatorBoolIsMovingLeft = "IsMovingLeft";
    private const string AnimatorBoolIsDeath = "IsDeath";
    private const string AnimatorBoolIsWin = "IsWin";

    public float Speed => _speed;

    private Grid _grid;
    private bool _isRoundStarted = false;

    private Vector2Int _currentDirection = Vector2Int.up;
    public Vector2Int CurrentDirection
    {
        get => _currentDirection;
        private set => _currentDirection = value;
    }

    protected virtual void Awake()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _grid = GameManager.Instance.Grid;
        _animator = GetComponentInParent<Animator>();
        _movement = new Movement(
            _rb,
            transform,
            _grid,
            _speed,
            _centerThreshold,
            _snapSpeedMultiplier,
            GameManager.Instance.EasyMovementEnabled
        );

        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput)
        {
            InputAction moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed += OnMove;

            InputAction pauseAction = _playerInput.actions[PauseActionName];
            pauseAction.performed += OnPause;

            _playerInput.actions[ShowMyPlayerActionName].performed += OnShowMyPlayer;
        }

        if (GameManager.Instance)
        {
            GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnded += Timer_OnRoundEnd;

            GameManager.Instance.Timer.OnMatchPause += Light_OnMatchPause;
            GameManager.Instance.Timer.OnMatchResume += Light_OnMatchResume;

            GameManager.Instance.PlayerManager.OnPlayerScoreChanged += (player, points) =>
            {
                Debug.Log(
                    $"{player.Role} zdoby� {points} punkt�w! Aktualny wynik: {player.PlayerScore.TotalScore}"
                );
            };

            GameManager.Instance.GridManager.OnGridChanged += GridManager_OnGridChanged;
        }
    }

    private void GridManager_OnGridChanged(int obj)
    {
        _movement = null;

        _grid = GameManager.Instance.Grid;
        _animator = GetComponentInParent<Animator>();

        _movement = new Movement(
            _rb,
            transform,
            _grid,
            _speed,
            _centerThreshold,
            _snapSpeedMultiplier,
            GameManager.Instance.EasyMovementEnabled
        );
    }

    protected override void Start()
    {
        base.Start();

        Color color;
        ColorUtility.TryParseHtmlString("#FFFF2A", out color);

        _playerNameObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = color;
    }

    private void Light_OnMatchResume(object sender, EventArgs e)
    {
        _movement.IsLocked = false;
    }

    private void Light_OnMatchPause(object sender, EventArgs e)
    {
        _movement.IsLocked = true;
    }

    private void Timer_OnRoundEnd(object sender, EventArgs e)
    {
        _animator.SetBool("IsDeath", false);
        _animator.SetBool(AnimatorBoolIsWin, false);
        _isRoundStarted = false;
    }

    private void Timer_OnRoundStart(object sender, EventArgs e)
    {
        _isRoundStarted = true;
    }

    public virtual void OnMove(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted)
            return;

        if (context.performed)
        {
            _moveInput = context.ReadValue<Vector2>();

            if (_moveInput.magnitude > 0.1f)
            {
                if (Mathf.Abs(_moveInput.x) > Mathf.Abs(_moveInput.y))
                    CurrentDirection = _moveInput.x > 0 ? Vector2Int.right : Vector2Int.left;
                else
                    CurrentDirection = _moveInput.y > 0 ? Vector2Int.up : Vector2Int.down;
            }

            _movement.OnMove(_moveInput);
        }
    }

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

    protected virtual void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.Timer.IsGamePaused)
        {
            return;
        }

        _movement.FixedTick();
        UpdateAnimation();

        // NOTE: Marking tiles as active (potential issue)
        // If the player skips over a tile, it won't activate it.
        // This situation should not happen with proper movement speed and tile size.
        // Event at low FPS this SHOULD be fine.
        GameManager.Instance.Grid.SetTileToLight(Grid.WorldToCell(transform.position));
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            var moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed -= OnMove;

            _playerInput.actions[ShowMyPlayerActionName].performed -= OnShowMyPlayer;
        }

        if (GameManager.Instance)
        {
            GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnded -= Timer_OnRoundEnd;
            GameManager.Instance.Timer.OnMatchPause -= Light_OnMatchPause;
            GameManager.Instance.Timer.OnMatchResume -= Light_OnMatchResume;

            GameManager.Instance.GridManager.OnGridChanged -= GridManager_OnGridChanged;
        }
    }

    private void UpdateAnimation()
    {
        bool isMoving = _moveInput.sqrMagnitude > 0.1f;
        if (!isMoving)
        {
            _animator.SetBool(AnimatorBoolIsMovingUp, false);
            _animator.SetBool(AnimatorBoolIsMovingBottom, false);
            _animator.SetBool(AnimatorBoolIsMovingLeft, false);
            _animator.SetBool(AnimatorBoolIsMovingRight, false);
            return;
        }

        // Reset
        _animator.SetBool(AnimatorBoolIsMovingUp, false);
        _animator.SetBool(AnimatorBoolIsMovingBottom, false);
        _animator.SetBool(AnimatorBoolIsMovingLeft, false);
        _animator.SetBool(AnimatorBoolIsMovingRight, false);

        switch (CurrentDirection)
        {
            case var d when d == Vector2Int.up:
                _animator.SetBool(AnimatorBoolIsMovingUp, true);
                break;

            case var d when d == Vector2Int.down:
                _animator.SetBool(AnimatorBoolIsMovingBottom, true);
                break;

            case var d when d == Vector2Int.left:
                _animator.SetBool(AnimatorBoolIsMovingLeft, true);
                break;

            case var d when d == Vector2Int.right:
                _animator.SetBool(AnimatorBoolIsMovingRight, true);
                break;
        }
    }

    public void FlashLight(float maxIntensity = 4f, float duration = 0.25f)
    {
        if (_activeLight == null)
            return;
        StopAllCoroutines();
        StartCoroutine(FlashLightCoroutine(maxIntensity, duration));
    }

    private IEnumerator FlashLightCoroutine(float maxIntensity, float duration)
    {
        float halfDuration = duration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            _activeLight.intensity = Mathf.Lerp(0f, maxIntensity, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            _activeLight.intensity = Mathf.Lerp(maxIntensity, 0f, elapsed / halfDuration);
            yield return null;
        }

        _activeLight.intensity = 0f;
    }

    public void SetActiveLight(Light2D light)
    {
        _activeLight = light;
    }

    private void OnShowMyPlayer(InputAction.CallbackContext context)
    {
        FlashLight();
    }

    public void HandleRoundLose()
    {
        _movement.IsLocked = true;
        StartCoroutine(HandleRoundLoseRoutine());
    }

    public void HandleRoundWin()
    {
        _movement.IsLocked = true;
        StartCoroutine(HandlRoundWinRoutine());
    }

    private IEnumerator HandleRoundLoseRoutine()
    {
        Vector3 ownerPosition = transform.position;
        var zone = GameManager.Instance.CameraManager.GetZoneByPosition(ownerPosition);

        if(zone != CameraManager.CameraZonesName.None)
        {
            GameManager.Instance.CameraManager.ZoomIn(zone);
        } 

        yield return new WaitForSeconds(1f);
        _animator.SetBool(AnimatorBoolIsDeath, true);
    }

    private IEnumerator HandlRoundWinRoutine()
    {
        Vector3 ownerPosition = transform.position;
        var zone = GameManager.Instance.CameraManager.GetZoneByPosition(ownerPosition);

        if (zone != CameraManager.CameraZonesName.None)
        {
            GameManager.Instance.CameraManager.ZoomIn(zone);
        }

        yield return new WaitForSeconds(1f);
        _animator.SetBool(AnimatorBoolIsWin, true);
    }
}
