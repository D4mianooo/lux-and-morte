using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents the "Eaten" state for shadows (ghosts) after they are eaten by the player.
/// In this state, the shadow returns to its home (ghost house) before respawning.
/// </summary>
public class ShadowEatenState : IShadowState
{
    public ShadowState State => ShadowState.Eaten;

    #region Constants and Fields
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(0, -1),
    };

    private const float DecisionInterval = 0.13f;

    private List<Vector2Int> _homeTargets;
    private int _currentTargetIndex;
    private Vector2Int _lastDirection;
    private Vector3Int _currentCell;
    private float _moveTimer;

    private Dictionary<Vector2Int, int> _distanceMap;
    #endregion

    #region IShadowState Implementation
    /// <summary>
    /// Called when the shadow enters the "Eaten" state.
    /// Initializes movement towards home and sets appearance to "dead".
    /// </summary>
    public void Enter(ShadowController shadow)
    {
        GameManager
            .Instance.PlayerManager.Players.First(p => p.Role == PlayerManager.PlayerRole.Light)
            .PlayerScore.RoundScores.First(r =>
                r.RoundNumber == GameManager.Instance.Timer.CurrentRound
            )
            .AddSkullKill();

        var player = GameManager.Instance.PlayerManager.Players.FirstOrDefault(p => p.Role == PlayerManager.PlayerRole.Light);
        player.RaisePlayerScoreChanged(player.PlayerScore.RoundScores[0].PointsPerSkullKill);

        _homeTargets = GameManager.Instance.Grid.GetShadowHomeTargets();
        _currentTargetIndex = 0;
        _lastDirection = Vector2Int.zero;
        shadow.CurrentDirection = Vector2Int.zero;

        shadow.Movement.Stop();

        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetDead();

        BuildDistanceMap(GameManager.Instance.Grid, _homeTargets[0]);
    }


    /// <summary>
    /// Called when exiting the "Eaten" state.
    /// No special logic is needed.
    /// </summary>
    public void Exit(ShadowController shadow) 
    {
        shadow.OnRevive();
    }

    /// <summary>
    /// Called every frame while the shadow is in the "Eaten" state.
    /// Moves the shadow along the path to its home.
    /// </summary>
    public void Update(ShadowController shadow)
    {
        _moveTimer += Time.deltaTime;
        if (_moveTimer < DecisionInterval)
            return;

        _moveTimer = 0f;

        var grid = GameManager.Instance.Grid;
        _currentCell = Grid.WorldToCell(shadow.transform.position);
        var cell = (Vector2Int)_currentCell;

        Vector2Int target = _homeTargets[_currentTargetIndex];

        if (cell == target)
        {
            _currentTargetIndex++;

            if (_currentTargetIndex >= _homeTargets.Count)
            {
                var appearance = shadow.GetComponent<ShadowAppearanceManager>();
                appearance.SetColorAfterEaten();
                appearance.SetColorBeforeFrightened();

                shadow.SetState(new ShadowExitBaseState());
                return;
            }

            BuildDistanceMap(grid, _homeTargets[_currentTargetIndex]);
            return;
        }

        Vector2Int bestDir = Vector2Int.zero;
        int bestDist = int.MaxValue;

        foreach (var dir in Directions)
        {
            Vector2Int next = cell + dir;

            if (!grid.IsWalkable((Vector3Int)next))
                continue;

            if (!_distanceMap.TryGetValue(next, out int dist))
                continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir = dir;
            }
        }

        if (bestDir == Vector2Int.zero)
            return;

        _lastDirection = bestDir;
        shadow.CurrentDirection = bestDir;
        shadow.Movement.OnMove(bestDir);
    }

    /// <summary>
    /// Builds a distance map using BFS, starting from the target cell.
    /// Stores the minimum number of steps required to reach the target
    /// from each walkable cell on the grid.
    /// Used for pathfinding (e.g. returning to home in Eaten state).
    /// </summary>
    private void BuildDistanceMap(Grid grid, Vector2Int target)
    {
        _distanceMap = new Dictionary<Vector2Int, int>();
        Queue<Vector2Int> queue = new();

        _distanceMap[target] = 0;
        queue.Enqueue(target);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int dist = _distanceMap[current];

            foreach (var dir in Directions)
            {
                Vector2Int next = current + dir;

                if (_distanceMap.ContainsKey(next))
                    continue;

                if (!grid.IsWalkable((Vector3Int)next))
                    continue;

                _distanceMap[next] = dist + 1;
                queue.Enqueue(next);
            }
        }
    }
    #endregion
}
