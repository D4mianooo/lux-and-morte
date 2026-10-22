using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameOverMenu : MenuBase
{
    [SerializeField]
    private TextMeshProUGUI _winnerText;

    [SerializeField]
    private TextMeshProUGUI _p1ScoreText;

    [SerializeField]
    private TextMeshProUGUI _p2ScoreText;

    [SerializeField] private TextMeshProUGUI _p1NickText;
    [SerializeField] private TextMeshProUGUI _p2NickText;

    [SerializeField] private GameObject _trophy;
    [SerializeField] private Animator _trophyAnimator;

    [SerializeField] private RectTransform _platform1;
    [SerializeField] private RectTransform _platform1Image;
    [SerializeField] private RectTransform _platform2;
    [SerializeField] private RectTransform _platform2Image;

    private float _platformMaxHeight;

    private void Awake()
    {
        _platformMaxHeight = _platform1.sizeDelta.y;
    }


    public void OnBtnMainMenu()
    {
        Debug.Log("Main Menu Button Clicked");
        MenuManager.Instance.LoadMainMenuAndReset();
    }

    public void UpdateText()
    {
        _trophy.SetActive(false);
        _trophyAnimator.SetBool("IsVisible", false);

        _p1NickText.gameObject.SetActive(false);
        _p2NickText.gameObject.SetActive(false);

        _platform1Image.sizeDelta = new Vector2(_platform1Image.sizeDelta.x, 0);
        _platform2Image.sizeDelta = new Vector2(_platform2Image.sizeDelta.x, 0);

        var players = GameManager.Instance.PlayerManager.Players;

        var orderedPlayers = players
            .OrderByDescending(p => p.PlayerScore.TotalScore)
            .ToList();

        var winner = orderedPlayers[0];
        var loser = orderedPlayers[1];

        _winnerText.SetText(winner.Nickname + " Wins!");

        float ratio = (float)loser.PlayerScore.TotalScore
              / winner.PlayerScore.TotalScore;

        int loserHeight = Mathf.RoundToInt(_platformMaxHeight * ratio);

        _platform2.sizeDelta = new Vector2(
            _platform2.sizeDelta.x,
            loserHeight
        );

        _p1NickText.SetText(winner.Nickname);
        _p2NickText.SetText(loser.Nickname);

        int winnerImageHeight =
            Mathf.RoundToInt(_platform1.sizeDelta.y - 10);

        int loserImageHeight =
            Mathf.RoundToInt(_platform2.sizeDelta.y - 10);

        StartCoroutine(
            CountScore(
                _p1ScoreText,
                _platform1Image,
                winner.PlayerScore.TotalScore,
                winnerImageHeight
            )
        );

        StartCoroutine(
            CountScore(
                _p2ScoreText,
                _platform2Image,
                loser.PlayerScore.TotalScore,
                loserImageHeight
            )
        );

    }

    private IEnumerator CountScore(
        TextMeshProUGUI text,
        RectTransform image,
        int targetScore,
        int targetHeight
    )
    {
        int currentScore = 0;
        int currentHeight = 0;

        float duration = 3.0f;
        float time = 0f;

        image.sizeDelta = new Vector2(image.sizeDelta.x, 0);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, t));
            currentHeight = Mathf.RoundToInt(Mathf.Lerp(0, targetHeight, t));

            text.SetText("Score: " + currentScore);
            image.sizeDelta = new Vector2(image.sizeDelta.x, currentHeight);

            yield return null;
        }

        text.SetText("Score: " + targetScore);
        image.sizeDelta = new Vector2(image.sizeDelta.x, targetHeight);

        _p1NickText.gameObject.SetActive(true);
        _p2NickText.gameObject.SetActive(true);

        _trophy.SetActive(true);
        _trophyAnimator.SetBool("IsVisible", true);
    }

}
