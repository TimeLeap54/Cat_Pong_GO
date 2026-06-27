using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Keyboard debug input source; gameplay systems consume unified frames.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private float moveX;
        private float aimY;
        private long inputTick;
        private bool jumpPressed;
        private bool swingPressed;
        private bool smashPressed;

        private void Update()
        {
            moveX = Input.GetAxisRaw("Horizontal");
            aimY = Input.GetAxisRaw("Vertical");
            jumpPressed |= Input.GetKeyDown(KeyCode.Space);
            swingPressed |= Input.GetKeyDown(KeyCode.J);
            smashPressed |= Input.GetKeyDown(KeyCode.K);
        }

        public PlayerInputFrame ConsumeFrame()
        {
            PlayerInputFrame frame = new PlayerInputFrame(
                moveX,
                jumpPressed,
                swingPressed,
                smashPressed,
                aimY,
                ++inputTick);
            jumpPressed = false;
            swingPressed = false;
            smashPressed = false;
            return frame;
        }

        public void InjectDebugFrame(PlayerInputFrame frame)
        {
            moveX = frame.MoveX;
            aimY = frame.AimDirection.y;
            jumpPressed |= frame.JumpPressed;
            swingPressed |= frame.SwingPressed;
            smashPressed |= frame.SmashPressed;
        }
    }
}
