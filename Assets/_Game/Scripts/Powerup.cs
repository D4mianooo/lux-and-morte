using System;
using UnityEngine;
using Random = System.Random;

public class Powerup : MonoBehaviour {
    [SerializeField] private PowerupVisual powerupVisual;
    [SerializeField] private PowerupCollision powerupCollision;

    public void OnEnable() {
        powerupCollision.OnPowerupCollected += Powerup_OnPowerupCollected;
    }
    private void Powerup_OnPowerupCollected(object sender, EventArgs e) {
        Disable();
    }
    public void Enable() {
        powerupVisual.Show();
        powerupCollision.EnableCollision();
    }

    public void Disable() {
        powerupVisual.Hide();
        powerupCollision.DisableCollision();
    }

    public void SetPowerup(LightPowerup lightPowerup, ShadowPowerup shadowPowerup) {
        SetLightPowerup(lightPowerup);
        SetShadowPowerup(shadowPowerup);
        powerupVisual.SetMaterial(lightPowerup.material, shadowPowerup.material);
    }
    
    private void SetLightPowerup(LightPowerup lightPowerup) {
        powerupCollision.LightPowerup = lightPowerup;
    }
    private void SetShadowPowerup(ShadowPowerup shadowPowerup) {
        powerupCollision.ShadowPowerup = shadowPowerup;
    }

}
