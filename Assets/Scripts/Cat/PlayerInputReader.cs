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
        private bool mobileLeftHeld;
        private bool mobileRightHeld;
        private Vector2 mobileMove;

        private void Update()
        {
            float keyboardMoveX = Input.GetAxisRaw("Horizontal");
            float buttonMoveX = mobileLeftHeld == mobileRightHeld ? 0f : mobileLeftHeld ? -1f : 1f;
            float mobileMoveX = Mathf.Abs(mobileMove.x) > 0.01f ? mobileMove.x : buttonMoveX;
            moveX = Mathf.Abs(keyboardMoveX) > 0.01f ? keyboardMoveX : mobileMoveX;

            float keyboardAimY = Input.GetAxisRaw("Vertical");
            aimY = Mathf.Abs(keyboardAimY) > 0.01f ? keyboardAimY : mobileMove.y;

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

        public void SetMobileLeftHeld(bool held)
        {
            mobileLeftHeld = held;
        }

        public void SetMobileRightHeld(bool held)
        {
            mobileRightHeld = held;
        }

        public void SetMobileMove(Vector2 move)
        {
            mobileMove = Vector2.ClampMagnitude(move, 1f);
        }

        public void PressMobileJump()
        {
            jumpPressed = true;
        }

        public void PressMobileSwing()
        {
            swingPressed = true;
        }

        public void PressMobileSmash()
        {
            smashPressed = true;
        }
    }
}
