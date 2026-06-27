namespace CatTennis.Rebuild.Cat
{
    public readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(
            float moveX,
            bool jumpPressed,
            bool swingPressed,
            bool smashPressed,
            float aimY = 0f,
            long inputTick = 0)
        {
            MoveX = moveX < -1f ? -1f : moveX > 1f ? 1f : moveX;
            JumpPressed = jumpPressed;
            SwingPressed = swingPressed;
            SmashPressed = smashPressed;
            AimDirection = new UnityEngine.Vector2(MoveX, aimY < -1f ? -1f : aimY > 1f ? 1f : aimY);
            InputTick = inputTick;
        }

        public float MoveX { get; }
        public bool JumpPressed { get; }
        public bool SwingPressed { get; }
        public bool SmashPressed { get; }
        public UnityEngine.Vector2 AimDirection { get; }
        public long InputTick { get; }
    }
}
