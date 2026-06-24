namespace CatTennis.Rebuild.Cat
{
    public readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(
            float moveX,
            bool jumpPressed,
            bool swingPressed,
            bool smashPressed)
        {
            MoveX = moveX < -1f ? -1f : moveX > 1f ? 1f : moveX;
            JumpPressed = jumpPressed;
            SwingPressed = swingPressed;
            SmashPressed = smashPressed;
        }

        public float MoveX { get; }
        public bool JumpPressed { get; }
        public bool SwingPressed { get; }
        public bool SmashPressed { get; }
    }
}
