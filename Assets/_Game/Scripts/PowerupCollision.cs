
using System;
using System.Collections;

using UnityEngine;



public class PowerupCollision : MonoBehaviour {
    [SerializeField] private ShadowController _shadowController;
    [SerializeField] private float speedBoost = 12.5f;
    
    public event EventHandler OnPowerupCollected;

    public LightPowerup LightPowerup { get; set; }
    public ShadowPowerup ShadowPowerup { get; set; }

    private Coroutine  shadowPowerupCoroutine;
    private Coroutine  lightPowerupCoroutine;
    
    private bool _isActive = false;

    private void OnEnable() {
        GameManager.Instance.Timer.OnRoundEnded += TimerOnOnRoundEnded;        
    }
    private void OnDisable() {
        GameManager.Instance.Timer.OnRoundEnded -= TimerOnOnRoundEnded;        
    }
    private void TimerOnOnRoundEnded(object sender, OnRoundEndEventArgs e) {
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.None;
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if (!_isActive) return;

        if (other.CompareTag("PlayerLight")) {
            OnPowerupCollected?.Invoke(this, EventArgs.Empty);
            HandleLightPowerup();
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.LightPowerupPickup);
            
        }

        if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            OnPowerupCollected?.Invoke(this, EventArgs.Empty);
            HandleShadowPowerup(controller);
            AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.ShadowPowerupPickup);
            
        }
    }
    private void HandleShadowPowerup(ShadowController controller) {
        switch (ShadowPowerup._powerupType) {
            case ShadowPowerupType.FarCry:
                if (shadowPowerupCoroutine != null) {
                    StopCoroutine(shadowPowerupCoroutine);
                    shadowPowerupCoroutine = null;
                }
                shadowPowerupCoroutine = StartCoroutine(FarCry(controller));
                break;
            case ShadowPowerupType.SarcasticSmile:
                if (shadowPowerupCoroutine != null) {
                    StopCoroutine(shadowPowerupCoroutine);
                    shadowPowerupCoroutine = null;
                }
                shadowPowerupCoroutine = StartCoroutine(SarcasticSmile());
                break;
            case ShadowPowerupType.HauntedRadar:
                GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.HauntedRadar;
                break;
        }
    }
    private void HandleLightPowerup() {
        switch (LightPowerup._powerupType) {
            case LightPowerupType.EmpathyMode:
                GameManager.Instance.CurrentLightPowerupType = LightPowerupType.EmpathyMode;
                GameManager.Instance.IsFrightenedShadowState = true;
                break;
            case LightPowerupType.SilentTreatment:
                if (lightPowerupCoroutine != null) {
                    StopCoroutine(lightPowerupCoroutine);
                    lightPowerupCoroutine = null;
                }
                lightPowerupCoroutine = StartCoroutine(SilentTreatment());
                GameManager.Instance.CurrentLightPowerupType = LightPowerupType.SilentTreatment;
                break;
            case LightPowerupType.DeepBreath:
                GameManager.Instance.CurrentLightPowerupType = LightPowerupType.DeepBreath;
                break;
        }
    }
    
    private IEnumerator FarCry(ShadowController controller) {
        SpeedParticleSystem ps = controller.GetComponent<SpeedParticleSystem>();
        if (ps != null) {
            ps.Run();
        }
        
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.FarCry;
        controller.Movement.SetSpeed(speedBoost);

        yield return WaitWhilePaused(ShadowPowerup._duration);

        ps.Stop();
        
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;
        
        if (controller) {
            controller.Movement.ResetSpeed();
        }
    }

    private IEnumerator SarcasticSmile() {
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.SarcasticSmile;
        yield return WaitWhilePaused(ShadowPowerup._duration);
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;
    }

    private IEnumerator SilentTreatment() {
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.SilentTreatment;
        yield return WaitWhilePaused(LightPowerup._duration);
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.None;
    }

    private IEnumerator WaitWhilePaused(float duration) {
        float remaining = duration;

        while (remaining > 0f) {
            if (!GameManager.Instance.Timer.IsGamePaused) {
                remaining -= Time.deltaTime;
            }

            yield return null;
        }
    }

    public void EnableCollision() {
        _isActive = true;
    }
    public void DisableCollision() {
        _isActive = false;
    }
}
