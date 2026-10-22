using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the shadow behavior cycle, including state transitions,
/// appearance updates, timing logic, and frightened mode handling.
/// </summary>
public class ShadowBehaviorCycle : MonoBehaviour
{
    #region Properties
    private ShadowController _controller;
    private Coroutine _cycleRoutine;

    private readonly float[] _scatterDurations = { 7f, 7f, 5f, 5f };
    private readonly float[] _chaseDurations = { 10f, 10f, 15f, 9999f };

    private int _phaseIndex = 0;
    private bool _runningCycle = false;

    [SerializeField] private float frightenedDuration = 60f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _controller = GetComponent<ShadowController>();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Starts the scatter/chase behavior cycle if not already running.
    /// </summary>
    public void StartBehaviorCycle()
    {
        if (!_runningCycle)
            _cycleRoutine = StartCoroutine(BehaviorCycleRoutine());
    }
    #endregion

    #region Main Cycle
    /// <summary>
    /// Executes the repeating behavior pattern: scatter → chase (multiple phases).
    /// </summary>
    private IEnumerator BehaviorCycleRoutine()
    {
        _runningCycle = true;

        while (_phaseIndex < _scatterDurations.Length)
        {
            yield return RunPhaseWithInterrupt(
                new ShadowScatterState(),
                _scatterDurations[_phaseIndex]
            );

            yield return RunPhaseWithInterrupt(
                new ShadowChaseState(),
                _chaseDurations[_phaseIndex]
            );

            _phaseIndex++;
        }

        yield return RunPhaseWithInterrupt(
            new ShadowChaseState(),
            Mathf.Infinity
        );

        _runningCycle = false;
    }
    #endregion

    #region Phase Handling
    /// <summary>
    /// Runs a behavior phase for a given duration while allowing interruption by frightened mode.
    /// </summary>
    private IEnumerator RunPhaseWithInterrupt(IShadowState phaseState, float duration)
    {
        float elapsed = 0f;
        if (!_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
        {
            _controller.SetState(phaseState);
        }

        while (elapsed < duration)
        {
            if (GameManager.Instance.IsFrightenedShadowState && _controller.CurrentState.State != ShadowState.Eaten)
            {
                yield return StartCoroutine(HandleFrightenedState());
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    #region Frightened Mode
    /// <summary>
    /// Handles the temporary frightened state, pausing the current cycle.
    /// Restores either the previous state or fallback to Scatter when finished.
    /// </summary>
    private IEnumerator HandleFrightenedState()
    {
        if (_controller.CurrentState.State == ShadowState.Eaten)
            yield break;

        IShadowState previousState = null;

        if (!_controller.IsShadowActive)
            previousState = _controller.CurrentState;

        if(!_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
            _controller.SetState(new ShadowFrightenedState());

        float remaining = frightenedDuration;

        while (remaining > 0f) {
            if (!GameManager.Instance.Timer.IsGamePaused) {
                remaining -= Time.deltaTime;
            }

            yield return null;
        }

        GameManager.Instance.IsFrightenedShadowState = false;
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.None;

        if (previousState != null && !_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
        {
            _controller.SetState(previousState);
        } else if(!_controller.IsShadowActive && _controller.CurrentState.State != ShadowState.Eaten)
        {
            _controller.SetState(new ShadowScatterState());
        }
    }
    #endregion
}
