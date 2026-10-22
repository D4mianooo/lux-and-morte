using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// Manages scene loading, unloading, and transitions across the game.
/// Provides centralized scene management with loading screen support and cleanup routines.
/// </summary>
public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

    public enum GameScene
    {
        MainMenu,
        Game,
    }

    public static readonly Dictionary<GameScene, string> Scenes = new()
    {
        { GameScene.MainMenu, "MainMenu" },
        { GameScene.Game, "GameplayScene" },
    };

    [SerializeField]
    private LoadingScreen _loadingScreen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads a scene asynchronously using Unity's underlying SceneManager.
    /// </summary>
    /// <param name="scene">Target game scene enum.</param>
    /// <param name="mode">Load scene mode (Single or Additive).</param>
    /// <returns>AsyncOperation tracking the scene load.</returns>
    public static AsyncOperation LoadSceneAsync(GameScene scene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        string sceneName = Scenes[scene];
        return UnitySceneManager.LoadSceneAsync(sceneName, mode);
    }

    /// <summary>
    /// Loads a scene asynchronously by scene name.
    /// </summary>
    /// <param name="sceneName">Target scene name.</param>
    /// <param name="mode">Load scene mode (Single or Additive).</param>
    /// <returns>AsyncOperation tracking the scene load.</returns>
    public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        return UnitySceneManager.LoadSceneAsync(sceneName, mode);
    }

    /// <summary>
    /// Unloads a scene asynchronously.
    /// </summary>
    /// <param name="scene">Target game scene enum to unload.</param>
    /// <returns>AsyncOperation tracking the scene unload.</returns>
    public static AsyncOperation UnloadSceneAsync(GameScene scene)
    {
        string sceneName = Scenes[scene];
        return UnitySceneManager.UnloadSceneAsync(sceneName);
    }

    /// <summary>
    /// Unloads a scene asynchronously by scene name.
    /// </summary>
    /// <param name="sceneName">Target scene name.</param>
    /// <returns>AsyncOperation tracking the scene unload.</returns>
    public static AsyncOperation UnloadSceneAsync(string sceneName)
    {
        return UnitySceneManager.UnloadSceneAsync(sceneName);
    }

    /// <summary>
    /// Loads a scene with a loading screen overlay, closes menus, and runs an optional completion callback.
    /// </summary>
    /// <param name="scene">The scene to load.</param>
    /// <param name="menuToOpen">Optional menu to open in MenuManager after the scene loads.</param>
    /// <param name="resetGameState">If true, destroys GameManager to reset game state before loading.</param>
    /// <param name="onComplete">Callback invoked after scene load and menu opening complete.</param>
    public void LoadScene(
        GameScene scene,
        MenuManager.Menu? menuToOpen = null,
        bool resetGameState = false,
        Action onComplete = null)
    {
        if (resetGameState && GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        StartCoroutine(LoadSceneCoroutine(scene, menuToOpen, onComplete));
    }

    /// <summary>
    /// Loads the main menu scene, resets game state, and stops audio playback.
    /// </summary>
    public void LoadMainMenuAndReset()
    {
        LoadScene(GameScene.MainMenu, MenuManager.Menu.Main, resetGameState: true);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSounds(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private IEnumerator LoadSceneCoroutine(
        GameScene scene,
        MenuManager.Menu? menuToOpen,
        Action onComplete)
    {
        bool fadeInComplete = false;
        if (_loadingScreen != null)
        {
            _loadingScreen.Show(() => fadeInComplete = true);

            while (!fadeInComplete)
            {
                yield return null;
            }
        }

        var asyncLoad = LoadSceneAsync(scene);

        while (!asyncLoad.isDone)
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetProgress(asyncLoad.progress);
            }
            yield return null;
        }

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.CloseAllSubMenus();

            if (menuToOpen != null)
            {
                MenuManager.Instance.OpenSubMenu(menuToOpen.Value);
            }
        }

        if (_loadingScreen != null)
        {
            _loadingScreen.Hide();
        }

        onComplete?.Invoke();
    }
}
