using UnityEngine;

public class PowerupVisual : MonoBehaviour {
    [Header("Components")]
    [SerializeField] private PowerupAnimation _powerupAnimation;
    [SerializeField] private GameObject _cube;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private ParticleSystem _particleSystem;
    
    [Header("Materials")]
    [SerializeField] private Material _empathyModeMaterial;
    [SerializeField] private Material _silentTreatmentMaterial;

    #region Constants
    private const int ParticleEmitCount = 50;
    private const int LightMaterialIndex = 0;
    private const int ShadowMaterialIndex = 2;
    #endregion

    #region Private Fields
        private MeshRenderer _meshRenderer;
        private ParticleSystem.MainModule _particleMainModule;
        private bool _isActive = false;
    #endregion
 
    #region Unity Methods
    private void Awake() {
        _meshRenderer = _cube.GetComponent<MeshRenderer>();
        _particleMainModule = _particleSystem.main;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!_isActive) return;
        
        if (other.CompareTag("PlayerLight")) {
            if (_meshRenderer.materials[LightMaterialIndex].name.StartsWith("EmpathyModeMaterial")) {
                EmitParticles(ParticleColors.EmpathyLight);
            }
            else {
                EmitParticles(ParticleColors.StandardLight);
            }
            _particleSystem.Emit(ParticleEmitCount);
        } else if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            var main = _particleSystem.main;

            if (_meshRenderer.materials[ShadowMaterialIndex].name.StartsWith("SarcasticSmileMaterial")) {
                EmitParticles(ParticleColors.SarcasticSmileMaterial);
            }
            else {
                EmitParticles(ParticleColors.StandardShadow);
            }
            _particleSystem.Emit(ParticleEmitCount);
        }
    }
    #endregion

    #region Public Methods
        public void SetMaterial(Material lightMaterial, Material shadowMaterial) {
            Material[] newMaterials = _meshRenderer.materials;
            newMaterials[LightMaterialIndex] = lightMaterial;
            newMaterials[ShadowMaterialIndex] = shadowMaterial;
            _meshRenderer.materials = newMaterials;
        }

        public void Hide() {
            _spriteRenderer.enabled = false;
            _cube.SetActive(false);
            _isActive = false;
            _powerupAnimation.StopAnimation();
        }

        public void Show() {
            _spriteRenderer.enabled = true;
            _cube.SetActive(true);
            _isActive = true;
            _powerupAnimation.StartAnimation();
        }
    #endregion

    #region Private Methods
        private void EmitParticles(Color color) {
            _particleMainModule.startColor = color;
            _particleSystem.Emit(ParticleEmitCount);
        }
    #endregion
}

public static class ParticleColors {
    public static readonly Color EmpathyLight = new Color32(93, 50, 204, 255); 
    public static readonly Color StandardLight = new Color32(0, 170 , 204, 255);
    public static readonly Color SarcasticSmileMaterial = new Color32(62 , 204 , 55, 255);
    public static readonly Color StandardShadow = new Color32(204, 50, 50, 255 );
}