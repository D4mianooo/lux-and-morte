using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MenuBase
{
    [SerializeField]
    private Button _btnPlay;

    [SerializeField]
    private Button _btnTutorial;

    [SerializeField]
    private Button _btnSettings;

    [SerializeField]
    private Button _btnCredits;

    [SerializeField]
    private Button _btnExit;

    public void Start()
    {
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
        int fullscreenMode = PlayerPrefs.GetInt(
            "FullscreenMode",
            (int)FullScreenMode.ExclusiveFullScreen
        );

        // Apply saved settings
        Screen.fullScreenMode = (FullScreenMode)fullscreenMode;
        Resolution[] resolutions = Screen.resolutions;
        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(
                resolution.width,
                resolution.height,
                Screen.fullScreenMode,
                resolution.refreshRateRatio
            );
        }
    }

    public void OnBtnPlay()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.StartCutscene();
        }
    }

    public void OnBtnTutorial()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Controls);
        }
    }

    public void OnBtnSettings()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Settings);
        }
    }

    public void OnBtnCredits()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Credits);
        }
    }

    public void OnBtnExit()
    {
        Application.Quit();
    }
}
