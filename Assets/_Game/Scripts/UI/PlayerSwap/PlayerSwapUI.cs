using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player swap UI overlay, showing which player has which role.
/// Top bar = Player 1 (fixed position), Bottom bar = Player 2 (fixed position).
/// Role text swaps between bars with dramatic animation when roles change.
/// </summary>
public class PlayerSwapUI : MonoBehaviour
{
    [Header("UI References - Fixed Player Positions")]
    [Tooltip("Light color indicator")]
    [SerializeField]
    private Image _lightColorIndicator;

    [Tooltip("Shadow color indicator")]
    [SerializeField]
    private Image _shadowColorIndicator;

    [Tooltip("Player 1's role text (Light/Shadow)")]
    [SerializeField]
    private TextMeshProUGUI _p1NameText;

    [Tooltip("Player 2's role text (Light/Shadow)")]
    [SerializeField]
    private TextMeshProUGUI _p2NameText;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [Header("Animation Settings")]
    [SerializeField]
    private float _fadeInDuration = 0.3f;

    [SerializeField]
    private float _roleSwapDuration = 0.6f;

    [SerializeField]
    private float _displayDuration = 1.5f;

    [SerializeField]
    private float _fadeOutDuration = 0.5f;

    [SerializeField]
    private float _textFlyoutDistance = 200f;

    [SerializeField]
    private Ease _roleSwapEase = Ease.OutBack;

    [Header("Score display")]
    [SerializeField]
    private TextMeshProUGUI _p1ScoreText;

    [SerializeField]
    private TextMeshProUGUI _p2ScoreText;

