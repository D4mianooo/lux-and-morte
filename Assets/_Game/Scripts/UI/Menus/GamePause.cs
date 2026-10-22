using UnityEngine;
using UnityEngine.UI;

public class GamePause : MenuBase
{
    [SerializeField]
    private Button _btnResume;

    [SerializeField]
    private Button _btnSettings;

    public void OnBtnExit()
    {
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadMainMenuAndReset();
        }
    }

    public void OnBtnSettings()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Settings);
        }
    }

    public void OnBtnResume()
    {
        if (GameManager.Instance?.Timer != null)
        {
            GameManager.Instance.Timer.Resume();
            MenuManager.Instance.CloseLastSubMenu();
        }
    }
}
