using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Defines the type of shadow/ghost with their behavior patterns.
/// </summary>
public enum ShadowType
{
    Blinky, // Red - chases the player directly
    Pinky, // Pink - tries to ambush the player from the front
    Inky, // Blue - unpredictable, depends on other ghosts
    Clyde, // Orange - alternates between chasing and retreating
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
/// <summary>
/// Controls the behavior, movement, state, and interactions of a shadow/ghost.
/// Handles AI movement, state transitions, collision with player light, and appearance updates.
/// </summary>
public class ShadowController : MonoBehaviour
{
    #region Inspector Fields
    [Header("Direct Indicator")]
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Animator _faceAnimator;

    [Header("Movement Settings")]
    [SerializeField]
    protected float _speed = 10.5f;

    [SerializeField]
    private float _centerThreshold = 0.15f;

    [SerializeField]
    private float _snapSpeedMultiplier = 1.5f;

    [Header("AI Settings")]
    [SerializeField]
    private bool _enableAIMovement = true;

    [SerializeField]
    private float _directionChangeDelay = 0.5f;

    [Header("Shadow Identity")]
    [SerializeField]
    private ShadowType _shadowType;
    #endregion

    #region Properties
    private Rigidbody2D _rb;
    private Movement _movement;
    private Vector2Int _currentDirection;
    private ShadowBehaviorCycle _shadowBehaviorCycle;
    private bool _isShadowActive = false;
    private ShadowPlayerController _owner;
    private bool _canBeSwitchedTo = true;

    public const string AnimatorBoolIsMovingBottom = "IsMovingBottom";
    public const string AnimatorBoolIsMovingUp = "IsMovingUp";
    public const string AnimatorBoolIsMovingRight = "IsMovingRight";
    public const string AnimatorBoolIsMovingLeft = "IsMovingLeft";

    private bool _isPushingPlayer;

    public bool IsShadowActive
    {
        get { return _isShadowActive; }
        set { _isShadowActive = value; }
    }

    public Vector2Int CurrentDirection
    {
        get => _currentDirection;
        set
        {
            _currentDirection = value;
            UpdateDirectObjectPosition();
        }
    }

    public bool CanBeSwitchedTo
    {
        get => _canBeSwitchedTo;
        set { _canBeSwitchedTo = value; }
    }
    public Movement Movement => _movement;
    public ShadowBehaviorCycle ShadowBehaviorCycle => _shadowBehaviorCycle;
    public ShadowPlayerController Owner
    {
        get => _owner;
        set
        {
            _owner = value;
            UpdateDirectObjectPosition();
        }
    }
    private Grid _grid;
    public bool EnableAI => _enableAIMovement;
    public float DirectionChangeDelay => _directionChangeDelay;
    public ShadowType Type => _shadowType;
    public Animator Animator => _animator;
    public Animator AnimatorFace => _faceAnimator;
    #endregion

    #region State Management
    private IShadowState _previousState;
    private IShadowState _currentState;
    private IShadowState _nextState;

    public IShadowState PreviousState => _previousState;
    public IShadowState CurrentState => _currentState;
    public IShadowState NextState => _nextState;

    /// <summary>
    /// Sets a new state for the shadow.
    /// </summary>
    public void SetState(IShadowState newState)
    {
        if (newState == null)
            return;

        _previousState = _currentState;
        _currentState?.Exit(this);

        _currentState = newState;
        _currentState.Enter(this);
    }

    /// <summary>
    /// Queues the next state to transition to.
    /// </summary>
    public void QueueNextState(IShadowState next)
    {
        _nextState = next;
    }

    /// <summary>
    /// Reverts to the previous state if available.
    /// </summary>
    public void RevertToPreviousState()
    {
        if (_previousState != null)
        {
            SetState(_previousState);
        }
    }

    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(
            _rb,
            transform,
            _grid,
            _speed,
            _centerThreshold,
            _snapSpeedMultiplier,
            GameManager.Instance.EasyMovementEnabled
        );
        _shadowBehaviorCycle = GetComponent<ShadowBehaviorCycle>();
    }

    private void Start()
    {
        GameManager.Instance.Timer.OnRoundEnded += Timer_OnRoundEnd;

        GameManager.Instance.GridManager.OnGridChanged += GridManager_OnGridChanged;
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

    private void Timer_OnRoundEnd(object sender, EventArgs e)
    {
        LightPlayerController lightPlayerController = GameManager
            .Instance.PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Light)
            .GetComponentInChildren<LightPlayerController>();

        lightPlayerController.Movement.SetSpeed(lightPlayerController.Speed);
    }

    void Update()
    {
        _currentState?.Update(this);

        if (_nextState != null)
        {
            SetState(_nextState);
            _nextState = null;
        }
    }

