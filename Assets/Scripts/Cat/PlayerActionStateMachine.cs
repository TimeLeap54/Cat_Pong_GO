using System;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Deterministic fixed-tick player action state independent of Unity physics.</summary>
    public sealed class PlayerActionStateMachine
    {
        private readonly PlayerActionSettings settings;
        private int swingTicksRemaining;
        private bool waitingToLeaveGround;
        private int waitingToLeaveGroundTicks;

        public PlayerActionStateMachine(PlayerActionSettings actionSettings)
        {
            actionSettings.Validate();
            settings = actionSettings;
            LocomotionState = LocomotionState.Grounded;
            SwingState = SwingState.Ready;
        }

        public LocomotionState LocomotionState { get; private set; }
        public SwingState SwingState { get; private set; }
        public SwingKind SwingKind { get; private set; }
        public long SwingId { get; private set; }

        public void Reset()
        {
            LocomotionState = LocomotionState.Grounded;
            SwingState = SwingState.Ready;
            SwingKind = SwingKind.None;
            swingTicksRemaining = 0;
            waitingToLeaveGround = false;
            waitingToLeaveGroundTicks = 0;
        }

        public PlayerActionFrame Step(PlayerInputFrame input, bool groundDetected)
        {
            AdvanceLocomotion(groundDetected);
            AdvanceSwing();

            bool jumpRequested = false;
            if (input.JumpPressed && LocomotionState == LocomotionState.Grounded)
            {
                LocomotionState = LocomotionState.Airborne;
                waitingToLeaveGround = true;
                waitingToLeaveGroundTicks = 10;
                jumpRequested = true;
            }

            if (SwingState == SwingState.Ready)
            {
                if (input.SmashPressed)
                {
                    StartSwing(SwingKind.Smash);
                }
                else if (input.SwingPressed)
                {
                    StartSwing(SwingKind.Normal);
                }
            }

            return new PlayerActionFrame(
                LocomotionState,
                SwingState,
                SwingKind,
                SwingId,
                jumpRequested,
                input.MoveX,
                input.AimDirection,
                input.InputTick);
        }

        private void AdvanceLocomotion(bool groundDetected)
        {
            if (waitingToLeaveGroundTicks > 0)
            {
                waitingToLeaveGroundTicks--;
                if (waitingToLeaveGroundTicks == 0)
                {
                    waitingToLeaveGround = false;
                }
            }

            if (!groundDetected)
            {
                LocomotionState = LocomotionState.Airborne;
                waitingToLeaveGround = false;
                waitingToLeaveGroundTicks = 0;
            }
            else if (!waitingToLeaveGround)
            {
                LocomotionState = LocomotionState.Grounded;
            }
        }

        private void AdvanceSwing()
        {
            if (SwingState == SwingState.Ready)
            {
                return;
            }

            swingTicksRemaining--;
            if (swingTicksRemaining > 0)
            {
                return;
            }

            switch (SwingState)
            {
                case SwingState.NormalStartup:
                    SetSwingState(SwingState.NormalActive, settings.NormalActiveTicks);
                    break;
                case SwingState.NormalActive:
                    SetSwingState(SwingState.NormalRecovery, settings.NormalRecoveryTicks);
                    break;
                case SwingState.NormalRecovery:
                    SetReady();
                    break;
                case SwingState.SmashStartup:
                    SetSwingState(SwingState.SmashActive, settings.SmashActiveTicks);
                    break;
                case SwingState.SmashActive:
                    SetSwingState(SwingState.SmashRecovery, settings.SmashRecoveryTicks);
                    break;
                case SwingState.SmashRecovery:
                    SetReady();
                    break;
                default:
                    throw new InvalidOperationException("Unknown swing state.");
            }
        }

        private void StartSwing(SwingKind kind)
        {
            if (SwingId == long.MaxValue)
            {
                throw new InvalidOperationException("Swing id cannot advance beyond Int64.MaxValue.");
            }

            SwingId++;
            SwingKind = kind;
            if (kind == SwingKind.Smash)
            {
                SetSwingState(SwingState.SmashStartup, settings.SmashStartupTicks);
            }
            else
            {
                SetSwingState(SwingState.NormalStartup, settings.NormalStartupTicks);
            }
        }

        private void SetSwingState(SwingState state, int ticks)
        {
            SwingState = state;
            swingTicksRemaining = ticks;
        }

        private void SetReady()
        {
            SwingState = SwingState.Ready;
            SwingKind = SwingKind.None;
            swingTicksRemaining = 0;
        }
    }
}
