using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Keyboard debug input source; gameplay systems consume unified frames.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private float moveX;
        private bool jumpPressed;
        private bool swingPressed;
        private bool smashPressed;

        private void Update()
        {
            moveX = Input.GetAxisRaw("Horizontal");
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
                smashPressed);
            jumpPressed = false;
            swingPressed = false;
            smashPressed = false;
            return frame;
        }

        public void InjectDebugFrame(PlayerInputFrame frame)
        {
            moveX = frame.MoveX;
            jumpPressed |= frame.JumpPressed;
            swingPressed |= frame.SwingPressed;
            smashPressed |= frame.SmashPressed;
        }
    }
}
