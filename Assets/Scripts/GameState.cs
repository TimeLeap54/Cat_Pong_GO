using System;
using UnityEngine;

[Serializable]
public class OpponentProfile
{
    public string displayName;
    public float moveSpeed;
    public float reactionDelay;
    [Range(0f, 1f)] public float mistakeRate;
    [Range(0f, 1f)] public float hitAccuracy;
    [Range(0f, 1f)] public float aggression;
}

public static class GameState
{
    public const int PointsToWin = 5;

    public static int CurrentRoundIndex { get; private set; }

    public static readonly OpponentProfile[] Opponents =
    {
        new OpponentProfile
        {
            displayName = "Round 1: Rookie Cat",
            moveSpeed = 3.4f,
            reactionDelay = 0.55f,
            mistakeRate = 0.42f,
            hitAccuracy = 0.48f,
            aggression = 0.18f
        },
        new OpponentProfile
        {
            displayName = "Round 2: Dojo Cat",
            moveSpeed = 5.15f,
            reactionDelay = 0.27f,
            mistakeRate = 0.16f,
            hitAccuracy = 0.72f,
            aggression = 0.5f
        },
        new OpponentProfile
        {
            displayName = "Final: Master Cat",
            moveSpeed = 5.85f,
            reactionDelay = 0.2f,
            mistakeRate = 0.12f,
            hitAccuracy = 0.82f,
            aggression = 0.62f
        }
    };

    public static OpponentProfile CurrentOpponent => Opponents[Mathf.Clamp(CurrentRoundIndex, 0, Opponents.Length - 1)];
    public static bool IsFinalRound => CurrentRoundIndex >= Opponents.Length - 1;

    public static void StartTournament()
    {
        CurrentRoundIndex = 0;
    }

    public static bool AdvanceRound()
    {
        if (IsFinalRound)
        {
            return false;
        }

        CurrentRoundIndex++;
        return true;
    }
}
