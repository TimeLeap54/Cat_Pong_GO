using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class AiOffensiveShotSelector
    {
        public ShotIntent Select(
            AiTacticalContext context,
            AIBalanceConfig fallbackConfig,
            RallyAiBalanceConfig rallyConfig,
            ShotIntent lastIntent,
            int consecutivePressureShots)
        {
            if (context.rallyCount < 3)
            {
                return ShotIntent.SafeReturn;
            }

            float ruthlessness = Mathf.Clamp01(context.rallyCount / 20f);
            if (context.ballArrivalRequiresJump)
            {
                float smashChance = Mathf.Lerp(0.05f, 0.85f, ruthlessness);
                if (Random.value < smashChance)
                {
                    return ShotIntent.Smash;
                }
            }

            float safeWeight;
            float deepWeight;
            float dropWeight;
            float lobWeight;
            BuildWeights(context, fallbackConfig, rallyConfig, ruthlessness,
                out safeWeight, out deepWeight, out dropWeight, out lobWeight);

            if (context.playerRecentlyJumped || context.playerOutOfPosition)
            {
                float mercyChance = Mathf.Lerp(0.50f, 0.00f, ruthlessness);
                if (Random.value < mercyChance)
                {
                    safeWeight = 0.4f;
                    deepWeight = 0f;
                    dropWeight = 0f;
                    lobWeight = 0.6f;
                }
            }

            if (lastIntent == ShotIntent.Drop) dropWeight = 0f;
            if (lastIntent == ShotIntent.Lob) lobWeight = 0f;

            int maxPressure = rallyConfig == null
                ? 2
                : Mathf.Max(1, Mathf.RoundToInt(rallyConfig.MaxConsecutivePressureShots));
            if (consecutivePressureShots >= maxPressure)
            {
                deepWeight = 0f;
                dropWeight = 0f;
                lobWeight = 0f;
            }

            return WeightedPick(
                (ShotIntent.SafeReturn, safeWeight),
                (ShotIntent.Deep, deepWeight),
                (ShotIntent.Drop, dropWeight),
                (ShotIntent.Lob, lobWeight));
        }

        public static bool IsPressureShot(ShotIntent intent)
        {
            return intent == ShotIntent.Deep || intent == ShotIntent.Drop || intent == ShotIntent.Lob;
        }

        private static void BuildWeights(
            AiTacticalContext context,
            AIBalanceConfig fallbackConfig,
            RallyAiBalanceConfig rallyConfig,
            float ruthlessness,
            out float safeWeight,
            out float deepWeight,
            out float dropWeight,
            out float lobWeight)
        {
            safeWeight = 0f;
            deepWeight = 0f;
            dropWeight = 0f;
            lobWeight = 0f;

            if (context.playerNearNet)
            {
                deepWeight = Mathf.Lerp(0.20f, 0.70f, ruthlessness);
                lobWeight = Mathf.Lerp(0.10f, 0.15f, ruthlessness);
                safeWeight = 1.0f - deepWeight - lobWeight;
                return;
            }

            if (context.playerDeepCourt)
            {
                dropWeight = Mathf.Lerp(0.10f, 0.65f, ruthlessness);
                deepWeight = Mathf.Lerp(0.40f, 0.15f, ruthlessness);
                safeWeight = 1.0f - dropWeight - deepWeight;
                return;
            }

            if (context.opponentNearNet)
            {
                deepWeight = Mathf.Lerp(0.35f, 0.60f, ruthlessness);
                lobWeight = Mathf.Lerp(0.15f, 0.15f, ruthlessness);
                safeWeight = 1.0f - deepWeight - lobWeight;
                return;
            }

            if (context.playerOutOfPosition)
            {
                deepWeight = Mathf.Lerp(0.30f, 0.50f, ruthlessness);
                dropWeight = Mathf.Lerp(0.20f, 0.40f, ruthlessness);
                safeWeight = 1.0f - deepWeight - dropWeight;
                return;
            }

            if (rallyConfig != null)
            {
                safeWeight = rallyConfig.GetSafeChance(context.rallyCount);
                deepWeight = rallyConfig.GetDeepChance(context.rallyCount);
                dropWeight = rallyConfig.GetDropChance(context.rallyCount);
                lobWeight = rallyConfig.GetLobChance(context.rallyCount);
                return;
            }

            safeWeight = fallbackConfig.SafeWeight;
            deepWeight = fallbackConfig.DeepWeight;
            dropWeight = fallbackConfig.DropWeight;
            lobWeight = fallbackConfig.LobWeight;
        }

        private static ShotIntent WeightedPick(params (ShotIntent intent, float weight)[] options)
        {
            float total = 0f;
            for (int i = 0; i < options.Length; i++)
            {
                total += Mathf.Max(0f, options[i].weight);
            }

            if (total <= 0f)
            {
                return ShotIntent.SafeReturn;
            }

            float roll = Random.value * total;
            for (int i = 0; i < options.Length; i++)
            {
                roll -= Mathf.Max(0f, options[i].weight);
                if (roll <= 0f)
                {
                    return options[i].intent;
                }
            }

            return ShotIntent.SafeReturn;
        }
    }
}
