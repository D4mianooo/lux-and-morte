using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PowerUp : MonoBehaviour
{
    [Header("Ustawienia pulsowania")]
    [SerializeField] private float pulseSpeed = 3f;     
    [SerializeField] private float scaleAmount = 0.5f; 

    [Header("Ustawienia koloru")]
    [SerializeField] private Color minColor = new Color(1f, 0.4f, 0.7f); 
    [SerializeField] private Color maxColor = new Color(1f, 1f, 0f); 

    private Vector3 _baseScale;
    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        _baseScale = transform.localScale;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        float scaleMultiplier = Mathf.Lerp(1f - scaleAmount / 2f, 1f + scaleAmount / 2f, t);
        transform.localScale = _baseScale * scaleMultiplier;

        _spriteRenderer.color = Color.Lerp(minColor, maxColor, t);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            GameManager.Instance.IsFrightenedShadowState = true;
            Destroy(this.gameObject);
        }
    }
}
