using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoBehaviour
{
    [Serializable]
    public enum CameraZonesName
    {
        TopLeft =0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3,
        CenterTop = 4,
        CenterBottom = 5,
        None = 6,
    }

    [Serializable]
    public struct CameraZone
    {
        public CameraZonesName name;
        public Vector2 offset;
        public Vector2 boundingX;
        public bool biggerThan0Y;
    }

    [Header("Pixel Perfect")]
    [SerializeField] private PixelPerfectCamera _mainPixelPerfectCamera;
    [SerializeField] private PixelPerfectCamera _anotherPixelPerfectCamera;

    [Header("Zoom Values")]
    [SerializeField] private int _fromPixelPerfectValue;
    [SerializeField] private int _toPixelPerfectValue;

    [Header("Zoom Animation")]
    [SerializeField] private float _zoomDuration = 0.6f;
    [SerializeField]
    private AnimationCurve _zoomCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    [SerializeField] private CinemachinePositionComposer _cinemachineCam;
    [SerializeField] private List<CameraZone> _cameraZones = new List<CameraZone>();
    [SerializeField] private CameraZonesName _cametraZoneToZoom;
    [SerializeField] private Vector2 _zoomOutOffset = new Vector2(-0.5f, -0.5f);

    private Coroutine _zoomCoroutine;

    public List<CameraZone> CameraZones { get { return _cameraZones; } }

    private bool _isZoomIn = false;

    [ContextMenu("Zoom In")]
    public void ZoomIn()
    {
        _isZoomIn = true;
        StartZoom(_fromPixelPerfectValue, _toPixelPerfectValue, true);
    }

    [ContextMenu("Zoom Out")]
    public void ZoomOut()
    {
        _isZoomIn = false;
        StartZoom(_toPixelPerfectValue, _fromPixelPerfectValue, false);
    }

    public void ZoomIn(CameraZonesName cameraZonesName)
    {
        _cametraZoneToZoom = cameraZonesName;
        ZoomIn();
    }

    private void StartZoom(int from, int to, bool zoomIn)
    {
        if (_zoomCoroutine != null)
            StopCoroutine(_zoomCoroutine);

        _zoomCoroutine = StartCoroutine(ZoomCoroutine(from, to, zoomIn));
    }

    private IEnumerator ZoomCoroutine(int from, int to, bool zoomIn)
    {
        Vector3 startOffset = _cinemachineCam.TargetOffset;
        Vector2 target2DOffset = zoomIn
            ? GetZoneOffset(_cametraZoneToZoom)
            : _zoomOutOffset; 
        Vector3 targetOffset = new Vector3(target2DOffset.x, target2DOffset.y, startOffset.z);

        if (zoomIn)
        {
            float time = 0f;
            while (time < _zoomDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / _zoomDuration);
                float curvedT = _zoomCurve.Evaluate(t);

                _cinemachineCam.TargetOffset = Vector3.Lerp(startOffset, targetOffset, curvedT);

                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, curvedT));
                _mainPixelPerfectCamera.assetsPPU = value;
                _anotherPixelPerfectCamera.assetsPPU = value;

                yield return null;
            }
        }
        else
        {
            float time = 0f;
            while (time < _zoomDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / _zoomDuration);
                float curvedT = _zoomCurve.Evaluate(t);

                _cinemachineCam.TargetOffset = Vector3.Lerp(startOffset, targetOffset, curvedT);

                yield return null;
            }

            time = 0f;
            int currentPPU = _mainPixelPerfectCamera.assetsPPU;
            while (time < _zoomDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / _zoomDuration);
                float curvedT = _zoomCurve.Evaluate(t);

                int value = Mathf.RoundToInt(Mathf.Lerp(currentPPU, to, curvedT));
                _mainPixelPerfectCamera.assetsPPU = value;
                _anotherPixelPerfectCamera.assetsPPU = value;

                yield return null;
            }
        }

        _cinemachineCam.TargetOffset = targetOffset;
        _mainPixelPerfectCamera.assetsPPU = to;
        _anotherPixelPerfectCamera.assetsPPU = to;
    }


    private Vector2 GetZoneOffset(CameraZonesName zoneName)
    {
        foreach (var zone in _cameraZones)
        {
            if (zone.name == zoneName)
                return zone.offset;
        }

        return Vector2.zero;
    }

    public CameraZonesName GetZoneByPosition(Vector3 position)
    {
        foreach (var zone in _cameraZones)
        {
            bool insideX = position.x >= zone.boundingX.x && position.x <= zone.boundingX.y;
            bool insideY = !zone.biggerThan0Y || position.y > 0;

            if (insideX && insideY)
                return zone.name;
        }

        return CameraZonesName.None;
    }

    private void Start()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.Timer.OnRoundEnding += Timer_OnRoundEnding;
        }
    }

    private void Timer_OnRoundEnding(object sender, OnRoundEndEventArgs e)
    {
        if(_isZoomIn)
            ZoomOut();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Timer.OnRoundEnding -= Timer_OnRoundEnding;
        }
    }
}
