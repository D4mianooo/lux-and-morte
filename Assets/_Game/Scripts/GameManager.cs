using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private GameObject _globalVolume;

    [SerializeField]
    public Match Timer;

    [SerializeField]
    private GameObject _particleSystem;

    [SerializeField]
    private PlayerSpawner _playerSpawner;

    [SerializeField]
    private GridManager _gridManager;

    [SerializeField]
    private CameraManager _cameraManager;

    public bool IsFrightenedShadowState = false;

    [SerializeField] private ShadowPowerupType _currentShadowPowerupType = ShadowPowerupType.None;
    [SerializeField] private LightPowerupType _currentLightPowerupType = LightPowerupType.None;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }
    public PlayerSpawner PlayerSpawner => _playerSpawner;
    public GameObject ParticleSystem
    {
        get { return _particleSystem; }
        set { _particleSystem = value; }
    }
    public GridManager GridManager => _gridManager;
    public bool EasyMovementEnabled { get; set; } = false;

    public CameraManager CameraManager => _cameraManager;

    public ShadowPowerupType CurrentShadowPowerupType
    {
        get => _currentShadowPowerupType;
        set => _currentShadowPowerupType = value;
    }

    public LightPowerupType CurrentLightPowerupType
    {
        get => _currentLightPowerupType;
        set => _currentLightPowerupType = value;
    }

    public bool IsLightWinningRound = false;

    public void RegisterGrid(Grid newGrid)
    {
        if (Grid != null)
        {
            Grid.OnNewLightTile -= GameManager_OnNewLightTile;
            Grid.OnAllLightTiles -= GameManager_OnAllLightTiles;
        }

        Grid = newGrid;
        Debug.Log("Register grid");

        if (Grid != null)
        {
            Grid.OnNewLightTile += GameManager_OnNewLightTile;
            Grid.OnAllLightTiles += GameManager_OnAllLightTiles;
        }
    }

    public void BtnMainMenu()
    {
        // Remove the GameManager before going to main menu
        Destroy(gameObject);
        if (AudioManager.Instance.ActiveInstances.Count > 0)
        {
            AudioManager.Instance.StopEventInstance(
                AudioManager.Instance.ActiveInstances.First().Key,
                FMOD.Studio.STOP_MODE.ALLOWFADEOUT
            );
        }
        SceneManager.LoadSceneAsync("MainMenu");
    }

    private void Awake()
    {
        PlayerManager = new PlayerManager();

        Timer.OnRoundEnded += OnRoundEnd;
        Timer.OnMatchEnd += OnMatchEnd;
        Timer.OnMatchStart += OnMatchStart;

        _globalVolume.SetActive(true);
    }

    private void OnMatchStart(object sender, OnMatchStartEventArgs e)
    {
        // Initialize player scores
        foreach (var p in PlayerManager.Players)
        {
            p.PlayerScore.RoundScores.Add(
                new PlayerScore.RoundScore(
                    Timer.CurrentRound,
                    PlayerManager.PointsPerSkullKill,
                    PlayerManager.PointsPerLightTile,
                    PlayerManager.MaxTimeBonusPoints,
                    PlayerManager.TimeBonusExponent
                )
            );
        }
    }

    private void OnMatchEnd(object sender, EventArgs e)
    {
        MenuManager.Instance.OpenSubMenu(MenuManager.Menu.GameOver);
        MenuManager.Instance.GetScript<GameOverMenu>(MenuManager.Menu.GameOver).UpdateText();
    }

    private void GameManager_OnNewLightTile(object sender, EventArgs e)
    {
        var player = PlayerManager.Players
            .FirstOrDefault(p => p.Role == PlayerManager.PlayerRole.Light);

        if (player == null)
            return;

        var roundScore = player.PlayerScore.RoundScores
            .FirstOrDefault(r => r.RoundNumber == Timer.CurrentRound);

        if (roundScore == null)
            return;

        roundScore.AddLightTile();
        if (_currentLightPowerupType == LightPowerupType.SilentTreatment) {
            player.RaisePlayerScoreChanged(player.PlayerScore.RoundScores[0].PointsPerLightTile + 1);
        }
        else { 
            player.RaisePlayerScoreChanged(player.PlayerScore.RoundScores[0].PointsPerLightTile);
        }
    }

    private void OnRoundEnd(object sender, EventArgs e)
    {
        IsLightWinningRound = false;

        // Swap players and update score
        PlayerManager.SwapPlayerRoles(
            PlayerManager.PlayerRole.Light,
            PlayerManager.PlayerRole.Skull
        );
        foreach (var p in PlayerManager.Players)
        {
            p.PlayerScore.RoundScores.Add(
                new PlayerScore.RoundScore(
                    Timer.CurrentRound,
                    PlayerManager.PointsPerSkullKill,
                    PlayerManager.PointsPerLightTile,
                    PlayerManager.MaxTimeBonusPoints,
                    PlayerManager.TimeBonusExponent
                )
            );
        }
    }

    private void GameManager_OnAllLightTiles(object sender, EventArgs e)
    {
        IsFrightenedShadowState = true;

        var player = PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Light);
        LightPlayerController lightPlayerController = player.GetComponentInChildren<LightPlayerController>();
        lightPlayerController.HandleRoundWin();

        // Score bonus for time left
        // (Get this round because round has not yet advanced)
        // Claculate the bonus before round end to preserve Timer.RoundTime
        PlayerManager
            .Players.First(p => p.Role == PlayerManager.PlayerRole.Light)
            ?.PlayerScore.RoundScores.First(r => r.RoundNumber == Timer.CurrentRound)
            ?.SetTimeBonus(Timer.RoundTime, Timer.RoundDuration);

        StartCoroutine(AllLightTilesEndRound());
    }

    private IEnumerator AllLightTilesEndRound()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.Win);
        yield return new WaitForSeconds(3);
        Timer.EndRound();
    }

    private void OnDestroy()
    {
        Timer.OnRoundEnded -= OnRoundEnd;
        if (Grid != null)
        {
            Grid.OnNewLightTile -= GameManager_OnNewLightTile;
        }
    }

    public Volume GetGlobalVolume()
    {
        return _globalVolume.GetComponent<Volume>();
    }
}
