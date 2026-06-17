using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class Day5BalanceSimulator
{
    private const string ReportPath = "Docs/Day5BalanceSimulation.md";
    private const int MatchesPerRound = 10;
    private const int RandomSeed = 20260617;

    [MenuItem("Tools/CatPong/Run Day 5 Balance Simulation")]
    public static void Run()
    {
        Directory.CreateDirectory("Docs");

        var random = new System.Random(RandomSeed);
        var report = new StringBuilder();
        report.AppendLine("# Day 5 Balance Simulation");
        report.AppendLine();
        report.AppendLine("This is a deterministic tuning check based on AI profile values. Use real playtest logs from Unity Console for final judgement.");
        report.AppendLine();
        report.AppendLine("| Round | Player Wins | AI Wins | Average Score Diff | Point Win Chance |");
        report.AppendLine("| --- | ---: | ---: | ---: | ---: |");

        foreach (var profile in GameState.Opponents)
        {
            var playerWins = 0;
            var aiWins = 0;
            var totalDiff = 0;
            var playerPointChance = EstimatePlayerPointChance(profile);

            for (var i = 0; i < MatchesPerRound; i++)
            {
                var playerScore = 0;
                var aiScore = 0;

                while (playerScore < GameState.PointsToWin && aiScore < GameState.PointsToWin)
                {
                    if (random.NextDouble() < playerPointChance)
                    {
                        playerScore++;
                    }
                    else
                    {
                        aiScore++;
                    }
                }

                if (playerScore > aiScore)
                {
                    playerWins++;
                }
                else
                {
                    aiWins++;
                }

                totalDiff += playerScore - aiScore;
            }

            var averageDiff = totalDiff / (float)MatchesPerRound;
            report.AppendLine($"| {profile.displayName} | {playerWins}/{MatchesPerRound} | {aiWins}/{MatchesPerRound} | {averageDiff:0.0} | {playerPointChance:0.00} |");
            Debug.Log($"Day5 balance sim - {profile.displayName}: player wins {playerWins}/{MatchesPerRound}, average diff {averageDiff:0.0}, point chance {playerPointChance:0.00}");
        }

        File.WriteAllText(ReportPath, report.ToString());
        AssetDatabase.Refresh();
    }

    private static double EstimatePlayerPointChance(OpponentProfile profile)
    {
        var aiSkill =
            Mathf.Clamp01(profile.moveSpeed / 7f) * 0.28f
            + Mathf.Clamp01(1f - profile.reactionDelay / 0.6f) * 0.25f
            + (1f - profile.mistakeRate) * 0.22f
            + profile.hitAccuracy * 0.17f
            + profile.aggression * 0.08f;

        return Mathf.Clamp(0.92f - aiSkill * 0.55f, 0.35f, 0.78f);
    }
}
