using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject _lightTutorial;
    [SerializeField] private GameObject _skullTutorial;
    [SerializeField] private Button _startButton;

    private void Awake() {
        _startButton.onClick.AddListener((Disable));
    }
    private void Disable() {
        _lightTutorial.SetActive(false);
        _skullTutorial.SetActive(false);
    }
    void OnEnable()
    {
        _startButton.onClick.AddListener((Disable));
        _lightTutorial.SetActive(true);
        _skullTutorial.SetActive(true);
    }

    void OnDisable() {
       _startButton.onClick.RemoveListener((Disable));
    }
}
