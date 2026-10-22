using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Match : MonoBehaviour
{
    #region Inspector Fields

    [Tooltip("Countdown before match start")]
    [SerializeField]
    private float _matchCountdown = 5f;

    [SerializeField]
    private float _roundCountdown = 3f;

    [Tooltip("Match duration in seconds")]
    [SerializeField]
    private float _matchDurationSeconds = 300f;

    [Tooltip("Round duration in seconds")]
    [SerializeField]
    private float _roundDurationSeconds = 10f;

    [Tooltip("Round in single match")]
    [SerializeField]
    [Range(1, 10)]
    private int _rounds = 3;

    private int _currentRound;
    public int CurrentRound
    {
        get => _currentRound;
    }
    #endregion

    #region Events

    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnMatchEnd;
    public event EventHandler OnMatchPause;
    public event EventHandler OnMatchResume;
    public event EventHandler<OnRoundEndEventArgs> OnRoundEnding;
    public event EventHandler<OnRoundEndEventArgs> OnRoundEnded;
    public event EventHandler OnRoundStart;

    #endregion

    #region Private Fields

    public float CountdownRemaining { get; private set; }
    public float MatchTime { get; private set; }
    public float RoundDuration => _roundDurationSeconds;
    public float RoundTime { get; private set; }
    public bool IsGameRunning { get; private set; }
    public bool IsGamePaused { get; private set; }
    #endregion

    #region Unity Methods

    private void Update()
    {
        if (!IsGameRunning || IsGamePaused)
            return;

        RoundTime += Time.deltaTime;
        if (_currentRound > _rounds)
        {
            EndMatch();
        }
        if (RoundTime >= _roundDurationSeconds)
        {
            EndRound();
        }
    }

    #endregion

    #region Match Flow

    public void StartMatch()
    {
        _currentRound = 1;
        if (IsGameRunning)
            return;
        StartCoroutine(StartMatchCountdown(_matchCountdown));
    }

    private IEnumerator StartMatchCountdown(float delay)
    {
        Pause();
        yield return RunCountdown(delay);
        Resume();
        RunMatch();
    }

    private void RunMatch()
    {
        IsGameRunning = true;
        MatchTime = 0;
        RoundTime = 0;

        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_rounds, _roundDurationSeconds));
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    public void EndMatch()
    {
        IsGameRunning = false;
        OnMatchEnd?.Invoke(this, EventArgs.Empty);
        StopAllCoroutines();
    }

    #endregion

    #region Round Flow

    /// <summary>
    /// Ends the current round. Pauses the timer, resets round time, and fires OnRoundEnd event.
    /// External systems should call StartRoundCountdown() when ready to begin the next round.
    /// </summary>
    public void EndRound()
    {
        OnRoundEnding?.Invoke(this, new OnRoundEndEventArgs(_currentRound));
        RoundTime = 0;
        _currentRound++;
        Pause();
        if (_currentRound > 10)
        {
            EndMatch();

            return;
        }
        OnRoundEnded?.Invoke(this, new OnRoundEndEventArgs(_currentRound));
    }

    /// <summary>
    /// Starts the countdown for the next round. Call this after handling round end logic (e.g., animations).
    /// </summary>
    public void StartRoundCountdown()
    {
        StartCoroutine(HandleRoundCountdown());
    }

    private IEnumerator HandleRoundCountdown()
    {
        yield return RunCountdown(_roundCountdown);
        Resume();
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Pause & Resume

    public void Pause()
    {
        IsGamePaused = true;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }

    public void Resume()
    {
        IsGamePaused = false;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Countdown Utility

    private IEnumerator RunCountdown(float duration)
    {
        CountdownRemaining = duration;
        while (CountdownRemaining > 0f)
        {
            yield return new WaitForEndOfFrame();
            CountdownRemaining -= Time.deltaTime;
        }
    }
    #endregion
}

#region Event Args Class

public class OnMatchStartEventArgs : EventArgs
{
    public OnMatchStartEventArgs(int matchRounds, float roundDuration, int currentRound = 1)
    {
        MatchRounds = matchRounds;
        CurrentRound = currentRound;
        RoundDuration = roundDuration;
    }

    public readonly int CurrentRound;
    public readonly int MatchRounds;
    public readonly float RoundDuration;
}

public class OnRoundEndEventArgs : EventArgs
{
    public OnRoundEndEventArgs(int currentRound)
    {
        CurrentRound = currentRound;
    }

    public readonly int CurrentRound;
}
#endregion