    private PlayerManager _playerManager;
    private Sequence _animationSequence;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Start hidden
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            _playerManager = GameManager.Instance.PlayerManager;
            if (_playerManager != null)
            {
                _playerManager.OnPlayerSwapped += OnPlayerSwapped;
            }
        }
        else
        {
            Debug.LogWarning("PlayerSwapUI: GameManager not found!");
        }
    }

    private void OnDestroy()
    {
        if (_playerManager != null)
        {
            _playerManager.OnPlayerSwapped -= OnPlayerSwapped;
        }

        _animationSequence?.Kill();
    }

    /// <summary>
    /// Called when players swap roles. Triggers the full swap animation sequence.
    /// </summary>
    private void OnPlayerSwapped(object sender, EventArgs e)
    {
        PlayScoreSequenceAndSwap();
    }

    /// <summary>
    /// Shows the initial player/role assignment with fade in, holds for display duration, then fades out.
    /// Use onComplete callback to trigger match start after the display.
    /// </summary>
    /// <param name="displayDuration">How long to display the role assignment before fading out</param>
    /// <param name="onComplete">Callback when the entire sequence (fade in, hold, fade out) completes</param>
    public void ShowInitialAssignment(float displayDuration = 3f, Action onComplete = null)
    {
        var p1 = GameManager.Instance.PlayerManager.Players.FirstOrDefault(p =>
            p.Index == PlayerManager.PlayerIndex.P1
        );
        var p2 = GameManager.Instance.PlayerManager.Players.FirstOrDefault(p =>
            p.Index == PlayerManager.PlayerIndex.P2
        );

        _p1NameText.text = p1 != null ? p1.Nickname : "Player 1";
        _p2NameText.text = p2 != null ? p2.Nickname : "Player 2";

        _animationSequence?.Kill();
        gameObject.SetActive(true);

        _p1ScoreText.text = FormatScoreText(PlayerManager.PlayerIndex.P1);
        _p2ScoreText.text = FormatScoreText(PlayerManager.PlayerIndex.P2);

        _lightColorIndicator.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            p1 != null && p1.Role == PlayerManager.PlayerRole.Light ? 0f : 180f
        );
        _shadowColorIndicator.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            p1 != null && p1.Role == PlayerManager.PlayerRole.Light ? 180f : 0f
        );

        // Create sequence: fade in -> hold -> fade out
        _animationSequence = DOTween.Sequence();
        _animationSequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration));
        _animationSequence.AppendInterval(displayDuration);
        _animationSequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration));
        _animationSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private string FormatScoreText(PlayerManager.PlayerIndex playerIndex)
    {
        var player = GameManager.Instance?.PlayerManager.Players.FirstOrDefault(p =>
            p.Index == playerIndex
        );
        return $"Score: {player?.PlayerScore.TotalScore ?? 0}";
    }

    private struct RoundScoreBreakdown
    {
        public int BaseScore; // Score from previous rounds
        public int TileScore;
        public int KillScore;
        public int TimeScore;
        public int TotalRoundScore => TileScore + KillScore + TimeScore;
    }

    private RoundScoreBreakdown GetRoundBreakdown(PlayerManager.PlayerIndex playerIndex)
    {
        // This function is called after round ends event is fired and roles are swapped
        // CurrentRound is already incremented (e.g., if round 1 just finished, CurrentRound = 2)
        // Match starts at round 1 (not 0)

        var player = GameManager.Instance.PlayerManager.Players.FirstOrDefault(p =>
            p.Index == playerIndex
        );
        if (player == null)
            return new RoundScoreBreakdown();

        // Get the just-completed round number (1-based)
        int justCompletedRoundNumber = (GameManager.Instance.Timer?.CurrentRound ?? 1) - 1;
        if (justCompletedRoundNumber < 1)
            justCompletedRoundNumber = 1;

        // Base score = sum of all rounds BEFORE the just-completed round
        int baseScore = player.PlayerScore.RoundScores.Sum(p =>
            p.RoundNumber < justCompletedRoundNumber ? p.TotalScore : 0
        );

        // Find the correct round by RoundNumber property
        int tileScore = 0;
        int killScore = 0;
        int timeScore = 0;

        var round = player.PlayerScore.RoundScores.FirstOrDefault(r =>
            r.RoundNumber == justCompletedRoundNumber
        );

        if (round != null)
        {
            tileScore = round.LightTiles * round.PointsPerLightTile;
            killScore = round.SkullKills * round.PointsPerSkullKill;
            timeScore = round.TimeBonus;
        }

        return new RoundScoreBreakdown
        {
            BaseScore = baseScore,
            TileScore = tileScore,
            KillScore = killScore,
            TimeScore = timeScore,
        };
    }

    private Tween ChangeTextSmooth(TextMeshProUGUI textComp, string newText, float duration = 0.5f)
    {
        Sequence s = DOTween.Sequence();
        s.Append(textComp.DOFade(0f, duration * 0.5f));
        s.AppendCallback(() => textComp.text = newText);
        s.Append(textComp.DOFade(1f, duration * 0.5f));
        return s;
    }

    /// <summary>
    /// Tweens the score text from base value, adding amountToAdd over time.
    /// Text format: "Score: {current} (+{remainingToAdd})"
    /// </summary>
    private Tween AnimateScoreAddition(
        TextMeshProUGUI scoreText,
        int startTotal,
        int amountToAdd,
        float duration = 1.5f
    )
    {
        if (amountToAdd == 0)
        {
            // If 0, wait for duration and update text to remove (+0) if we want clean loop,
            // but we want to show (+0) to indicate activity was processed.
            return DOTween
                .Sequence()
                .OnStart(() => scoreText.text = $"Score: {startTotal} (+0)")
                .AppendInterval(duration)
                .OnComplete(() => scoreText.text = $"Score: {startTotal}");
        }

        float displayScore = startTotal;
        float remainingAdd = amountToAdd;
        int lastDisplayValue = -1;

        return DOTween
            .To(() => displayScore, x => displayScore = x, startTotal + amountToAdd, duration)
            .SetEase(Ease.OutCubic)
            .OnStart(() =>
            {
                scoreText.text = $"Score: {startTotal} (+{amountToAdd})";
            })
            .OnUpdate(() =>
            {
                remainingAdd = amountToAdd - (displayScore - startTotal);
                int currentDisplay = Mathf.FloorToInt(displayScore);
                int currentRemaining = Mathf.CeilToInt(remainingAdd);

                if (currentDisplay != lastDisplayValue)
                {
                    lastDisplayValue = currentDisplay;
                    // Play a beep occasionally or on change - might be too frequent, maybe limit?
                    // Reusing existing logic
                    AudioManager.Instance?.PlayOneShot(
                        AudioManager.Instance.FMODEvents.SFX.ScoreBeep
                    );
                }

                if (currentRemaining > 0)
                    scoreText.text = $"Score: {currentDisplay} (+{currentRemaining})";
                else
                    scoreText.text = $"Score: {currentDisplay}";
            })
            .OnComplete(() =>
            {
                scoreText.text = $"Score: {startTotal + amountToAdd}";
            });
    }

    /// <summary>
    /// Plays the complete player swap animation sequence with detailed score breakdown.
    /// </summary>
    public void PlayScoreSequenceAndSwap()
    {
        _animationSequence?.Kill();
        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;

        var p1 = GameManager.Instance.PlayerManager.Players.FirstOrDefault(p =>
            p.Index == PlayerManager.PlayerIndex.P1
        );
        var p2 = GameManager.Instance.PlayerManager.Players.FirstOrDefault(p =>
            p.Index == PlayerManager.PlayerIndex.P2
        );
        string p1Name = p1?.Nickname ?? "Player 1";
        string p2Name = p2?.Nickname ?? "Player 2";

        // Determine who WAS Light during the just-completed round
        // Note: Roles have already been swapped by the time this is called
        // So the player who is NOW Light was Skull during the completed round
        // and the player who is NOW Skull was Light during the completed round
        var lightPlayerIndex =
            p1?.Role == PlayerManager.PlayerRole.Light
                ? PlayerManager.PlayerIndex.P2
                : PlayerManager.PlayerIndex.P1;
        var skullPlayerIndex =
            lightPlayerIndex == PlayerManager.PlayerIndex.P1
                ? PlayerManager.PlayerIndex.P2
                : PlayerManager.PlayerIndex.P1;

        // Get breakdown - Light player gets all bonuses, Skull only gets kill score
        var lightScore = GetRoundBreakdown(lightPlayerIndex);
        var skullScore = GetRoundBreakdown(skullPlayerIndex);

        // Determine which text fields correspond to which role
        bool p1WasLight = lightPlayerIndex == PlayerManager.PlayerIndex.P1;
        var lightNameText = p1WasLight ? _p1NameText : _p2NameText;
        var skullNameText = p1WasLight ? _p2NameText : _p1NameText;
        var lightScoreText = p1WasLight ? _p1ScoreText : _p2ScoreText;
        var skullScoreText = p1WasLight ? _p2ScoreText : _p1ScoreText;

        // Prepare UI - Start with player nicknames
        _p1NameText.text = p1Name;
        _p2NameText.text = p2Name;

        // Set color indicator rotations based on current roles (after swap)
        // Roles are already swapped, so Light is now where Skull was
        _lightColorIndicator.transform.rotation = Quaternion.Euler(0f, 0f, p1WasLight ? 0f : 180f);
        _shadowColorIndicator.transform.rotation = Quaternion.Euler(0f, 0f, p1WasLight ? 180f : 0f);

        // Initial Score Text
        lightScoreText.text = $"Score: {lightScore.BaseScore}";
        skullScoreText.text = $"Score: {skullScore.BaseScore}";

        _animationSequence = DOTween.Sequence();

        // 1. Fade In & Show Names
        _animationSequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration));
        _animationSequence.AppendInterval(1.0f);

        int lightRunningTotal = lightScore.BaseScore;

        // 2. Tile Score Phase (Light player only)
        _animationSequence.Append(ChangeTextSmooth(lightNameText, "Tile Score"));
        _animationSequence.Append(
            AnimateScoreAddition(lightScoreText, lightRunningTotal, lightScore.TileScore)
        );
        _animationSequence.AppendInterval(0.2f);
        lightRunningTotal += lightScore.TileScore;

        // 3. Kill Score Phase (Light player only)
        _animationSequence.Append(ChangeTextSmooth(lightNameText, "Kill Score"));
        _animationSequence.Append(
            AnimateScoreAddition(lightScoreText, lightRunningTotal, lightScore.KillScore)
        );
        _animationSequence.AppendInterval(0.2f);
        lightRunningTotal += lightScore.KillScore;

        // 4. Time Bonus Phase (Light player only)
        _animationSequence.Append(ChangeTextSmooth(lightNameText, "Time Bonus"));
        _animationSequence.Append(
            AnimateScoreAddition(lightScoreText, lightRunningTotal, lightScore.TimeScore)
        );
        _animationSequence.AppendInterval(0.5f);

        // 5. Restore Light player's name to their nickname (Skull player name never changed)
        string lightPlayerName = p1WasLight ? p1Name : p2Name;
        _animationSequence.Append(ChangeTextSmooth(lightNameText, lightPlayerName));
        _animationSequence.AppendInterval(1.0f); // "Wait a second"

        // Visual Swap
        _animationSequence.Append(RotateColorIndicators());
        _animationSequence.AppendInterval(_roleSwapDuration);

        // Fade Out
        _animationSequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration));

        _animationSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.Timer != null)
            {
                GameManager.Instance.Timer.StartRoundCountdown();
            }
        });
    }

    /// <summary>
    /// Animate 180 deg rotation of color indicators.
    /// </summary>
    private Sequence RotateColorIndicators()
    {
        Sequence s = DOTween.Sequence();

        s.Append(
            _lightColorIndicator
                .transform.DORotate(
                    new Vector3(
                        0f,
                        0f,
                        _lightColorIndicator.transform.rotation.eulerAngles.z + 180f
                    ),
                    _roleSwapDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(_roleSwapEase)
        );

        s.Join(
            _shadowColorIndicator
                .transform.DORotate(
                    new Vector3(
                        0f,
                        0f,
                        _shadowColorIndicator.transform.rotation.eulerAngles.z + 180f
                    ),
                    _roleSwapDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(_roleSwapEase)
        );

        return s;
    }

    /// <summary>
    /// Immediately hides the swap UI without animation.
    /// </summary>
    public void Hide()
    {
        _animationSequence?.Kill();
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
