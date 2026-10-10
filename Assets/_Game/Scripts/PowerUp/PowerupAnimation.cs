using UnityEngine;
using DG.Tweening;

public class PowerupAnimation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationDuration = 0.3f;
    [SerializeField] private float pauseDuration = 0.1f;

    private Sequence _rotationSequence;

    public void StartAnimation()
    {
        StartRotation();
    }

    public void StopAnimation()
    {
        _rotationSequence?.Kill();
    }

    private void StartRotation()
    {
        _rotationSequence = DOTween.Sequence();

        _rotationSequence
            .Append(transform.DORotate(new Vector3(0, 90, 0), rotationDuration, RotateMode.LocalAxisAdd))
            .AppendInterval(pauseDuration)
        //    .Append(transform.DORotate(new Vector3(90, 0, 0), rotationDuration, RotateMode.LocalAxisAdd))
          //  .AppendInterval(pauseDuration)
            .Append(transform.DORotate(new Vector3(0, 90, 0), rotationDuration, RotateMode.LocalAxisAdd))
            .AppendInterval(pauseDuration)
            .SetLoops(-1)
            .SetEase(Ease.Linear);
    }
}
