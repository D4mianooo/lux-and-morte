using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Handles player-specific UI for displaying the player's nickname at the start of each round.
/// Manages the animation effect (pulsing) to help the player locate themselves on the map,
/// and swaps nicknames between rounds if needed.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Fields
    protected GameObject _playerNameObj;
    public string PlayerNick = "";
    private float _baseScale = 0.05f;
    private GameObject _playerScoreObj;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Subscribes to round start and round end events when the object is initialized.
    /// </summary>
    protected virtual void Start()
    {
        GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnded += Timer_OnRoundEnd;

        GameManager.Instance.PlayerManager.OnPlayerScoreChanged += PlayerManager_OnPlayerScoreChanged;
    }

    /// <summary>
    /// Unsubscribes from round events when the object is destroyed to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart;

    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// Called when a round ends.
    /// Swaps the current player nickname with another player's nickname.
    /// Updates the UI text accordingly.
    /// </summary>
    private void Timer_OnRoundEnd(object sender, OnRoundEndEventArgs e)
    {
        //string playerNickPom = _playerNick;
        //string anotherPlayerNickPom = _anotherPlayerNick;

        //_playerNick = anotherPlayerNickPom;
        //_anotherPlayerNick = playerNickPom;

        _playerNameObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = PlayerNick;
    }

    /// <summary>
    /// Called when a round starts.
    /// Activates the player name UI and starts the pulsing animation coroutine.
    /// </summary>
    private void Timer_OnRoundStart(object sender, System.EventArgs e)
    {
        if (_playerNameObj == null) return;

        _playerNameObj.SetActive(true);
        StartCoroutine(PulseCoroutine());
    }

    /// <summary>
    /// Triggers the score animation when this player earns points and logs it.
    /// Only runs for LightPlayerController instances.
    /// </summary>
    protected void PlayerManager_OnPlayerScoreChanged(PlayerManager.PlayerData arg1, int arg2)
    {
        if (this.GetComponent<LightPlayerController>() == null) return;

        StartCoroutine(AnimateScoreCoroutine(arg2));
        Debug.Log("Player scored: " + arg2);
    }
    #endregion

    #region Animations
    /// <summary>
    /// Coroutine that animates the player's name UI with a pulsing "join join" effect.
    /// The UI scales up and down smoothly several times, then hides at the end.
    /// </summary>
    private IEnumerator PulseCoroutine()
    {
        Transform t = _playerNameObj.transform;

        float totalDuration = 2f;         
        int pulses = 3;                    
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = elapsed / totalDuration;

            float scaleMultiplier = 1f;

            for (int i = 0; i < pulses; i++)
            {
                float start = (float)i / pulses;
                float end = (float)(i + 1) / pulses;

                if (normalized >= start && normalized <= end)
                {
                    float localT = (normalized - start) * pulses; 
                    scaleMultiplier = 1f + Mathf.Sin(localT * Mathf.PI) * 1.5f; 
                    break;
                }
            }

            t.localScale = Vector3.one * _baseScale * scaleMultiplier;
            yield return null;
        }

        t.localScale = Vector3.one * _baseScale;

        _playerNameObj.SetActive(false);
    }

    /// <summary>
    /// Animates the player's score popup when points are earned.
    /// The popup text appears, jumps up quickly, scales up for emphasis, and then returns to its original position and scale.
    /// Plays a score sound effect when the animation starts. 
    /// The animation is fast and dynamic to provide immediate visual feedback for scoring.
    /// </summary>
    /// <param name="points">The number of points to display in the popup.</param>
    private IEnumerator AnimateScoreCoroutine(int points)
    {
        if (_playerScoreObj == null) yield break;

        TMPro.TextMeshProUGUI scoreText = _playerScoreObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        scoreText.text = "+" + points;
        _playerScoreObj.SetActive(true);

        AudioManager.Instance?.PlayOneShot(
            AudioManager.Instance.FMODEvents.SFX.ScoreBeep
        );

        _playerScoreObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        _playerScoreObj.transform.localScale = Vector3.one * _baseScale;

        Sequence seq = DOTween.Sequence();

        seq.Append(_playerScoreObj.transform.DOLocalMoveY(1.1f, 0.12f).SetEase(Ease.OutQuad));
        seq.Join(_playerScoreObj.transform.DOScale(_baseScale * 1.5f, 0.12f).SetEase(Ease.OutQuad));

        seq.Append(_playerScoreObj.transform.DOLocalMoveY(0.7f, 0.12f).SetEase(Ease.InQuad));
        seq.Join(_playerScoreObj.transform.DOScale(_baseScale, 0.12f).SetEase(Ease.InQuad));

        seq.OnComplete(() => _playerScoreObj.SetActive(false));

        yield return seq.WaitForCompletion();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets the current player's nickname and updates the UI text.
    /// </summary>
    public void SetPlayerNick(string nick)
    {
        PlayerNick = nick;

        _playerNameObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = PlayerNick;
    }

    /// <summary>
    /// Assigns the UI object used to display the player's nickname.
    /// Initially disables the object until the round starts.
    /// </summary>
    public void SetPlayerNameObj(GameObject playerNameObj)
    {
        _playerNameObj = playerNameObj;
        _playerNameObj.SetActive(false);
    }

    /// <summary>
    /// Assigns the UI object used to display the player's score and hides it initially.
    /// </summary>
    public void SetPlayerScoreObj(GameObject playerScoreOPbj)
    {
        _playerScoreObj = playerScoreOPbj;
        _playerScoreObj.SetActive(false);
    }
    #endregion
}
