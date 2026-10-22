using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Settings : MenuBase
{
    [SerializeField]
    private UnityEngine.UI.Slider _sliderMaster;

    [SerializeField]
    private UnityEngine.UI.Slider _sliderSFX;

    [SerializeField]
    private UnityEngine.UI.Slider _sliderBGM;

    [SerializeField]
    private UnityEngine.UI.Slider _sliderUI;

    [SerializeField]
    private TMP_Dropdown _resolutionDropdown;

    [SerializeField]
    private UnityEngine.UI.Toggle _toggleFullscreen;

    [SerializeField]
    private UnityEngine.UI.Toggle _toggleEasyMovement;

    [SerializeField]
    private Sprite _toggleBackgroundOn;

    [SerializeField]
    private Sprite _toggleBackgroundOff;

    [SerializeField]
    private UnityEngine.UI.Image _toggleEasyMovementBackground;

    [SerializeField]
    private UnityEngine.UI.Image _toggleFullscreenBackground;


    private Resolution[] _resolutions;
    private int _currentResolutionIndex;

    private void Start()
    {
        _sliderMaster.value = AudioManager.Instance.MasterVolume;
        _sliderSFX.value = AudioManager.Instance.SFXVolume;
        _sliderBGM.value = AudioManager.Instance.MusicVolume;
        _sliderUI.value = AudioManager.Instance.UIVolume;
        _toggleEasyMovement.isOn = PlayerPrefs.GetInt("EasyMovement", 0) == 1;
        _toggleFullscreen.isOn =
            PlayerPrefs.GetInt("FullscreenMode", 0) == (int)FullScreenMode.ExclusiveFullScreen;
        _resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", 0);

        UpdateToggleBackground(_toggleEasyMovement, _toggleEasyMovementBackground);
        UpdateToggleBackground(_toggleFullscreen, _toggleFullscreenBackground);

        SetupResolutionDropdown();
    }

    private void SetupResolutionDropdown()
    {
        if (_resolutionDropdown == null)
            return;

        // Get all available resolutions
        _resolutions = Screen.resolutions;

        // Clear existing options
        _resolutionDropdown.ClearOptions();

        // Create list of resolution strings
        System.Collections.Generic.List<string> options =
            new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option =
                _resolutions[i].width
                + "x"
                + _resolutions[i].height
                + " @ "
                + _resolutions[i].refreshRateRatio.value.ToString("F0")
                + "Hz";
            options.Add(option);

            // Check if this is the current resolution
            if (
                _resolutions[i].width == Screen.width
                && _resolutions[i].height == Screen.height
                && Mathf.Approximately(
                    (float)_resolutions[i].refreshRateRatio.value,
                    (float)Screen.currentResolution.refreshRateRatio.value
                )
            )
            {
                currentResolutionIndex = i;
            }
        }

        // Add options to dropdown
        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.value = currentResolutionIndex;
        _resolutionDropdown.RefreshShownValue();
        _currentResolutionIndex = currentResolutionIndex;
    }

    public void OnResolutionChange()
    {
        if (_resolutionDropdown == null)
            return;

        _currentResolutionIndex = _resolutionDropdown.value;
        Resolution resolution = _resolutions[_currentResolutionIndex];
        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreenMode,
            resolution.refreshRateRatio
        );
    }

    public void OnFullscreenToggle()
    {
        Screen.fullScreenMode = _toggleFullscreen.isOn
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.Windowed;

        UpdateToggleBackground(_toggleFullscreen, _toggleFullscreenBackground);
    }

    public void OnEasyMovementToggle()
    {
        UpdateToggleBackground(_toggleEasyMovement, _toggleEasyMovementBackground);
    }

    public void OnSliderChange(string type)
    {
        switch (type)
        {
            case "Master":
                AudioManager.Instance.MasterVolume = _sliderMaster.value;
                break;
            case "SFX":
                AudioManager.Instance.SFXVolume = _sliderSFX.value;
                break;
            case "BGM":
                AudioManager.Instance.MusicVolume = _sliderBGM.value;
                break;
            case "UI":
                AudioManager.Instance.UIVolume = _sliderUI.value;
                break;
        }
    }

    public void OnBtnSave()
    {
        AudioManager.Instance.SaveSettings();
        PlayerPrefs.SetInt("ResolutionIndex", _currentResolutionIndex);
        PlayerPrefs.SetInt("FullscreenMode", (int)Screen.fullScreenMode);
        PlayerPrefs.SetInt("EasyMovement", _toggleEasyMovement.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnBtnBack()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.CloseLastSubMenu();
        }
    }

    private void UpdateToggleBackground(UnityEngine.UI.Toggle toggle, UnityEngine.UI.Image background)
    {
        if (toggle.isOn)
            background.sprite = _toggleBackgroundOn;
        else
            background.sprite = _toggleBackgroundOff;
    }

}
