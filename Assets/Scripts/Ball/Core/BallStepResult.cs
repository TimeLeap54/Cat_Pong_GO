namespace CatTennis.BallPhysics.Core
{
    /// <summary>State and neutral physical observations produced by one step.</summary>
    public readonly struct BallStepResult
    {
        public BallStepResult(
            BallSnapshot nextSnapshot,
            bool hadGroundContact,
            bool didBounce,
            bool didSettle,
            float impactSpeed,
            float contactY)
        {
            NextSnapshot = nextSnapshot;
            HadGroundContact = hadGroundContact;
            DidBounce = didBounce;
            DidSettle = didSettle;
            ImpactSpeed = impactSpeed;
            ContactY = contactY;
        }

        public BallSnapshot NextSnapshot { get; }
        public bool HadGroundContact { get; }
        public bool DidBounce { get; }
        public bool DidSettle { get; }
        public float ImpactSpeed { get; }
        public float ContactY { get; }
    }
}
