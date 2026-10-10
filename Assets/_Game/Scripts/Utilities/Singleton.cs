using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component {
    public static T Instance;
    private void OnEnable() {
        if (Instance == null) {
            Instance = this as T;
            DontDestroyOnLoad(this);
        }
        else {
            Destroy(gameObject);
        }
    }
}
