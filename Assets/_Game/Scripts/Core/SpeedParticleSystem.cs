using UnityEngine;

enum SpeedParticleSystemState {
    SkullBottomAnimation,
    SkullUpAnimation,
    SkullRightAnimation,
    SkullLeftAnimation
}
public class SpeedParticleSystem : MonoBehaviour {
    [SerializeField] private Animator _animator;
    [SerializeField] private ShadowController _shadowController;
    [SerializeField] private TrailRenderer _trail;
    public void Stop() {
        _trail.enabled = false;
    }
    public void Run() {
        _trail.Clear();
        _trail.enabled = true;
    }
}
