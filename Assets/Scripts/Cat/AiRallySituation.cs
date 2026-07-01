namespace CatTennis.Rebuild.Cat
{
    public readonly struct AiRallySituation
    {
        public AiRallySituation(
            AiRallyPhase phase,
            long pointId,
            int rallyHitCount,
            bool receiving,
            bool serveToss,
            bool serveReturn = false,
            AiDefenseStance defenseStance = AiDefenseStance.None)
        {
            Phase = phase;
            PointId = pointId;
            RallyHitCount = rallyHitCount;
            Receiving = receiving;
            ServeToss = serveToss;
            ServeReturn = serveReturn;
            DefenseStance = defenseStance;
        }

        public AiRallyPhase Phase { get; }
        public long PointId { get; }
        public int RallyHitCount { get; }
        public bool Receiving { get; }
        public bool ServeToss { get; }
        public bool ServeReturn { get; }
        public AiDefenseStance DefenseStance { get; }
        public bool CanPlanDefense => Phase == AiRallyPhase.Defense || Phase == AiRallyPhase.ServeReceive;
        public bool CanAttack => CanPlanDefense;

        public AiRallySituation WithDefenseStance(AiDefenseStance stance)
        {
            return new AiRallySituation(Phase, PointId, RallyHitCount, Receiving, ServeToss, ServeReturn, stance);
        }
    }
}
