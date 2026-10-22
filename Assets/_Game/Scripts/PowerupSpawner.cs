using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class PowerupSpawner : MonoBehaviour {
    [SerializeField] private float _firstPowerupSpawnTime = 5f;
    [SerializeField] private float _spawnDelta = 3f;
    [SerializeField] private int _powerupCount = 3;

    [Header("Powerups")]
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] private List<LightPowerup> _lightPowerups;
    [SerializeField] private List<ShadowPowerup> _shadowPowerups;
    
    private Grid _grid;
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();
    private readonly List<Powerup> _powerups = new List<Powerup>();
    private readonly List<PowerupCollision> _powerupsCollisions = new List<PowerupCollision>();

    private void Start() {
        _grid = GameManager.Instance.Grid;

        if (_grid == null) {
            Debug.LogError("Grid not assigned to PowerupSpawner!");
            return;
        }

        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnded += PowerupSpawner_OnRoundEnded;
    }

    private void OnDisable() {
        GameManager.Instance.Timer.OnRoundStart -= PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnded -= PowerupSpawner_OnRoundEnded;
    }

    private void PowerupSpawner_OnRoundEnded(object sender, EventArgs e) {
        StopAllRunningCoroutines();
        DeactivateAllPowerups();
    }

    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
        _grid = GameManager.Instance.Grid;
        SpawnPowerups();
        //_randomIdxs = GenerateRandomPowerupIndices(_powerups.Count, _powerupCount);
        
        _runningCoroutines.Add(StartCoroutine(ActivateSingleAfterDelay(0, _firstPowerupSpawnTime)));
    }
    
    private int[] GenerateRandomPowerupIndices(int max, int count) {
        List<int> list = new List<int>();
        
        for (int i = 0; i < max; i++)
            list.Add(i);

        for (int i = 0; i < max; i++) {
            int k = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[k]) = (list[k], list[i]);
        }

        int[] result = list.GetRange(0, count).ToArray();
        

        return result;
    }

    private void SpawnPowerups() {
        List<Vector3> spawnPoints = _grid.GetPowerupSpawnPoints(); 
        Debug.Log($"Powerup spawn points: {spawnPoints.Count}");
        if (spawnPoints.Count >= _powerupCount) {
            for (int i = 0; i < spawnPoints.Count; i++) {
                GameObject powerupGO = Instantiate(_powerupPrefab, spawnPoints[i], Quaternion.identity);
                Powerup powerup = powerupGO.GetComponent<Powerup>();
            
                powerup.SetPowerup(GetRandomLightPowerup(), GetRandomShadowPowerup());
                powerup.Disable();
            
                _powerups.Add(powerup);
                _powerupsCollisions.Add(powerup.GetComponent<PowerupCollision>());
            }
        }
        else {
            int spawnPointIdx = 0;
            for(int i = 0; i < _powerupCount; i++) {
                GameObject powerupGO = Instantiate(_powerupPrefab, spawnPoints[spawnPointIdx++], Quaternion.identity);
                spawnPointIdx %= spawnPoints.Count;
                
                Powerup powerup = powerupGO.GetComponent<Powerup>();
            
                powerup.SetPowerup(GetRandomLightPowerup(), GetRandomShadowPowerup());
                powerup.Disable();
            
                _powerups.Add(powerup);
                _powerupsCollisions.Add(powerup.GetComponent<PowerupCollision>());
            } 
        }
        for(int i = 0; i < _powerupCount - 1; i++) {
            int index = i;
            _powerupsCollisions[index].OnPowerupCollected += (sender, args) => {
                _runningCoroutines.Add(StartCoroutine(ActivateSingleAfterDelay(index + 1, _spawnDelta)));
            };
        }
    }
    private IEnumerator ActivateSingleAfterDelay(int index, float delay) {
        yield return new WaitForSeconds(delay);
        float remaining = delay;

        while (remaining > 0f) {
            if (!GameManager.Instance.Timer.IsGamePaused) {
                remaining -= Time.deltaTime;
            }

            yield return null;
        }
        
        if (_powerups[index]) {
            Powerup pc = _powerups[index];
            pc.Enable();
        }
    }

    private void DeactivateAllPowerups() {
        foreach (var p in _powerups) {
            if (p != null) {
                Destroy(p.gameObject);
            }
        }
        _powerups.Clear();
        _powerupsCollisions.Clear();
    }

    private void StopAllRunningCoroutines() {
        foreach (var c in _runningCoroutines)
            if (c != null) StopCoroutine(c);

        _runningCoroutines.Clear();
    }
    
    private LightPowerup GetRandomLightPowerup() {
        var random = new Random();
        return random.Next(0, 100) > 50 ? _lightPowerups[0] : _lightPowerups[1];
    }
    
    private ShadowPowerup GetRandomShadowPowerup() {
        var random = new Random();
        return random.Next(0, 100) > 50 ? _shadowPowerups[0] : _shadowPowerups[1];
    }
}
[Serializable]
public class LightPowerup {
    public LightPowerupType _powerupType;
    public int _duration;
    public Material material;

}
[Serializable]
public class ShadowPowerup{
    public ShadowPowerupType _powerupType;
    public int _duration;
    public Material material;

}
public enum ShadowPowerupType {
    None,
    FarCry,
    SarcasticSmile,
    HauntedRadar
}

public enum LightPowerupType {
    None,
    DeepBreath,
    EmpathyMode,
    SilentTreatment
}