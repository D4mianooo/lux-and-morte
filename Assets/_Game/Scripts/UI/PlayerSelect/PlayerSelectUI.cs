using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectUI : MonoBehaviour
{
    #region Progress Bar Properties
    [Header("Shared Progress Bar")]
    [SerializeField]
    private Slider _p1Progress;

    [SerializeField]
    private Slider _p2Progress;

    [SerializeField]
    private Texture2D _lightRawImage;

    [SerializeField]
    private Texture2D _shadowRawImage;

    [SerializeField]
    private RawImage _p1CharacterRawImage;

    [SerializeField]
    private RawImage _p2CharacterRawImage;

    [SerializeField]
    private float _progressBouceBackDuration = 2f;

    [SerializeField]
    private Ease _progressBouceBackEase = Ease.OutBack;
    #endregion

    #region Player Indicator Properties
    [Header("Player Button indicators")]
    [SerializeField]
    private Image _p1R2Image;

    [SerializeField]
    private Image _p1L2Image;

    [SerializeField]
    private TextMeshProUGUI _p1ShadowText;

    [SerializeField]
    private TextMeshProUGUI _p1LightText;

    [SerializeField]
    private Image _p2R2Image;

    [SerializeField]
    private Image _p2L2Image;

    [SerializeField]
    private TextMeshProUGUI _p2ShadowText;

    [SerializeField]
    private TextMeshProUGUI _p2LightText;

    [SerializeField]
    private Color _colorInactive = new Color(0.75f, 0.75f, 0.75f);

    [SerializeField]
    private Color _colorActive = Color.white;

    [SerializeField]
    private float _indicatorTransitionDuration = 0.25f;

    [SerializeField]
    private Ease _indicatorTransitionEase = Ease.InOutSine;
    #endregion

    #region Player Name Input
    [SerializeField]
    private Transform _p1PlayerNameBar;

    [SerializeField]
    private Transform _p2PlayerNameBar;

    [SerializeField]
    private Transform _p1PlayerNameInput;

    [SerializeField]
    private Transform _p2PlayerNameInput;

    [Tooltip("Ease time for name input bar to fade in")]
    [SerializeField]
    private float _nameInputBarInEaseTime = 0.5f;

    [Tooltip("Ease type for name input bar to fade in")]
    [SerializeField]
    private Ease _nameInputBarInEase = Ease.InOutSine;

    [Tooltip("Ease type for name input bar to fade out")]
    [SerializeField]
    private float _nameInputBarOutEaseTime = 0.5f;

    [Tooltip("Ease type for name input bar to fade out")]
    [SerializeField]
    private Ease _nameInputBarOutEase = Ease.InBack;
    #endregion

    #region Player Ready Indicators
    [Header("Player Ready Indicators")]
    [SerializeField]
    private Transform _p1PlayerReadyButton;

    [SerializeField]
    private Transform _p2PlayerReadyButton;

    [SerializeField]
    private Transform _p1ReadyTextTransform;

    [SerializeField]
    private Transform _p2ReadyTextTransform;

    [SerializeField]
    private TextMeshProUGUI _p1Text;

    [SerializeField]
    private TextMeshProUGUI _p2Text;

    [SerializeField]
    private float _playerTextTransitionDuration = 0.5f;

    [SerializeField]
    private Ease _playerTextTransitionEase = Ease.InOutSine;

    [SerializeField]
    private float _playerReadyScaleDuration = 0.5f;

    [SerializeField]
    private Ease _playerReadyScaleEase = Ease.InOutSine;
    #endregion

    #region Button Prompts
    [Header("Button Prompts")]
    [Tooltip("Optional: Image component for P1 confirm button icon")]
    [SerializeField]
    private Image _p1ConfirmButtonIcon;

    [Tooltip("Optional: Image component for P2 confirm button icon")]
    [SerializeField]
    private Image _p2ConfirmButtonIcon;

    [Tooltip("Optional: Sprite for Xbox South button (A)")]
    [SerializeField]
    private Sprite _xboxSouthButtonSprite;

    [Tooltip("Optional: Sprite for PlayStation South button (X)")]
    [SerializeField]
    private Sprite _playstationSouthButtonSprite;

    [Tooltip("Optional: Sprite for Xbox L2 trigger (LT)")]
    [SerializeField]
    private Sprite _xboxL2TriggerSprite;

    [Tooltip("Optional: Sprite for PlayStation L2 trigger (L2)")]
    [SerializeField]
    private Sprite _playstationL2TriggerSprite;

    [Tooltip("Optional: Sprite for Xbox R2 trigger (RT)")]
    [SerializeField]
    private Sprite _xboxR2TriggerSprite;

    [Tooltip("Optional: Sprite for PlayStation R2 trigger (R2)")]
    [SerializeField]
    private Sprite _playstationR2TriggerSprite;
    #endregion

    #region Shake Animation
    [Header("Name Conflict Shake Animation")]
    [Tooltip("Duration of the shake animation in seconds")]
    [SerializeField]
    private float _nameConflictShakeDuration = 0.5f;

    [Tooltip("Intensity/strength of the shake animation")]
    [SerializeField]
    private float _nameConflictShakeStrength = 10f;

    [Tooltip("How much the shake will vibrate")]
    [SerializeField]
    private int _nameConflictShakeVibrato = 20;
    #endregion

    [Header("General")]
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private PlayerSwapUI _playerSwapUI;

    private Dictionary<PlayerManager.PlayerIndex, PlayerManager.PlayerRole> _playerRoles =
        new Dictionary<PlayerManager.PlayerIndex, PlayerManager.PlayerRole>();
    private Dictionary<PlayerManager.PlayerIndex, bool> _playerReady =
        new Dictionary<PlayerManager.PlayerIndex, bool>();
    private Dictionary<
        PlayerManager.PlayerIndex,
        PlayerSpawner.ControllerType
    > _playerControllerTypes =
        new Dictionary<PlayerManager.PlayerIndex, PlayerSpawner.ControllerType>();

    private Tween _p1Tween;
    private Tween _p2Tween;
    private Tween _p1ShakeTween;
    private Tween _p2ShakeTween;

    private PlayerManager.PlayerRole _p1IndicatorState = PlayerManager.PlayerRole.None;
    private PlayerManager.PlayerRole _p2IndicatorState = PlayerManager.PlayerRole.None;

    private void Start()
    {
        _p1ReadyTextTransform.gameObject.SetActive(false);
        _p2ReadyTextTransform.gameObject.SetActive(false);

        if (_p1PlayerNameBar != null)
            _p1PlayerNameBar.GetComponent<CanvasGroup>().alpha = 0f;
        if (_p2PlayerNameBar != null)
            _p2PlayerNameBar.GetComponent<CanvasGroup>().alpha = 0f;

        if (_p1PlayerReadyButton != null)
            _p1PlayerReadyButton.GetComponent<CanvasGroup>().alpha = 0f;
        if (_p2PlayerReadyButton != null)
            _p2PlayerReadyButton.GetComponent<CanvasGroup>().alpha = 0f;

        // Hide character images initially
        if (_p1CharacterRawImage != null)
            _p1CharacterRawImage.gameObject.SetActive(false);
        if (_p2CharacterRawImage != null)
            _p2CharacterRawImage.gameObject.SetActive(false);

        // Initialize progress bars
        if (_p1Progress != null)
        {
            _p1Progress.value = 0;
        }

        if (_p2Progress != null)
        {
            _p2Progress.value = 0;
        }

        // Initialize indicator visuals to neutral
        ApplyIndicatorStateInstant(0, PlayerManager.PlayerRole.None);
        ApplyIndicatorStateInstant(1, PlayerManager.PlayerRole.None);
    }

    private void OnEnable()
    {
        PlayerSpawner.OnRoleSelectionStarted += HandleRoleSelectionStarted;
        PlayerSpawner.OnRoleSelectionChanged += HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased += HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn += HandlePlayersReadyToSpawn;
        PlayerSpawner.OnPlayerNameChanged += HandlePlayerNameChanged;
        PlayerSpawner.OnControllerTypeDetected += HandleControllerTypeDetected;
        PlayerSpawner.OnPlayerNameConflict += HandlePlayerNameConflict;
    }

    private void OnDisable()
    {
        PlayerSpawner.OnRoleSelectionStarted -= HandleRoleSelectionStarted;
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
        PlayerSpawner.OnPlayerNameChanged -= HandlePlayerNameChanged;
        PlayerSpawner.OnControllerTypeDetected -= HandleControllerTypeDetected;
        PlayerSpawner.OnPlayerNameConflict -= HandlePlayerNameConflict;
    }

    private void OnDestroy()
    {
        PlayerSpawner.OnRoleSelectionStarted -= HandleRoleSelectionStarted;
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
        PlayerSpawner.OnPlayerNameChanged -= HandlePlayerNameChanged;
        PlayerSpawner.OnControllerTypeDetected -= HandleControllerTypeDetected;
        PlayerSpawner.OnPlayerNameConflict -= HandlePlayerNameConflict;
    }

    private void HandlePlayerNameConflict(
        object sender,
        PlayerSpawner.PlayerNameConflictEventArgs e
    )
    {
        if (e.PlayerIndex == PlayerManager.PlayerIndex.P1)
        {
            // Kill any existing shake tween to prevent interruption
            _p1ShakeTween?.Kill();

            // Store the original position before shaking
            Vector3 originalPosition = _p1PlayerNameBar.localPosition;

            // Create and track the shake tween
            _p1ShakeTween = _p1PlayerNameBar
                .DOShakePosition(
                    _nameConflictShakeDuration,
                    _nameConflictShakeStrength,
                    _nameConflictShakeVibrato
                )
                .OnComplete(() =>
                {
                    // Restore original position after shake completes
                    _p1PlayerNameBar.localPosition = originalPosition;
                    _p1ShakeTween = null;
                });
        }
        else if (e.PlayerIndex == PlayerManager.PlayerIndex.P2)
        {
            // Kill any existing shake tween to prevent interruption
            _p2ShakeTween?.Kill();

            // Store the original position before shaking
            Vector3 originalPosition = _p2PlayerNameBar.localPosition;

            // Create and track the shake tween
            _p2ShakeTween = _p2PlayerNameBar
                .DOShakePosition(
                    _nameConflictShakeDuration,
                    _nameConflictShakeStrength,
                    _nameConflictShakeVibrato
                )
                .OnComplete(() =>
                {
                    // Restore original position after shake completes
                    _p2PlayerNameBar.localPosition = originalPosition;
                    _p2ShakeTween = null;
                });
        }
    }

    private void HandleRoleSelectionStarted(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        UpdateRoleSelectionVisuals(e.PlayerIndex, e.PlayerRole);
    }

    private void HandleControllerTypeDetected(
        object sender,
        PlayerSpawner.ControllerTypeDetectedEventArgs e
    )
    {
        // Store the controller type
        _playerControllerTypes[e.PlayerIndex] = e.ControllerType;

        // Update confirm button icon based on controller type
        Image confirmButtonIcon =
            e.PlayerIndex == PlayerManager.PlayerIndex.P1
                ? _p1ConfirmButtonIcon
                : _p2ConfirmButtonIcon;

        if (confirmButtonIcon != null)
        {
            Sprite buttonSprite =
                e.ControllerType == PlayerSpawner.ControllerType.Xbox
                    ? _xboxSouthButtonSprite
                    : _playstationSouthButtonSprite;

            if (buttonSprite != null)
            {
                confirmButtonIcon.sprite = buttonSprite;
            }
        }

        // Update L2/R2 trigger icons based on controller type
        Image l2Icon = e.PlayerIndex == PlayerManager.PlayerIndex.P1 ? _p1L2Image : _p2L2Image;
        Image r2Icon = e.PlayerIndex == PlayerManager.PlayerIndex.P1 ? _p1R2Image : _p2R2Image;

        if (l2Icon != null)
        {
            Sprite l2Sprite =
                e.ControllerType == PlayerSpawner.ControllerType.Xbox
                    ? _xboxL2TriggerSprite
                    : _playstationL2TriggerSprite;

            if (l2Sprite != null)
            {
                l2Icon.sprite = l2Sprite;
            }
        }

        if (r2Icon != null)
        {
            Sprite r2Sprite =
                e.ControllerType == PlayerSpawner.ControllerType.Xbox
                    ? _xboxR2TriggerSprite
                    : _playstationR2TriggerSprite;

            if (r2Sprite != null)
            {
                r2Icon.sprite = r2Sprite;
            }
        }

        Debug.Log(
            $"Updated button icons for Player {e.PlayerIndex} to {e.ControllerType} controller layout"
        );
    }

    private void HandleRoleSelectionChanged(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        PlayerManager.PlayerIndex playerIndex = e.PlayerIndex;
        if (
            playerIndex != PlayerManager.PlayerIndex.P1
            && playerIndex != PlayerManager.PlayerIndex.P2
        )
            return;

        // Track which role this player is selecting
        _playerRoles[playerIndex] = e.PlayerRole;

        // Get the appropriate progress bar based on player
        Slider progressBar = null;
        RawImage characterImage = null;

        if (playerIndex == PlayerManager.PlayerIndex.P1)
        {
            progressBar = _p1Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.PlayerRole
            ];
            _p1Tween?.Kill();
            characterImage = _p1CharacterRawImage;
        }
        else if (playerIndex == PlayerManager.PlayerIndex.P2)
        {
            progressBar = _p2Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.PlayerRole
            ];
            _p2Tween?.Kill();
            characterImage = _p2CharacterRawImage;
        }

        // Update character image texture based on role
        if (characterImage != null)
        {
            characterImage.texture =
                e.PlayerRole == PlayerManager.PlayerRole.Light ? _lightRawImage : _shadowRawImage;
        }

        // Update progress bar
        if (progressBar != null)
        {
            progressBar.value = e.HoldProgress;

            // Position character image at the edge of the slider fill
            if (characterImage != null)
            {
                UpdateCharacterImagePosition(progressBar, characterImage, playerIndex);
            }
        }

        // Handle ready state
        Transform readyText =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1ReadyTextTransform
                : _p2ReadyTextTransform;
        Transform nameInputBar =
            playerIndex == PlayerManager.PlayerIndex.P1 ? _p1PlayerNameBar : _p2PlayerNameBar;
        Transform readyButton =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1PlayerReadyButton
                : _p2PlayerReadyButton;
        TextMeshProUGUI playerText =
            playerIndex == PlayerManager.PlayerIndex.P1 ? _p1Text : _p2Text;
        bool wasReady = _playerReady.ContainsKey(playerIndex) && _playerReady[playerIndex];

        // Show name input bar when confirmed but not yet ready
        if (
            e.IsConfirmed
            && !e.IsReady
            && nameInputBar != null
            && nameInputBar.GetComponent<CanvasGroup>().alpha == 0f
        )
        {
            nameInputBar
                .GetComponent<CanvasGroup>()
                .DOFade(1f, _nameInputBarInEaseTime)
                .SetEase(_nameInputBarInEase);
            readyButton
                .GetComponent<CanvasGroup>()
                .DOFade(1f, _nameInputBarInEaseTime)
                .SetEase(_nameInputBarInEase);
        }

        if (e.IsConfirmed && e.IsReady && readyButton.localScale != Vector3.zero)
        {
            readyButton.DOScale(0, _nameInputBarOutEaseTime).SetEase(_nameInputBarOutEase);
        }

        // Show ready text only when IsReady is true
        if (e.IsReady && !wasReady)
        {
            _playerReady[playerIndex] = true;
            if (readyText != null)
            {
                readyText.gameObject.SetActive(true);
                readyText.localScale = Vector3.zero;
                playerText
                    .DOColor(PlayerSpawner.RoleColors[e.PlayerRole], _playerTextTransitionDuration)
                    .SetEase(_playerTextTransitionEase);
                readyText
                    .DOScale(Vector3.one, _playerReadyScaleDuration)
                    .SetEase(_playerReadyScaleEase);
            }
        }

        if (!e.IsReady && wasReady)
        {
            _playerReady[playerIndex] = false;
            if (readyText != null)
            {
                readyText.gameObject.SetActive(false);
            }
        }
    }

    private void HandlePlayerNameChanged(object sender, PlayerSpawner.PlayerNameChangeEventArgs e)
    {
        Transform target =
            e.PlayerIndex == PlayerManager.PlayerIndex.P1 ? _p1PlayerNameInput : _p2PlayerNameInput;

        Debug.Log(
            $"Update name: {e.NewName} for Player {e.PlayerIndex}, child count: {target.childCount}"
        );

        if (target.childCount > e.NewName.Length)
        {
            // NewName is shorter than current - remove excess characters
            for (int i = target.childCount; i > e.NewName.Length; i--)
            {
                Destroy(target.GetChild(i - 1).gameObject);
            }
        }
        else if (target.childCount < e.NewName.Length)
        {
            // NewName is longer than current - add missing characters
            for (int i = target.childCount; i < e.NewName.Length; i++)
            {
                GameObject charObj = target.GetChild(0).gameObject;
                charObj.name = $"LetterInput_${i}";
                Instantiate(charObj, target);
            }
        }

        for (int i = 0; i < e.NewName.Length; i++)
        {
            if (i != e.NewName.Length - 1)
            {
                target.GetChild(i).GetChild(0).gameObject.SetActive(false);
                target.GetChild(i).GetChild(2).gameObject.SetActive(false);
            }
            else
            {
                target.GetChild(i).GetChild(0).gameObject.SetActive(true);
                target.GetChild(i).GetChild(2).gameObject.SetActive(true);
            }
            target.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = e.NewName[i]
                .ToString();
        }
    }

    private void UpdateCharacterImagePosition(
        Slider slider,
        RawImage characterImage,
        PlayerManager.PlayerIndex playerIndex
    )
    {
        if (slider == null || characterImage == null || slider.fillRect == null)
            return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        RectTransform characterRect = characterImage.GetComponent<RectTransform>();

        // Get the fill area width in world space
        float sliderWidth = sliderRect.rect.width;
        float fillWidth = sliderWidth * slider.value;

        float characterHalfWidth = characterRect.rect.width * 0.5f;
        float finalPosition;

        // P1 fills from left to right, P2 fills from right to left
        if (playerIndex == PlayerManager.PlayerIndex.P1)
        {
            float minX = -sliderWidth * sliderRect.pivot.x;
            float edgePosition = minX + fillWidth;
            finalPosition = edgePosition - characterHalfWidth;
        }
        else
        {
            float maxX = sliderWidth * (1 - sliderRect.pivot.x);
            float edgePosition = maxX - fillWidth;
            finalPosition = edgePosition + characterHalfWidth;
        }

        // Set the position and offset Y by half height to center on middle line
        float characterHalfHeight = characterRect.rect.height * 0.5f;
        Vector2 anchoredPos = characterRect.anchoredPosition;
        anchoredPos.x = finalPosition;
        anchoredPos.y = -characterHalfHeight;
        characterRect.anchoredPosition = anchoredPos;
    }

    private void UpdateRoleSelectionVisuals(
        PlayerManager.PlayerIndex playerIndex,
        PlayerManager.PlayerRole selectedRole
    )
    {
        // Show character image when selection starts
        RawImage characterImage =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1CharacterRawImage
                : _p2CharacterRawImage;
        if (characterImage != null && !characterImage.gameObject.activeSelf)
        {
            characterImage.gameObject.SetActive(true);
        }

        // Decide target states for both players based on who started selection and which role
        if (selectedRole == PlayerManager.PlayerRole.Light)
        {
            if (playerIndex == PlayerManager.PlayerIndex.P1)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Light);
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Skull);
            }
            else if (playerIndex == PlayerManager.PlayerIndex.P2)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Light);
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Skull);
            }
        }
        else
        {
            if (playerIndex == PlayerManager.PlayerIndex.P1)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Skull);
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Light);
            }
            else if (playerIndex == PlayerManager.PlayerIndex.P2)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Skull);
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Light);
            }
        }
    }

    void HandleRoleSelectionReleased(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        PlayerManager.PlayerIndex playerIndex = e.PlayerIndex;
        if (
            playerIndex != PlayerManager.PlayerIndex.P1
            && playerIndex != PlayerManager.PlayerIndex.P2
        )
            return;

        Transform readyText =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1ReadyTextTransform
                : _p2ReadyTextTransform;
        // Reset the progress bar for the released role
        if (playerIndex == PlayerManager.PlayerIndex.P1 && _p1Progress != null)
        {
            _p1Tween?.Kill();
            _p1Tween = DOTween
                .To(
                    () => _p1Progress.value,
                    x => _p1Progress.value = x,
                    0f,
                    _progressBouceBackDuration * _p1Progress.value // Scale duration based on current value
                )
                .SetEase(_progressBouceBackEase)
                .OnUpdate(() =>
                {
                    if (_p1CharacterRawImage != null && _p1CharacterRawImage.gameObject.activeSelf)
                    {
                        UpdateCharacterImagePosition(
                            _p1Progress,
                            _p1CharacterRawImage,
                            playerIndex
                        );
                    }
                });
        }
        else if (playerIndex == PlayerManager.PlayerIndex.P2 && _p2Progress != null)
        {
            _p2Tween?.Kill();
            _p2Tween = DOTween
                .To(
                    () => _p2Progress.value,
                    x => _p2Progress.value = x,
                    0f,
                    _progressBouceBackDuration * _p2Progress.value // Scale duration based on current value
                )
                .SetEase(_progressBouceBackEase)
                .OnUpdate(() =>
                {
                    if (_p2CharacterRawImage != null && _p2CharacterRawImage.gameObject.activeSelf)
                    {
                        UpdateCharacterImagePosition(
                            _p2Progress,
                            _p2CharacterRawImage,
                            playerIndex
                        );
                    }
                });
        }

        // Clear ready state
        _playerReady[playerIndex] = false;
        if (readyText != null)
        {
            readyText.gameObject.SetActive(false);
        }

        // Clear tracked role
        if (_playerRoles.ContainsKey(playerIndex))
        {
            _playerRoles.Remove(playerIndex);
        }

        // Refresh visuals: if the other player is still selecting, reflect their state;
        // otherwise clear indicator visuals to neutral
        PlayerManager.PlayerIndex other = (PlayerManager.PlayerIndex)(((int)playerIndex % 2) + 1);
        if (_playerRoles.ContainsKey(other))
        {
            UpdateRoleSelectionVisuals(other, _playerRoles[other]);
        }
        else
        {
            // No one selecting — reset to neutral
            SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.None);
            SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.None);
        }
    }

    private void HandlePlayersReadyToSpawn(object sender, EventArgs e)
    {
        Sequence s = DOTween.Sequence();
        s.AppendInterval(3f);

        if (_canvasGroup != null)
        {
            // Fade out the entire UI if canvas group is assigned
            s.Append(
                _canvasGroup
                    .DOFade(0f, 0.5f)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    })
            );
        }
        else
        {
            // If not, just deactivate after delay
            s.InsertCallback(
                3f,
                () =>
                {
                    gameObject.SetActive(false);
                }
            );
        }

        s.AppendCallback(() =>
        {
            // Show the initial role assignment for 3 seconds, then start the match
            if (_playerSwapUI != null)
            {
                // Get Player 1's role from PlayerSpawner
                PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();

                // Spawn players before starting the match
                if (GameManager.Instance != null && GameManager.Instance.PlayerSpawner != null)
                {
                    GameManager.Instance.PlayerSpawner.ExecutePlayerSpawn();
                }

                _playerSwapUI.ShowInitialAssignment(
                    displayDuration: 3f,
                    onComplete: () =>
                    {
                        // Start the match countdown after displaying roles
                        if (GameManager.Instance != null && GameManager.Instance.Timer != null)
                        {
                            GameManager.Instance.Timer.StartMatch();
                        }
                    }
                );
            }
        });

        s.Play();
    }

    private void SetIndicatorState(
        PlayerManager.PlayerIndex playerIndex,
        PlayerManager.PlayerRole newState
    )
    {
        if (playerIndex == PlayerManager.PlayerIndex.P1)
        {
            if (_p1IndicatorState == newState)
                return;

            _p1IndicatorState = newState;
            switch (newState)
            {
                case PlayerManager.PlayerRole.Light:
                    _p1L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1R2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1ShadowText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerManager.PlayerRole.Skull:
                    _p1L2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1LightText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerManager.PlayerRole.None:
                default:
                    _p1L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
            }
        }
        else if (playerIndex == PlayerManager.PlayerIndex.P2)
        {
            if (_p2IndicatorState == newState)
                return;

            _p2IndicatorState = newState;
            switch (newState)
            {
                case PlayerManager.PlayerRole.Light:
                    _p2L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2R2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2ShadowText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerManager.PlayerRole.Skull:
                    _p2L2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2LightText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerManager.PlayerRole.None:
                default:
                    _p2L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
            }
        }
    }

    private void ApplyIndicatorStateInstant(int playerIndex, PlayerManager.PlayerRole state)
    {
        Color lightColor = _colorActive;
        Color shadowColor = _colorActive;

        if (state == PlayerManager.PlayerRole.Light)
        {
            lightColor = _colorActive;
            shadowColor = _colorInactive;
        }
        else if (state == PlayerManager.PlayerRole.Skull)
        {
            lightColor = _colorInactive;
            shadowColor = _colorActive;
        }

        if (playerIndex == 0)
        {
            _p1L2Image.color = lightColor;
            _p1LightText.color = lightColor;
            _p1R2Image.color = shadowColor;
            _p1ShadowText.color = shadowColor;
            _p1IndicatorState = state;
        }
        else if (playerIndex == 1)
        {
            _p2L2Image.color = lightColor;
            _p2LightText.color = lightColor;
            _p2R2Image.color = shadowColor;
            _p2ShadowText.color = shadowColor;
            _p2IndicatorState = state;
        }
    }
}