    public void FixedUpdate()
    {
        _movement.FixedTick();

        float speedMult = 1f;

        _movement.SpeedMult = speedMult;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            Debug.Log("Collisin with light");
            if (GameManager.Instance.IsFrightenedShadowState)
            {
                if (_isShadowActive)
                {
                    _owner = GetComponentInParent<ShadowPlayerController>();
                    _owner.StartCoroutine(_owner.SwitchShadowsRandomCoroutine());
                }
                SetState(new ShadowEatenState());
            }
            else if (_currentState != null && _currentState.State == ShadowState.Eaten)
            {
                return;
            }
            else
            {
                if ((_isShadowActive || GameManager.Instance.CurrentShadowPowerupType == ShadowPowerupType.SarcasticSmile) 
                    && !GameManager.Instance.IsLightWinningRound)
                {
                    var player = GameManager.Instance.PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Light);
                    LightPlayerController lightPlayerController = player.GetComponentInChildren<LightPlayerController>();

                    StartCoroutine(PushPlayerAndEndRound(lightPlayerController));
                }
                else
                {
                    LightPlayerController lightPlayerController =
                        collision.GetComponentInChildren<LightPlayerController>();
                    lightPlayerController.Movement.SetSpeed(lightPlayerController.Speed * 0.5f);
                }
            }
        }
    }

    private IEnumerator PushPlayerAndEndRound(LightPlayerController light)
    {
        if (_isPushingPlayer)
            yield break;

        _isPushingPlayer = true;

        foreach (var shadow in _owner.Shadows)
        {
            shadow.GetComponent<ShadowController>().SetState(new ShadowEndRoundState());
        }

        light.Movement.IsLocked = true;

        light.HandleRoundLose();
        AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.GameOver);

        yield return new WaitForSeconds(3);
        GameManager.Instance.Timer.EndRound();

        _isPushingPlayer = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            LightPlayerController lightPlayerController =
                collision.GetComponentInChildren<LightPlayerController>();

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(LightPlayerSpeedReset(lightPlayerController));
            }
        }
    }

    private IEnumerator LightPlayerSpeedReset(LightPlayerController lightPlayerController)
    {
        yield return new WaitForSeconds(1);
        lightPlayerController.Movement.SetSpeed(lightPlayerController.Speed);
    }

    private void OnDestroy()
    {
        GameManager.Instance.GridManager.OnGridChanged -= GridManager_OnGridChanged;
    }

    #endregion

    #region Movement
    /// <summary>
    /// Moves the shadow based on input and sets current direction.
    /// </summary>
    public void Move(Vector2 input)
    {
        Vector2Int newDirection = Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            newDirection = input.x > 0 ? Vector2Int.right : Vector2Int.left;
        else if (Mathf.Abs(input.y) > 0)
            newDirection = input.y > 0 ? Vector2Int.up : Vector2Int.down;

        if (newDirection != Vector2Int.zero)
            CurrentDirection = newDirection;

        _movement.OnMove(input);
    }

    /// <summary>
    /// Updates the direct indicator's position based on current direction.
    /// </summary>
    private void UpdateDirectObjectPosition()
    {
        if (_animator == null)
            return;

        Vector3 localPos = Vector3.zero;

        switch (_currentDirection)
        {
            case var d when d == Vector2Int.up:
                _animator.SetBool(AnimatorBoolIsMovingBottom, false);
                _animator.SetBool(AnimatorBoolIsMovingRight, false);
                _animator.SetBool(AnimatorBoolIsMovingLeft, false);

                _animator.SetBool(AnimatorBoolIsMovingUp, true);

                _faceAnimator.SetBool(AnimatorBoolIsMovingBottom, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingRight, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingLeft, false);

                _faceAnimator.SetBool(AnimatorBoolIsMovingUp, true);
                break;
            case var d when d == Vector2Int.down:
                _animator.SetBool(AnimatorBoolIsMovingUp, false);
                _animator.SetBool(AnimatorBoolIsMovingRight, false);
                _animator.SetBool(AnimatorBoolIsMovingLeft, false);

                _animator.SetBool(AnimatorBoolIsMovingBottom, true);

                _faceAnimator.SetBool(AnimatorBoolIsMovingUp, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingRight, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingLeft, false);

                _faceAnimator.SetBool(AnimatorBoolIsMovingBottom, true);
                break;
            case var d when d == Vector2Int.left:
                _animator.SetBool(AnimatorBoolIsMovingUp, false);
                _animator.SetBool(AnimatorBoolIsMovingBottom, false);
                _animator.SetBool(AnimatorBoolIsMovingRight, false);

                _animator.SetBool(AnimatorBoolIsMovingLeft, true);

                _faceAnimator.SetBool(AnimatorBoolIsMovingUp, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingBottom, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingRight, false);

                _faceAnimator.SetBool(AnimatorBoolIsMovingLeft, true);
                break;
            case var d when d == Vector2Int.right:
                _animator.SetBool(AnimatorBoolIsMovingUp, false);
                _animator.SetBool(AnimatorBoolIsMovingBottom, false);
                _animator.SetBool(AnimatorBoolIsMovingLeft, false);

                _animator.SetBool(AnimatorBoolIsMovingRight, true);

                _faceAnimator.SetBool(AnimatorBoolIsMovingUp, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingBottom, false);
                _faceAnimator.SetBool(AnimatorBoolIsMovingLeft, false);

                _faceAnimator.SetBool(AnimatorBoolIsMovingRight, true);
                break;
        }
    }

    #endregion

    #region Setters
    /// <summary>
    /// Sets the shadow's color.
    /// </summary>
    public void SetColor(Color color)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = color;
    }

    /// <summary>
    /// Sets the shadow type (Blinky, Pinky, etc.).
    /// </summary>
    public void SetShadowType(ShadowType type)
    {
        _shadowType = type;
    }
    #endregion


    public void OnRevive()
    {
        if (Owner == null)
            return;

        if (Owner.AreAllShadowsEaten())
        {
            Debug.LogWarning($"OnRevive triggered by shadow type: {Type}");
            StartCoroutine(ActivateNextShadowNextFrame());
        }
    }

    private IEnumerator ActivateNextShadowNextFrame()
    {
        yield return null;
        Owner.SetActiveShadow(this);
    }
}
