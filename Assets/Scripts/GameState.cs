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
            moveSpeed = 3.8f,
            reactionDelay = 0.42f,
            mistakeRate = 0.34f,
            hitAccuracy = 0.55f,
            aggression = 0.25f
        },
        new OpponentProfile
        {
            displayName = "Round 2: Dojo Cat",
            moveSpeed = 5.0f,
            reactionDelay = 0.26f,
            mistakeRate = 0.18f,
            hitAccuracy = 0.72f,
            aggression = 0.48f
        },
        new OpponentProfile
        {
            displayName = "Final: Master Cat",
            moveSpeed = 6.2f,
            reactionDelay = 0.16f,
            mistakeRate = 0.08f,
            hitAccuracy = 0.86f,
            aggression = 0.72f
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

