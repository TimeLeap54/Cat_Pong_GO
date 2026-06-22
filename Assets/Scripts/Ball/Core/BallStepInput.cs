namespace CatTennis.BallPhysics.Core
{
    /// <summary>External geometry and fixed time supplied for one physics step.</summary>
    public readonly struct BallStepInput
    {
        public BallStepInput(float fixedDeltaTime, bool hasGroundPlane, float groundY)
        {
            FixedDeltaTime = fixedDeltaTime;
            HasGroundPlane = hasGroundPlane;
            GroundY = groundY;
        }

        public float FixedDeltaTime { get; }
        public bool HasGroundPlane { get; }
        public float GroundY { get; }
    }
}
