using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class GridPortal : MonoBehaviour
{
    private Vector2? _lastPlayerPosition = null;
    private bool _hasTriggeredTeleport = false;

    // Cooldown tracking
    private bool _isOnCooldown = false;
    private float _cooldownTimer = 0f;
    private float _cooldownDuration = 1.0f;

    // Visual feedback
    private Light2D _light2D;
    private float _originalIntensity;
    private float _dimmedIntensity = 0.2f; // 20% of original intensity when on cooldown
    private Tilemap _tilemapPortals;
    private TileAnimationFlags _originalAnimationFlags;
    private Color _cooldownColor = Color.white;

    private uint _portalId;
    public uint PortalId
    {
        get { return _portalId; }
        set { _portalId = value; }
    }

    private Color _portalColor;
    public Color PortalColor
    {
        get { return _portalColor; }
        set
        {
            _portalColor = value;

            if (_light2D != null)
            {
                _light2D.color = _portalColor;
            }
        }
    }

    private Vector2Int _cellPosition;
    public Vector2Int CellPosition
    {
        get { return _cellPosition; }
        set { _cellPosition = value; }
    }

    public float CooldownDuration
    {
        get { return _cooldownDuration; }
        set { _cooldownDuration = value; }
    }

    private void Awake()
    {
        _light2D = GetComponent<Light2D>();
        if (_light2D != null)
        {
            _originalIntensity = _light2D.intensity;
        }
    }

    private void Update()
    {
        if (_isOnCooldown)
        {

            _cooldownTimer -= Time.deltaTime;

            if (_cooldownTimer <= 0f)
            {
                // Cooldown finished
                EndCooldown();
            }
            else
            {
                // Update light intensity to show cooldown progress
                float cooldownProgress = 1f - (_cooldownTimer / _cooldownDuration);
                float currentIntensity = Mathf.Lerp(
                    _dimmedIntensity * _originalIntensity,
                    _originalIntensity,
                    cooldownProgress
                );

                if (_light2D != null)
                {
                    _light2D.intensity = currentIntensity;
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("PlayerLight"))
            return;

        // Cannot use portal while on cooldown
        if (_isOnCooldown)
            return;

        Debug.Log("Player in portal trigger");

        Vector2 playerPosition = collision.transform.position;
        Vector2 portalCenter = new Vector2(_cellPosition.x + 0.5f, _cellPosition.y + 0.5f);

        // Check if player is at or has crossed the center point
        if (!_hasTriggeredTeleport)
        {
            bool isAtCenter = IsAtCenter(playerPosition, portalCenter);
            bool hasCrossedCenter =
                _lastPlayerPosition.HasValue
                && HasCrossedCenter(_lastPlayerPosition.Value, playerPosition, portalCenter);

            Debug.Log($"isAtCenter: {isAtCenter}, hasCrossedCenter: {hasCrossedCenter}");

            if (isAtCenter || hasCrossedCenter)
            {
                _hasTriggeredTeleport = true;
                TeleportPlayer(collision.gameObject);
            }
        }

        _lastPlayerPosition = playerPosition;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("PlayerLight"))
            return;

        Debug.Log("Player exited portal trigger");

        // Reset state when player exits the portal
        _lastPlayerPosition = null;
        _hasTriggeredTeleport = false;
    }

    private bool IsAtCenter(Vector2 playerPos, Vector2 center)
    {
        // Check if player is at the exact center (within a small threshold)
        float threshold = 0.1f; // Small tolerance for floating point precision
        float distance = Vector2.Distance(playerPos, center);
        return distance <= threshold;
    }

    private bool HasCrossedCenter(Vector2 previousPos, Vector2 currentPos, Vector2 center)
    {
        // Check if the player crossed the center horizontally or vertically
        bool crossedHorizontally =
            (previousPos.x < center.x && currentPos.x >= center.x)
            || (previousPos.x > center.x && currentPos.x <= center.x);

        bool crossedVertically =
            (previousPos.y < center.y && currentPos.y >= center.y)
            || (previousPos.y > center.y && currentPos.y <= center.y);

        // Return true if crossed in either direction and is close enough to center in the other axis
        float tolerance = 0.5f; // Half a tile width

        if (crossedHorizontally && Mathf.Abs(currentPos.y - center.y) <= tolerance)
            return true;

        if (crossedVertically && Mathf.Abs(currentPos.x - center.x) <= tolerance)
            return true;

        return false;
    }

    private void TeleportPlayer(GameObject player)
    {
        Vector3Int? portal = GameManager.Instance.Grid.FindPortalPairPosition(
            _portalId,
            _cellPosition
        );

        if (portal == null)
            return;

        // Teleport player
        player.transform.position = Grid.GetCellCenterWorld(portal.Value, Grid.TilemapType.Portals);

        // Start cooldown on this portal and its pair
        StartCooldown();
        GameManager.Instance.Grid.StartPortalCooldown(_portalId, _cellPosition);
    }

    /// <summary>
    /// Starts the cooldown timer and applies visual effects.
    /// </summary>
    public void StartCooldown()
    {
        if (_isOnCooldown)
            return;

        _isOnCooldown = true;
        _cooldownTimer = _cooldownDuration;

        // Dim the light immediately
        if (_light2D != null)
        {
            _light2D.intensity = _dimmedIntensity * _originalIntensity;
            _light2D.color = _cooldownColor;
        }

        // Pause tile animation
        PauseTileAnimation();
    }

    /// <summary>
    /// Ends the cooldown and restores visual effects.
    /// </summary>
    private void EndCooldown()
    {
        _isOnCooldown = false;
        _cooldownTimer = 0f;

        // Restore original light intensity
        if (_light2D != null)
        {
            _light2D.intensity = _originalIntensity;
            _light2D.color = _portalColor;
        }

        // Resume tile animation
        ResumeTileAnimation();
    }

    /// <summary>
    /// Pauses the animated tile at this portal's position.
    /// </summary>
    private void PauseTileAnimation()
    {
        if (_tilemapPortals == null)
        {
            _tilemapPortals = GameManager.Instance.Grid.GetPortalTilemap();
        }

        if (_tilemapPortals != null)
        {
            Vector3Int cellPos = new Vector3Int(_cellPosition.x, _cellPosition.y, 0);
            _originalAnimationFlags = _tilemapPortals.GetTileAnimationFlags(cellPos);
            _tilemapPortals.SetColor(cellPos, _cooldownColor);
            _tilemapPortals.SetTileAnimationFlags(cellPos, TileAnimationFlags.PauseAnimation);
        }
    }

    /// <summary>
    /// Resumes the animated tile at this portal's position.
    /// </summary>
    private void ResumeTileAnimation()
    {
        if (_tilemapPortals != null)
        {
            Vector3Int cellPos = new Vector3Int(_cellPosition.x, _cellPosition.y, 0);
            _tilemapPortals.SetColor(cellPos, _portalColor);
            _tilemapPortals.SetTileAnimationFlags(cellPos, _originalAnimationFlags);
        }
    }
}
