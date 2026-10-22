using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] Color _timerDefaultColor;
    [SerializeField] Color _timerDangerZoneColor;
    [SerializeField]
    private TMP_Text _roundsLeft;
    
    [SerializeField]
    private Slider _roundSlider;

    [SerializeField]
    private TMP_Text _countdownTop;

    [SerializeField]
    private TMP_Text _countdownBottom;

    [SerializeField]
    private float _countdownAnimationDuration = 0.5f;

    private int _lastCountdownSecond = -1;
    private Vector3 _topInitialPosition;
    private Vector3 _bottomInitialPosition;
    #endregion

    #region Private Fields
    private float _matchDurationSeconds;
    private float _roundDurationSeconds;
    private bool _soundPlayed = false;
    
    private const string RoundProgressFormat = "ROUND {0} / {1}";
    private Match _timer;
    private int _maxRounds;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        _timer = GameManager.Instance.Timer;
        if (_timer == null)
        {
            Debug.LogError("TimerUI: Timer not initialized.");
            enabled = false;
            return;
        }

        // Store initial positions
        _topInitialPosition = _countdownTop.transform.localPosition;
        _bottomInitialPosition = _countdownBottom.transform.localPosition;

        // Initialize alpha values
        var topGroup = _countdownTop.GetComponent<CanvasGroup>();
        var bottomGroup = _countdownBottom.GetComponent<CanvasGroup>();
        if (topGroup == null)
            topGroup = _countdownTop.gameObject.AddComponent<CanvasGroup>();
        if (bottomGroup == null)
            bottomGroup = _countdownBottom.gameObject.AddComponent<CanvasGroup>();

        topGroup.alpha = 0f;
        bottomGroup.alpha = 0f;

        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        // NOTE: Show/Hige timer/player indicator GO based on countdown state
        if (!_countdownTop.gameObject.activeSelf && _timer.CountdownRemaining > 0)
        {
            _countdownTop.gameObject.SetActive(true);
        }
        else if (_timer.CountdownRemaining <= 0 && _countdownTop.gameObject.activeSelf)
        {
            _countdownTop.gameObject.SetActive(false);
        }

        // Handle countdown text update and animation
        if (_timer.CountdownRemaining >= 0f)
        {
            int currentSecond = Mathf.CeilToInt(_timer.CountdownRemaining);

            if (currentSecond != _lastCountdownSecond && currentSecond > 0)
            {
                AnimateCountdownCycle(currentSecond);
                _lastCountdownSecond = currentSecond;
            }
            else if (currentSecond == 0 && _lastCountdownSecond != 0)
            {
                // Countdown just ended, reset positions and alphas
                ResetCountdownUI();
                _lastCountdownSecond = 0;
            }
        }
        else if (_countdownTop.gameObject.activeSelf)
        {
            _countdownTop.SetText("");
            _countdownBottom.SetText("");
        }

        if (!_timer.IsGameRunning || _timer.IsGamePaused)
            return;

        UpdateSliders();
    }
    #endregion


    #region Event Subscription
    private void SubscribeEvents()
    {
        _timer.OnMatchStart += TimerUI_OnMatchStart;
        _timer.OnRoundEnded += TimerUI_OnRoundEnd;
        _timer.OnMatchEnd += TimerUI_OnMatchEnd;
    }

    private void UnsubscribeEvents()
    {
        if (_timer == null)
            return;

        _timer.OnMatchStart -= TimerUI_OnMatchStart;
        _timer.OnRoundEnded -= TimerUI_OnRoundEnd;
        _timer.OnMatchEnd -= TimerUI_OnMatchEnd;
    }
    #endregion

    #region Event Handlers
    private void TimerUI_OnMatchStart(object sender, OnMatchStartEventArgs e)
    {
        _roundDurationSeconds = e.RoundDuration;
        _roundSlider.value = 1f;
        _roundSlider.fillRect.GetComponent<Image>().color = _timerDefaultColor;
        _maxRounds = e.MatchRounds;
        _roundsLeft.text = string.Format(RoundProgressFormat, e.CurrentRound, _maxRounds);
        _soundPlayed = false;
    }

    private void TimerUI_OnRoundEnd(object sender, OnRoundEndEventArgs e)
    {
        _roundSlider.value = 1f;
        _roundSlider.fillRect.GetComponent<Image>().color = _timerDefaultColor;
        _roundsLeft.text = string.Format(RoundProgressFormat, e.CurrentRound, _maxRounds);
        _soundPlayed = false;
    }

    private void TimerUI_OnMatchEnd(object sender, System.EventArgs e)
    {
        _countdownTop.SetText("");
    }

    #endregion

    #region Private Methods
    private void UpdateSliders()
    {
        float roundTimeLeft = 1f - (_timer.RoundTime / _roundDurationSeconds);

        _roundSlider.value = Mathf.Lerp(_roundSlider.value , roundTimeLeft, Time.deltaTime);
        if (roundTimeLeft <= 0.25f) {
            // if (!_soundPlayed) {
            //     _soundPlayed = true;
            //     AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.TimeDangerZone);
            // }
            _roundSlider.fillRect.GetComponent<Image>().color = _timerDangerZoneColor;
        }
    }

    private void ResetCountdownUI()
    {
        _countdownTop.transform.DOKill();
        _countdownBottom.transform.DOKill();
        _countdownTop.GetComponent<CanvasGroup>().DOKill();
        _countdownBottom.GetComponent<CanvasGroup>().DOKill();

        _countdownTop.transform.localPosition = _topInitialPosition;
        _countdownBottom.transform.localPosition = _bottomInitialPosition;
        _countdownTop.GetComponent<CanvasGroup>().alpha = 0f;
        _countdownBottom.GetComponent<CanvasGroup>().alpha = 0f;
        _countdownTop.SetText("");
        _countdownBottom.SetText("");
    }

    private void AnimateCountdownCycle(int currentNumber)
    {
        // Kill any existing tweens on these objects
        _countdownTop.transform.DOKill();
        _countdownBottom.transform.DOKill();
        _countdownTop.GetComponent<CanvasGroup>().DOKill();
        _countdownBottom.GetComponent<CanvasGroup>().DOKill();

        // Only show bottom number if there's a valid previous number from this countdown
        bool hasValidPreviousNumber =
            !string.IsNullOrEmpty(_countdownTop.text) && _lastCountdownSecond == currentNumber + 1;

        if (hasValidPreviousNumber)
        {
            // Move current middle number to center and prepare to fade out
            _countdownBottom.text = _countdownTop.text;
            _countdownBottom.transform.localPosition = _topInitialPosition;
            _countdownBottom.GetComponent<CanvasGroup>().alpha = 1f;
        }
        else
        {
            // First number of a new countdown - hide bottom number
            _countdownBottom.text = "";
            _countdownBottom.GetComponent<CanvasGroup>().alpha = 0f;
        }

        // Set new number at top (200 units above center) and prepare to fade in
        _countdownTop.text = currentNumber.ToString();
        _countdownTop.transform.localPosition = _topInitialPosition + new Vector3(0f, 200f, 0f);
        _countdownTop.GetComponent<CanvasGroup>().alpha = 0f;

        // Animate the cycle
        Sequence sq = DOTween.Sequence();

        // Top number: fade in and move down to center position (synchronous movement)
        sq.Join(
            _countdownTop
                .transform.DOLocalMove(_topInitialPosition, _countdownAnimationDuration)
                .SetEase(Ease.OutExpo)
        );
        sq.Join(
            _countdownTop
                .GetComponent<CanvasGroup>()
                .DOFade(1f, _countdownAnimationDuration)
                .SetEase(Ease.OutExpo)
        );

        // Bottom number: move down 200 units and fade out (synchronous movement)
        sq.Join(
            _countdownBottom
                .transform.DOLocalMove(
                    _topInitialPosition + new Vector3(0f, -200f, 0f),
                    _countdownAnimationDuration
                )
                .SetEase(Ease.OutExpo)
        );
        sq.Join(
            _countdownBottom
                .GetComponent<CanvasGroup>()
                .DOFade(0f, _countdownAnimationDuration)
                .SetEase(Ease.OutExpo)
        );
    }
    #endregion
}
