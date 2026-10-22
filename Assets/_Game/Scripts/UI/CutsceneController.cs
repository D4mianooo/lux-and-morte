using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;
using UnityEngine.InputSystem;

public class CutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class TextObject
    {
        public string text;
        public float time;
    }

    [System.Serializable]
    public class MovingObject
    {
        public RectTransform rect;
        public MoveType moveType;
        public float moveDistance = 300f;

        [Header("Fade Settings")]
        public float fadeStartTime = 0f;
        public float fadeDuration = 1f;

        [HideInInspector] public Vector2 startPos;

        public UnityEngine.UI.Image image;
    }

    public enum MoveType
    {
        Static,
        UpToDown,
        DownToUp
    }

    public UnityEvent OnCutsceneFinished;
    public List<MovingObject> objects;
    public List<TextObject> textObjects;
    public float duration = 3f;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI skipText;
    public float typingSpeed = 0.03f;

    private bool isPlaying = false;
    public float frameInterval = 0.05f;

    public InputActionReference skipAction;

    private void Awake()
    {
        foreach (var obj in objects)
        {
            if (obj.rect != null)
                obj.startPos = obj.rect.anchoredPosition;
        }
    }

    [ContextMenu("Start Cutscene")]
    public void StartCutscene()
    {
        if (!isPlaying)
        {
            dialogueText.gameObject.SetActive(false);
            skipText.gameObject.SetActive(false);

            foreach (var obj in objects)
            {

                if (obj.image != null)
                {
                    Color c = obj.image.color;
                    c.a = 0f;
                    obj.image.color = c;
                }
            }

            ResetObjectsPosition();

            StartCoroutine(PlayCutscene());
            StartCoroutine(PlayTextSequence());
        }
            
    }

    private IEnumerator PlayCutscene()
    {
        isPlaying = true;

        foreach (var obj in objects)
        {
            if (obj.rect == null) continue;

            //obj.startPos = obj.rect.anchoredPosition;

            if (obj.image != null)
            {
                Color c = obj.image.color;
                c.a = 0f;
                obj.image.color = c;
            }

            float time = 0f;

            while (time < obj.fadeDuration)
            {
                float t = time / obj.fadeDuration;
                float easedT = Mathf.SmoothStep(0, 1, t);

                Vector2 targetOffset = Vector2.zero;

                switch (obj.moveType)
                {
                    case MoveType.UpToDown:
                        targetOffset = new Vector2(0, -obj.moveDistance);
                        break;

                    case MoveType.DownToUp:
                        targetOffset = new Vector2(0, obj.moveDistance);
                        break;
                }

                obj.rect.anchoredPosition =
                    obj.startPos + targetOffset * easedT;

                if (obj.image != null)
                {
                    Color c = obj.image.color;
                    c.a = easedT;
                    obj.image.color = c;
                }

                time += Time.deltaTime;
                yield return new WaitForSeconds(frameInterval);
            }
        }

        isPlaying = false;

        OnCutsceneFinished?.Invoke();
    }

    private IEnumerator PlayTextSequence()
    {
        yield return new WaitForSeconds(4f);

        dialogueText.text = "";
        dialogueText.gameObject.SetActive(true);
        skipText.gameObject.SetActive(true);

        if (dialogueText == null || textObjects.Count == 0)
            yield break;

        foreach (var textObj in textObjects)
        {
            dialogueText.text = "";

            foreach (char letter in textObj.text)
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(textObj.time);
        }

        dialogueText.text = "";
    }

    public void OnCutsceneFinish()
    {
        if (MenuManager.Instance != null)
        {
            // Load game scene with loading screen, close all menus, don't open any menu
            MenuManager.Instance.LoadScene(
                MenuManager.Scene.Game,
                menuToOpen: null,
                resetGameState: false,
                onComplete: () =>
                {
                    AudioManager.Instance.PlaySound(
                        AudioManager.Instance.FMODEvents.Music.Music8Bit
                    );

                    GameManager.Instance.EasyMovementEnabled =
                        PlayerPrefs.GetInt("EasyMovement", 0) == 1;
                }
            );
        }

        StartCoroutine(OnCutsceneFinishCor());
    }

    private IEnumerator OnCutsceneFinishCor()
    {
        yield return new WaitForSeconds(1.0f);

        this.gameObject.SetActive(false);

        foreach (var obj in objects)
        {
            if (obj == objects[objects.Count - 1])
                continue;

            if (obj.image != null)
            {
                Color c = obj.image.color;
                c.a = 0f;
                obj.image.color = c;

            }
        }
    }

    private void OnEnable()
    {
        if (skipAction != null)
            skipAction.action.performed += OnSkipPerformed;

        skipAction?.action.Enable();
    }

    private void OnDisable()
    {
        if (skipAction != null)
            skipAction.action.performed -= OnSkipPerformed;

        skipAction?.action.Disable();
    }

    private void OnSkipPerformed(InputAction.CallbackContext context)
    {
        if (!isPlaying) return;

        StopAllCoroutines();
        isPlaying = false;

        foreach (var obj in objects)
        {
            if (obj.rect == null) continue;

            Vector2 finalOffset = Vector2.zero;

            switch (obj.moveType)
            {
                case MoveType.UpToDown:
                    finalOffset = new Vector2(0, -obj.moveDistance);
                    break;
                case MoveType.DownToUp:
                    finalOffset = new Vector2(0, obj.moveDistance);
                    break;
            }

            obj.rect.anchoredPosition = obj.startPos;

            if (obj.image != null)
            {
                Color c = obj.image.color;
                c.a = 1f;
                obj.image.color = c;
            }
        }

        dialogueText.gameObject.SetActive(true);
        skipText.gameObject.SetActive(false);

        if (MenuManager.Instance != null)
        {
            // Load game scene with loading screen, close all menus, don't open any menu
            MenuManager.Instance.LoadScene(
                MenuManager.Scene.Game,
                menuToOpen: null,
                resetGameState: false,
                onComplete: () =>
                {
                    AudioManager.Instance.PlaySound(
                        AudioManager.Instance.FMODEvents.Music.Music8Bit
                    );

                    GameManager.Instance.EasyMovementEnabled =
                        PlayerPrefs.GetInt("EasyMovement", 0) == 1;
                }
            );
        }

        StartCoroutine(OnCutsceneFinishCor());
    }

    private void ResetObjectsPosition()
    {
        foreach (var obj in objects)
        {
            if (obj.rect == null) continue;

            obj.rect.anchoredPosition = obj.startPos;

            if (obj.image != null)
            {
                Color c = obj.image.color;
                c.a = 0f; 
                obj.image.color = c;
            }
        }
    }
}