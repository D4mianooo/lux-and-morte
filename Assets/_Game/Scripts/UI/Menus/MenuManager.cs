using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    public enum Menu
    {
        Main,
        Settings,
        Credits,
        Controls,
        Pause,
        GameOver,
    }

    public enum Scene
    {
        MainMenu,
        Game,
    }

    [Serializable]
    public struct MenuData
    {
        public GameObject Canvas;
        public GameObject Selected;
    }

    [SerializeField]
    private EventSystem _eventSystem;

    [SerializeField]
    private LoadingScreen _loadingScreen;

    [SerializeField]
    private CutsceneController _cutsceneController;

    [Header("Menu GameObjects")]
    [SerializeField]
    private MenuData _mainMenu;

    [SerializeField]
    private MenuData _settingsMenu;

    [SerializeField]
    private MenuData _creditsMenu;

    [SerializeField]
    private MenuData _controlsMenu;

    [SerializeField]
    private MenuData _pauseMenu;

    [SerializeField]
    private MenuData _gameOverMenu;

    [SerializeField]
    private GameObject _cutsceneObj;

    private Dictionary<Menu, MenuData> _subMenus;
    private Stack<Tuple<Menu, MenuData>> _openedMenus = new Stack<Tuple<Menu, MenuData>>();

    public static readonly Dictionary<Scene, string> Scenes = new()
    {
        { Scene.MainMenu, "MainMenu" },
        { Scene.Game, "SampleScene" },
    };

    public CutsceneController CutsceneController => _cutsceneController;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _subMenus = new Dictionary<Menu, MenuData>
        {
            { Menu.Main, _mainMenu },
            { Menu.Settings, _settingsMenu },
            { Menu.Credits, _creditsMenu },
            { Menu.Controls, _controlsMenu },
            { Menu.Pause, _pauseMenu },
            { Menu.GameOver, _gameOverMenu },
        };
    }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        OpenSubMenu(Menu.Main);
    }

    public void OpenSubMenu(Menu menu)
    {
        // Check if the wanted menu exists
        if (_subMenus.ContainsKey(menu) == false)
        {
            Debug.LogError($"MenuManager: Menu {menu} not found!");
            return;
        }

        // Save the currently selected object before modifying anything
        GameObject currentlySelected = _eventSystem.currentSelectedGameObject;

        // Update the last menu's saved selected object with what's currently selected
        Tuple<Menu, MenuData> lastMenuTuple = null;
        if (_openedMenus.Count > 0)
        {
            lastMenuTuple = _openedMenus.Pop();
            var updatedMenuData = new MenuData
            {
                Canvas = lastMenuTuple.Item2.Canvas,
                Selected = currentlySelected,
            };
            _openedMenus.Push(new(lastMenuTuple.Item1, updatedMenuData));

            // Hide last menu
            _subMenus[lastMenuTuple.Item1].Canvas.SetActive(false);
        }

        _openedMenus.Push(
            new(
                menu,
                new MenuData
                {
                    Canvas = _subMenus[menu].Canvas,
                    Selected = _subMenus[menu].Selected,
                }
            )
        );

        // Show new menu
        _subMenus[menu].Canvas.SetActive(true);
        _eventSystem.SetSelectedGameObject(_subMenus[menu].Selected);
    }

    public void CloseLastSubMenu()
    {
        if (_openedMenus.Count == 0)
            return;

        Tuple<Menu, MenuData> currentMenu = _openedMenus.Pop();
        _subMenus[currentMenu.Item1].Canvas.SetActive(false);

        Tuple<Menu, MenuData> lastMenu = _openedMenus.Count > 0 ? _openedMenus.Peek() : null;
        if (lastMenu != null)
        {
            _subMenus[lastMenu.Item1].Canvas.SetActive(true);
            // Only set selected object if we have one saved
            if (lastMenu.Item2.Selected != null)
            {
                _eventSystem.SetSelectedGameObject(lastMenu.Item2.Selected);
            }
        }
    }

    public void CloseAllSubMenus(Menu? openOne = null)
    {
        while (_openedMenus.Count > 0)
        {
            Tuple<Menu, MenuData> lastMenu = _openedMenus.Pop();
            _subMenus[lastMenu.Item1].Canvas.SetActive(false);
        }
        if (openOne != null)
        {
            OpenSubMenu(openOne.Value);
        }
    }

    public bool IsLastMenuOfType(Menu menu)
    {
        if (_openedMenus.Count == 0)
            return false;

        Tuple<Menu, MenuData> currentMenu = _openedMenus.Peek();
        return currentMenu.Item1 == menu;
    }

    public AsyncOperation LoadSceneAsync(Scene scene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        string sceneName = Scenes[scene];
        return SceneManager.LoadSceneAsync(sceneName, mode);
    }

    /// <summary>
    /// Loads a scene with loading screen. Automatically closes all menus.
    /// </summary>
    /// <param name="scene">The scene to load.</param>
    /// <param name="menuToOpen">Menu to open after scene loads (null to keep all closed).</param>
    /// <param name="resetGameState">If true, destroys GameManager to reset game state.</param>
    /// <param name="onComplete">Callback invoked after scene is loaded and menu is opened.</param>
    public void LoadScene(
        Scene scene,
        Menu? menuToOpen = null,
        bool resetGameState = false,
        Action onComplete = null
    )
    {
        if (resetGameState && GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        StartCoroutine(LoadSceneCoroutine(scene, menuToOpen, onComplete));
    }

    /// <summary>
    /// Loads the main menu scene and resets all game state to fresh.
    /// Destroys GameManager and any other persistent game objects.
    /// </summary>
    public void LoadMainMenuAndReset()
    {
        LoadScene(Scene.MainMenu, Menu.Main, resetGameState: true);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSounds(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private System.Collections.IEnumerator LoadSceneCoroutine(
        Scene scene,
        Menu? menuToOpen,
        Action onComplete
    )
    {
        Debug.Log($"[MenuManager] Starting scene load for {scene}");

        // Show loading screen to cover everything - wait for fade in callback
        bool fadeInComplete = false;
        if (_loadingScreen != null)
        {
            _loadingScreen.Show(() => fadeInComplete = true);

            // Wait for fade in to complete
            while (!fadeInComplete)
            {
                yield return null;
            }
        }

        Debug.Log($"[MenuManager] Loading screen shown, starting scene load");

        // Start loading the scene
        var asyncLoad = LoadSceneAsync(scene);

        Debug.Log($"[MenuManager] Waiting for scene to load to 100%...");

        // Wait until the scene is fully loaded (isDone)
        while (!asyncLoad.isDone)
        {
            // Update loading bar with real progress (0 to 1)
            if (_loadingScreen != null)
            {
                _loadingScreen.SetProgress(asyncLoad.progress);
            }
            yield return null;
        }

        Debug.Log($"[MenuManager] Scene fully loaded and activated");

        // Close any old menus
        CloseAllSubMenus();

        // Now that we're in the new scene, open the target menu if specified
        if (menuToOpen != null)
        {
            OpenSubMenu(menuToOpen.Value);
        }

        Debug.Log($"[MenuManager] Menu opened, hiding loading screen");

        // Hide loading screen with fade out
        if (_loadingScreen != null)
        {
            _loadingScreen.Hide();
        }

        // Invoke the completion callback
        onComplete?.Invoke();

        Debug.Log($"[MenuManager] Scene transition complete");
    }

    public AsyncOperation UnloadSceneAsync(Scene scene)
    {
        string sceneName = Scenes[scene];
        return SceneManager.UnloadSceneAsync(sceneName);
    }

    public T GetScript<T>(Menu menu)
    {
        if (_subMenus == null || !_subMenus.TryGetValue(menu, out var data))
        {
            throw new ArgumentException($"MenuManager: Menu {menu} not found!");
        }

        return data.Canvas.GetComponent<T>();
    }

    public void StartCutscene()
    {
        _cutsceneObj.SetActive(true);
        _cutsceneController.StartCutscene();
    }

    public void StopCutscene()
    {
        _cutsceneObj.SetActive(false);
    }
}
