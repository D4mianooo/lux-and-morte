using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class PixelPerfectUIHandler : MonoBehaviour
{
    public Vector2 referenceResolution = new Vector2(960, 540); // Your 16:9 base pixel size

    [Header("Fill Panels (Letterbox/Pillarbox)")]
    public RectTransform leftPanel;
    public RectTransform rightPanel;
    public RectTransform topPanel;
    public RectTransform bottomPanel;

    void Update()
    {
        RectTransform rt = GetComponent<RectTransform>();
        CanvasScaler cs = GetComponent<CanvasScaler>();

        // 1. Calculate the integer scale
        int scaleX = Screen.width / (int)referenceResolution.x;
        int scaleY = Screen.height / (int)referenceResolution.y;
        int targetScale = Mathf.Max(1, Mathf.Min(scaleX, scaleY));

        cs.scaleFactor = targetScale;

        // 2. Position the fill panels to cover only the outer areas
        // Calculate in canvas space (reference resolution space), not screen pixels
        float canvasWidth = Screen.width / targetScale;
        float canvasHeight = Screen.height / targetScale;

        float horizontalPadding = (canvasWidth - referenceResolution.x) / 2f;
        float verticalPadding = (canvasHeight - referenceResolution.y) / 2f;

        // Left panel
        if (leftPanel != null && horizontalPadding > 0)
        {
            leftPanel.gameObject.SetActive(true);
            leftPanel.anchorMin = new Vector2(0, 0);
            leftPanel.anchorMax = new Vector2(0, 1);
            leftPanel.pivot = new Vector2(0, 0.5f);
            leftPanel.anchoredPosition = Vector2.zero;
            leftPanel.sizeDelta = new Vector2(horizontalPadding, 0);
        }
        else if (leftPanel != null)
            leftPanel.gameObject.SetActive(false);

        // Right panel
        if (rightPanel != null && horizontalPadding > 0)
        {
            rightPanel.gameObject.SetActive(true);
            rightPanel.anchorMin = new Vector2(1, 0);
            rightPanel.anchorMax = new Vector2(1, 1);
            rightPanel.pivot = new Vector2(1, 0.5f);
            rightPanel.anchoredPosition = Vector2.zero;
            rightPanel.sizeDelta = new Vector2(horizontalPadding, 0);
        }
        else if (rightPanel != null)
            rightPanel.gameObject.SetActive(false);

        // Top panel
        if (topPanel != null && verticalPadding > 0)
        {
            topPanel.gameObject.SetActive(true);
            topPanel.anchorMin = new Vector2(0, 1);
            topPanel.anchorMax = new Vector2(1, 1);
            topPanel.pivot = new Vector2(0.5f, 1);
            topPanel.anchoredPosition = Vector2.zero;
            topPanel.sizeDelta = new Vector2(0, verticalPadding);
        }
        else if (topPanel != null)
            topPanel.gameObject.SetActive(false);

        // Bottom panel
        if (bottomPanel != null && verticalPadding > 0)
        {
            bottomPanel.gameObject.SetActive(true);
            bottomPanel.anchorMin = new Vector2(0, 0);
            bottomPanel.anchorMax = new Vector2(1, 0);
            bottomPanel.pivot = new Vector2(0.5f, 0);
            bottomPanel.anchoredPosition = Vector2.zero;
            bottomPanel.sizeDelta = new Vector2(0, verticalPadding);
        }
        else if (bottomPanel != null)
            bottomPanel.gameObject.SetActive(false);
    }
}
