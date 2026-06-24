namespace CatTennis.Rebuild.Cat
{
    public readonly struct PlayerActionFrame
    {
        public PlayerActionFrame(
            LocomotionState locomotionState,
            SwingState swingState,
            SwingKind swingKind,
            long swingId,
            bool jumpRequested,
            float moveX)
        {
            LocomotionState = locomotionState;
            SwingState = swingState;
            SwingKind = swingKind;
            SwingId = swingId;
            JumpRequested = jumpRequested;
            MoveX = moveX;
        }

        public LocomotionState LocomotionState { get; }
        public SwingState SwingState { get; }
        public SwingKind SwingKind { get; }
        public long SwingId { get; }
        public bool JumpRequested { get; }
        public float MoveX { get; }
        public bool IsHitActive => SwingState == SwingState.NormalActive ||
                                   SwingState == SwingState.SmashActive;
    }
}
