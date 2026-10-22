using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class ShadowIndicatorSlot
{
    public int id;
    public ShadowIndicator indicator;
    public Image baseImage;
    public Image faceImage;
}

[System.Serializable]
public class ShadowIndicatorInitialState
{
    public int slotId;
    public ShadowIndicator indicator;
    public Vector3 position;
}

[System.Serializable]
public class ShadowIndicatorDebugInfo
{
    public int slotId;
    public Color currentColor;
}

/// <summary>
/// Handles the UI for switching between shadows (ghosts).
/// Shows indicators for active and switchable shadows, and flashes buttons when a switch occurs.
/// </summary>
public class ShadowSwitchUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI Indicators")]
    [SerializeField]
    private List<ShadowIndicatorSlot> _shadowIndicators;
    private List<ShadowIndicatorSlot> _initialSlotsOrder;
    private List<ShadowIndicatorInitialState> _initialState;

    [Header("UI Controlers")]
    [SerializeField]
    private GameObject _l1;

    [SerializeField]
    private GameObject _r1;

    [Header("Sprites")]
    [SerializeField]
    private Sprite _defaultBaseSprite;
    [SerializeField]
    private Sprite _l1BaseSprite;
    [SerializeField]
    private Sprite _r1BaseSprite;
    [SerializeField]
    private Sprite _defaultFaceSprite;
    [SerializeField]
    private Sprite _l1FaceSprite;
    [SerializeField]
    private Sprite _r1FaceSprite;

    [Header("DEBUG")]
    [SerializeField]
    private List<ShadowIndicatorDebugInfo> _debugIndicators;
    #endregion

    #region Properties
    private bool _isSubscribed = false;
    private bool _isShadowLinked = false;
    private bool _isRotatingIndicators = false;
    private ShadowPlayerController playerController;

    private enum RotationVisual
    {
        Base,
        Left,
        Right
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _initialSlotsOrder = new List<ShadowIndicatorSlot>(_shadowIndicators);
        CacheInitialState();
    }

    private void Start()
    {
        GameManager.Instance.Timer.OnRoundEnded += Timer_OnRoundEnd;
    }

    /// <summary>
    /// Called when the round ends.
    /// Restores the shadow indicator UI to its initial state by:
    /// - resetting indicator positions instantly,
    /// - restoring the original slot order,
    /// - clearing all linked shadow references,
    /// - allowing shadows to be re-linked in the next round.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void Timer_OnRoundEnd(object sender, System.EventArgs e)
    {
        ResetIndicatorsInstant();

        _shadowIndicators.Clear();
        _shadowIndicators.AddRange(_initialSlotsOrder);

        foreach (var slot in _shadowIndicators)
        {
            if (slot?.indicator != null)
                slot.indicator.LinkedShadow = null;
        }

        _isShadowLinked = false;

        SetIndicatorsVisual(RotationVisual.Base);
    }

    /// <summary>
    /// Caches the initial state of all shadow indicators.
    /// Stores each indicator's slot ID, reference, and starting position
    /// so the UI can be correctly restored after rotations or round resets.
    /// </summary>
    private void CacheInitialState()
    {
        _initialState = new List<ShadowIndicatorInitialState>();

        foreach (var slot in _shadowIndicators)
        {
            if (slot.indicator == null)
                continue;

            _initialState.Add(
                new ShadowIndicatorInitialState
                {
                    slotId = slot.id,
                    indicator = slot.indicator,
                    position = slot.indicator.transform.position,
                }
            );
        }
    }

    /// <summary>
    /// Per-frame check: subscribes to shadow events once the ShadowPlayerController is available.
    /// </summary>
    private void Update()
    {
        UpdateDebugInfo();

        if (_isSubscribed && _isShadowLinked)
            return;

        PlayerManager player = GameManager.Instance.PlayerManager;
        if (player == null)
            return;

        ShadowPlayerController playerController = player
            .GetPlayerOfType(PlayerManager.PlayerRole.Skull)
            ?.GetComponentInChildren<ShadowPlayerController>();
        if (playerController != null && !_isSubscribed)
        {
            _isSubscribed = true;

            this.playerController = playerController;
            playerController.OnLBSwitchEvent += OnLBSwitch;
            playerController.OnRBSwitchEvent += OnRBSwitch;

            playerController.OnActiveRandomSwitch += PlayerController_OnActiveRandomSwitch;
        }

        if (playerController != null && !_isShadowLinked)
        {
            if(playerController.Shadows != null && playerController.Shadows.Count == 3)
            {
                GetSlotById(0).indicator.LinkedShadow = playerController.Shadows[2]; 
                GetSlotById(1).indicator.LinkedShadow = playerController.Shadows[0]; 
                GetSlotById(2).indicator.LinkedShadow = playerController.Shadows[1]; 

                Debug.Log("Is linked");

                _isShadowLinked = true;
            }
        }
    }

    /// <summary>
    /// Updates runtime debug information for shadow indicators.
    /// Collects the current visual state of each indicator (slot ID and color)
    /// to assist with debugging and inspector visualization.
    /// This method is intended for development and debugging purposes only.
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (_shadowIndicators == null)
            return;

        if (_debugIndicators == null)
            _debugIndicators = new List<ShadowIndicatorDebugInfo>();

        _debugIndicators.Clear();

        foreach (var slot in _shadowIndicators)
        {
            if (slot == null || slot.indicator == null)
                continue;

            _debugIndicators.Add(
                new ShadowIndicatorDebugInfo
                {
                    slotId = slot.id,
                    currentColor = slot.indicator.GetColor(),
                }
            );
        }
    }

    /// <summary>
    /// Handles a random active shadow switch triggered by the player controller.
    /// Determines which UI indicator represents the newly active shadow
    /// based on its color, then rotates the indicator list so that the
    /// active shadow is positioned in the middle slot.
    /// </summary>
    private void PlayerController_OnActiveRandomSwitch()
    {
        Color activeColor = playerController.ASwitchColor;

        int activeSlotIndex = -1;

        for (int i = 0; i < _shadowIndicators.Count; i++)
        {
            if (_shadowIndicators[i].indicator.GetColor() == activeColor)
            {
                activeSlotIndex = i;
                break;
            }
        }

        if (activeSlotIndex == -1)
        {
            Debug.LogWarning("Active shadow not found in UI indicators");
            return;
        }

        int steps = 1 - activeSlotIndex;

        if (steps == 0)
            return;

        RotateIndicators(steps > 0 ? +1 : -1);
    }
    #endregion

    #region Buttons
    /// <summary>
    /// Called when the player triggers the left shoulder (LB) switch action.
    /// Initiates a rotation of the shadow indicators to the left until
    /// the active shadow is positioned in the middle slot,
    /// and briefly flashes the corresponding UI button.
    /// </summary>
    private void OnLBSwitch()
    {
        if (_isRotatingIndicators) return;
        StartCoroutine(TryRotateUntilMiddleActive(+1, _l1));
    }

    /// <summary>
    /// Called when the player triggers the right shoulder (RB) switch action.
    /// Initiates a rotation of the shadow indicators to the right until
    /// the active shadow is positioned in the middle slot,
    /// and briefly flashes the corresponding UI button.
    /// </summary>
    private void OnRBSwitch()
    {
        if (_isRotatingIndicators) return;
        StartCoroutine(TryRotateUntilMiddleActive(-1, _r1));
    }

    /// <summary>
    /// Attempts to rotate the shadow indicator UI in the given direction until
    /// the active shadow is placed in the middle slot.
    /// The rotation is retried up to a maximum number of attempts to prevent
    /// infinite loops, and safely validates all references before checking
    /// the active shadow state.
    /// </summary>
    /// <param name="direction">
    /// Rotation direction:
    /// +1 rotates indicators to the left,
    /// -1 rotates indicators to the right.
    /// </param>
    /// <param name="button">
    /// UI button to flash while the rotation is being attempted.
    /// </param>
    private IEnumerator TryRotateUntilMiddleActive(int direction, GameObject button)
    {
        SetIndicatorsVisual(direction > 0
            ? RotationVisual.Left
            : RotationVisual.Right);

        _isRotatingIndicators = true;
        int maxAttempts = _shadowIndicators.Count;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            RotateIndicators(direction);
            AnimateIndicatorsDuringRotation();
            StartCoroutine(FlashButton(button, 0.3f));

            yield return new WaitForSeconds(0.3f); 

            attempts++;

            if (_shadowIndicators.Count > 1)
            {
                var middleSlot = _shadowIndicators[1];
                if (middleSlot != null && middleSlot.indicator != null && middleSlot.indicator.LinkedShadow != null)
                {
                    var shadowController = middleSlot.indicator.LinkedShadow.GetComponent<ShadowController>();
                    if (shadowController != null && shadowController.IsShadowActive)
                    {
                        yield return new WaitForSeconds(0.1f);
                        SetIndicatorsVisual(RotationVisual.Base);
                        AnimateMiddleIndicatorImpact();
                        _isRotatingIndicators = false;
                        yield break;
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.2f);
        SnapIndicatorsToNearestBackup();
        SetIndicatorsVisual(RotationVisual.Base);
        _isRotatingIndicators = false;
    }

    /// <summary>
    /// Rotates the shadow indicator UI elements in the specified direction.
    /// Visually animates the indicators to their new positions using DOTween,
    /// then updates the internal slot order to reflect the rotation.
    /// </summary>
    /// <param name="direction">
    /// Rotation direction:
    /// +1 rotates indicators forward,
    /// -1 rotates indicators backward.
    /// </param>
    private void RotateIndicators(int direction)
    {
        float duration = 0.25f;

        int count = _shadowIndicators.Count;
        if (count <= 1)
            return;

        ShadowIndicatorSlot[] rotatedSlots = new ShadowIndicatorSlot[count];
        Vector3[] targetPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            int targetIndex = (i + direction + count) % count;
            rotatedSlots[targetIndex] = _shadowIndicators[i];
            targetPositions[targetIndex] = _shadowIndicators[i].indicator.transform.position;
        }

        for (int i = 0; i < count; i++)
        {
            int targetIndex = (i - direction + count) % count; 
            _shadowIndicators[i].indicator.transform.DOMove(targetPositions[targetIndex], duration)
                .SetEase(Ease.InOutCubic);
        }

        for (int i = 0; i < count; i++)
        {
            _shadowIndicators[i] = rotatedSlots[i];
        }
    }

    /// <summary>
    /// Coroutine to flash a UI button for a short duration.
    /// </summary>
    /// <param name="button">The button GameObject to flash.</param>
    /// <param name="duration">How long the button stays visible.</param>
    private IEnumerator FlashButton(GameObject button, float duration)
    {
        if (button == null)
            yield break;

        button.SetActive(true);
        yield return new WaitForSeconds(duration);
        button.SetActive(false);
    }
    #endregion

    /// <summary>
    /// Returns the shadow indicator slot with the given ID.
    /// </summary>
    private ShadowIndicatorSlot GetSlotById(int id)
    {
        return _shadowIndicators.Find(s => s.id == id);
    }

    /// <summary>
    /// Resets all shadow indicators to their initial positions using animation.
    /// </summary>
    public void ResetIndicators()
    {
        if (_initialState == null || _initialState.Count == 0)
            return;

        float duration = 0.25f;

        foreach (var state in _initialState)
        {
            ShadowIndicatorSlot slot = GetSlotById(state.slotId);
            if (slot == null)
                continue;

            slot.indicator = state.indicator;

            state.indicator.transform.DOKill();
            state.indicator.transform.DOMove(state.position, duration).SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// Instantly resets all shadow indicators to their initial positions.
    /// </summary>
    public void ResetIndicatorsInstant()
    {
        foreach (var state in _initialState)
        {
            ShadowIndicatorSlot slot = GetSlotById(state.slotId);
            if (slot == null)
                continue;

            slot.indicator = state.indicator;
            state.indicator.transform.position = state.position;
        }
    }

    /// <summary>
    /// Ensures all shadow indicators are snapped to the nearest backup positions
    /// after a rotation or any manual movement.
    /// Each indicator will align to the closest position stored in _initialState.
    /// </summary>
    private void SnapIndicatorsToNearestBackup()
    {
        if (_initialState == null || _initialState.Count == 0)
            return;

        float snapThreshold = 0.1f; 
        float snapDuration = 0.2f; 

        foreach (var slot in _shadowIndicators)
        {
            if (slot?.indicator == null)
                continue;

            Vector3 currentPos = slot.indicator.transform.position;
            Vector3 nearestPos = _initialState[0].position;
            float minDist = Vector3.Distance(currentPos, nearestPos);

            foreach (var state in _initialState)
            {
                float dist = Vector3.Distance(currentPos, state.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestPos = state.position;
                }
            }

            if (minDist > snapThreshold)
            {
                slot.indicator.transform.DOKill();
                slot.indicator.transform.DOMove(nearestPos, snapDuration).SetEase(Ease.OutCubic);
            }
        }
    }

    private void SetIndicatorsVisual(RotationVisual visual)
    {
        Sprite baseSprite;
        Sprite faceSprite;

        switch (visual)
        {
            case RotationVisual.Left:
                baseSprite = _l1BaseSprite;
                faceSprite = _l1FaceSprite;
                break;

            case RotationVisual.Right:
                baseSprite = _r1BaseSprite;
                faceSprite = _r1FaceSprite;
                break;

            default:
                baseSprite = _defaultBaseSprite;
                faceSprite = _defaultFaceSprite;
                break;
        }

        foreach (var slot in _shadowIndicators)
        {
            if (slot == null)
                continue;

            if (slot.baseImage != null)
                slot.baseImage.sprite = baseSprite;

            if (slot.faceImage != null)
                slot.faceImage.sprite = faceSprite;
        }
    }

    private void AnimateIndicatorsDuringRotation()
    {
        foreach (var slot in _shadowIndicators)
        {
            if (slot?.indicator == null)
                continue;

            Transform t = slot.indicator.transform;

            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScale(0.75f, 0.25f).SetEase(Ease.OutQuad));
            seq.Join(t.DORotate(new Vector3(0, 0, Random.Range(-12f, 12f)), 0.18f));
            seq.Append(t.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            seq.Join(t.DORotate(Vector3.zero, 0.25f));
        }
    }

    private void AnimateMiddleIndicatorImpact()
    {
        if (_shadowIndicators.Count < 2)
            return;

        Transform t = _shadowIndicators[1].indicator.transform;
        t.DOKill();
        t.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOScale(1.25f, 0.12f));
        seq.Append(t.DOScale(1f, 0.22f).SetEase(Ease.OutBounce));
        seq.Join(t.DOPunchRotation(new Vector3(0, 0, 12f), 0.2f));
    }
}
