using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manages menu stack navigation, opening, closing, and selection states for UI menus.
/// Focused strictly on menu lifecycle and UI navigation without scene transition concerns.
/// </summary>
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

    [Serializable]
    public struct MenuData
    {
        public GameObject Canvas;
        public GameObject Selected;
    }

    [Header("Event System")]
    [SerializeField]
    private EventSystem _eventSystem;

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

    private Dictionary<Menu, MenuData> _subMenus;
    private Stack<Tuple<Menu, MenuData>> _openedMenus = new Stack<Tuple<Menu, MenuData>>();

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
            return;
        }

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
        OpenSubMenu(Menu.Main);
    }

    /// <summary>
    /// Opens the specified submenu, pushing it to the menu stack and activating its canvas.
    /// </summary>
    /// <param name="menu">The submenu type to open.</param>
    public void OpenSubMenu(Menu menu)
    {
        if (!_subMenus.ContainsKey(menu))
        {
            Debug.LogError($"MenuManager: Menu {menu} not found!");
            return;
        }

        GameObject currentlySelected = _eventSystem != null ? _eventSystem.currentSelectedGameObject : null;

        if (_openedMenus.Count > 0)
        {
            Tuple<Menu, MenuData> lastMenuTuple = _openedMenus.Pop();
            var updatedMenuData = new MenuData
            {
                Canvas = lastMenuTuple.Item2.Canvas,
                Selected = currentlySelected,
            };
            _openedMenus.Push(new(lastMenuTuple.Item1, updatedMenuData));

            if (lastMenuTuple.Item2.Canvas != null)
            {
                _subMenus[lastMenuTuple.Item1].Canvas.SetActive(false);
            }
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

        if (_subMenus[menu].Canvas != null)
        {
            _subMenus[menu].Canvas.SetActive(true);
        }

        if (_eventSystem != null && _subMenus[menu].Selected != null)
        {
            _eventSystem.SetSelectedGameObject(_subMenus[menu].Selected);
        }
    }

    /// <summary>
    /// Closes the top-most submenu on the stack and restores the previous submenu if available.
    /// </summary>
    public void CloseLastSubMenu()
    {
        if (_openedMenus.Count == 0)
        {
            return;
        }

        Tuple<Menu, MenuData> currentMenu = _openedMenus.Pop();
        if (currentMenu.Item2.Canvas != null)
        {
            _subMenus[currentMenu.Item1].Canvas.SetActive(false);
        }

        Tuple<Menu, MenuData> lastMenu = _openedMenus.Count > 0 ? _openedMenus.Peek() : null;
        if (lastMenu != null)
        {
            if (lastMenu.Item2.Canvas != null)
            {
                _subMenus[lastMenu.Item1].Canvas.SetActive(true);
            }

            if (_eventSystem != null && lastMenu.Item2.Selected != null)
            {
                _eventSystem.SetSelectedGameObject(lastMenu.Item2.Selected);
            }
        }
    }

    /// <summary>
    /// Closes all open submenus on the stack and optionally opens a new one.
    /// </summary>
    /// <param name="openOne">Optional menu to open after closing all others.</param>
    public void CloseAllSubMenus(Menu? openOne = null)
    {
        while (_openedMenus.Count > 0)
        {
            Tuple<Menu, MenuData> lastMenu = _openedMenus.Pop();
            if (lastMenu.Item2.Canvas != null)
            {
                _subMenus[lastMenu.Item1].Canvas.SetActive(false);
            }
        }

        if (openOne != null)
        {
            OpenSubMenu(openOne.Value);
        }
    }

    /// <summary>
    /// Checks whether the currently active submenu at the top of the stack matches the specified type.
    /// </summary>
    /// <param name="menu">Menu type to test.</param>
    /// <returns>True if the active menu matches, false otherwise.</returns>
    public bool IsLastMenuOfType(Menu menu)
    {
        if (_openedMenus.Count == 0)
        {
            return false;
        }

        Tuple<Menu, MenuData> currentMenu = _openedMenus.Peek();
        return currentMenu.Item1 == menu;
    }

    /// <summary>
    /// Retrieves a component of type T attached to the menu's canvas.
    /// </summary>
    /// <typeparam name="T">Component type to find on the canvas.</typeparam>
    /// <param name="menu">Menu type.</param>
    /// <returns>Component of type T.</returns>
    public T GetScript<T>(Menu menu)
    {
        if (_subMenus == null || !_subMenus.TryGetValue(menu, out var data))
        {
            throw new ArgumentException($"MenuManager: Menu {menu} not found!");
        }

        if (data.Canvas == null)
        {
            throw new InvalidOperationException($"MenuManager: Canvas for menu {menu} is null!");
        }

        return data.Canvas.GetComponent<T>();
    }
}
