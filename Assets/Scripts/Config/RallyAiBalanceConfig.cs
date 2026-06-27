using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    [CreateAssetMenu(fileName = "RallyAiBalanceConfig", menuName = "Cat Tennis/AI/Rally AI Balance")]
    public sealed class RallyAiBalanceConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private float reactionDelayMin = 0.08f;
        [SerializeField] private float reactionDelayMax = 0.22f;
        [SerializeField] private float predictionErrorEarly = 0.45f;
        [SerializeField] private float predictionErrorLate = 0.12f;

        [Header("Shot Probability Curves")]
        [SerializeField] private AnimationCurve safeChanceByRally = AnimationCurve.Linear(0, 1f, 50, 0.3f);
        [SerializeField] private AnimationCurve deepChanceByRally = AnimationCurve.Linear(0, 0f, 50, 0.4f);
        [SerializeField] private AnimationCurve dropChanceByRally = AnimationCurve.Linear(0, 0f, 50, 0.2f);
        [SerializeField] private AnimationCurve lobChanceByRally = AnimationCurve.Linear(0, 0f, 50, 0.1f);

        [Header("Pressure Curves")]
        [SerializeField] private AnimationCurve targetAccuracyByRally = AnimationCurve.Linear(0, 0.5f, 50, 0.9f);
        [SerializeField] private AnimationCurve shotSpeedByRally = AnimationCurve.Linear(0, 1.0f, 50, 1.25f);
        [SerializeField] private AnimationCurve maxDifficultyByRally = AnimationCurve.Linear(0, 1f, 50, 5f);

        [Header("Fairness")]
        [SerializeField] private float minReachableDistanceFromPlayer = 1.1f;
        [SerializeField] private float maxConsecutivePressureShots = 2f;
        [SerializeField] private float mercyShotChanceWhenPlayerStruggling = 0.35f;

        public float ReactionDelayMin => reactionDelayMin;
        public float ReactionDelayMax => reactionDelayMax;
        public float PredictionErrorEarly => predictionErrorEarly;
        public float PredictionErrorLate => predictionErrorLate;

        public float GetSafeChance(int rally) => safeChanceByRally.Evaluate(rally);
        public float GetDeepChance(int rally) => deepChanceByRally.Evaluate(rally);
        public float GetDropChance(int rally) => dropChanceByRally.Evaluate(rally);
        public float GetLobChance(int rally) => lobChanceByRally.Evaluate(rally);

        public float GetTargetAccuracy(int rally) => targetAccuracyByRally.Evaluate(rally);
        public float GetShotSpeedMultiplier(int rally) => shotSpeedByRally.Evaluate(rally);
        public int GetMaxDifficulty(int rally) => Mathf.RoundToInt(maxDifficultyByRally.Evaluate(rally));

        public float MinReachableDistanceFromPlayer => minReachableDistanceFromPlayer;
        public float MaxConsecutivePressureShots => maxConsecutivePressureShots;
        public float MercyShotChanceWhenPlayerStruggling => mercyShotChanceWhenPlayerStruggling;
    }
}
