using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

[Serializable]
public class PlayerScore
{
    [Serializable]
    public class RoundScore
    {
        public int RoundNumber { get; private set; } = 0;

        public int PointsPerSkullKill { get; set; } = 10;
        public int PointsPerLightTile { get; set; } = 1;
        public int MaxTimeBonusPoints { get; set; } = 20;
        public float TimeBonusExponent { get; set; } = 1.5f;

        public int TotalScore
        {
            get =>
                (SkullKills * PointsPerSkullKill) + (LightTiles * PointsPerLightTile) + TimeBonus;
        }
        public int SkullKills { get; private set; } = 0;
        public int LightTiles { get; private set; } = 0;
        public int TimeBonus { get; private set; } = 0;

        public RoundScore(
            int roundNumber,
            int pointsPerSkullKill = 10,
            int pointsPerLightTile = 1,
            int maxTimeBonusPoints = 20,
            float timeBonusExponent = 1.5f
        )
        {
            RoundNumber = roundNumber;
            PointsPerSkullKill = pointsPerSkullKill;
            PointsPerLightTile = pointsPerLightTile;
            MaxTimeBonusPoints = maxTimeBonusPoints;
            TimeBonusExponent = timeBonusExponent;
        }

        public void AddSkullKill(int kills = 1)
        {
            SkullKills += kills;
        }

        public void AddLightTile(int tiles = 1)
        {
            LightTiles += tiles;
        }

        public void SetTimeBonus(float roundTime, float roundDuration)
        {
            TimeBonus = Mathf.RoundToInt(
                MaxTimeBonusPoints
                    * Mathf.Pow(
                        Mathf.Clamp01((roundDuration - roundTime) / roundDuration),
                        TimeBonusExponent
                    )
            );
        }
    }

    public int TotalScore
    {
        get => RoundScores.Sum(round => round.TotalScore);
    }
    public List<RoundScore> RoundScores { get; private set; } = new List<RoundScore>();
}
