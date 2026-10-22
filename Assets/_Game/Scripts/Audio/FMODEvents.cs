using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "FMODEvents", menuName = "Scriptable Objects/FMODEvents")]
public class FMODEvents : ScriptableObject
{
    [Header("Sound Effects")]
    public AudioEvents.SFX SFX;

    [Header("Background Music")]
    public AudioEvents.Music Music;

    [Header("UI")]
    public AudioEvents.UI UI;

    /// <summary>
    /// Validates all event references are assigned
    /// </summary>
    private void OnValidate()
    {
        ValidateEventReferences();
    }

    private void ValidateEventReferences()
    {
        // Check SFX
        if (SFX != null)
        {
            if (SFX.LightPowerupPickup.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.PowerupPickup is not assigned in {name}");
            if (SFX.ShadowPowerupPickup.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.PowerupPickup is not assigned in {name}");
            if (SFX.TimeDangerZone.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.TimeDangerZone is not assigned in {name}");
            if (SFX.EnemySwap.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.TimeDangerZone is not assigned in {name}");
            if (SFX.ScoreBeep.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.ScoreBeep is not assigned in {name}");
            if (SFX.GameOver.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.GameOver is not assigned in {name}");
            if (SFX.Win.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.Win is not assigned in {name}");
        }

        // Check BGM
        if (Music != null)
        {
            if (Music.Music8Bit.IsNull)
                Debug.LogWarning($"[FMODEvents] Music.Music8Bit is not assigned in {name}");
        }

        if (UI != null)
        {
            if (UI.Select.IsNull)
                Debug.LogWarning($"[FMODEvents] UI.UIBtnHover is not assigned in {name}");
        }
    }
}

namespace AudioEvents
{
    [System.Serializable]
    public class SFX
    {
        public EventReference LightPowerupPickup;
        public EventReference ShadowPowerupPickup;
        public EventReference TimeDangerZone;
        public EventReference EnemySwap;
        public EventReference ScoreBeep;
        public EventReference GameOver;
        public EventReference Win;
    }

    [System.Serializable]
    public class Music
    {
        public EventReference Music8Bit;
    }

    [System.Serializable]
    public class UI
    {
        public EventReference Select;
        public EventReference Close;
        public EventReference Cancel;
        public EventReference Error;
        public EventReference Cursor;
    }
}
