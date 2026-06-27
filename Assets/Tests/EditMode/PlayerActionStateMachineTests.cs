using CatTennis.Rebuild.Cat;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class PlayerActionStateMachineTests
    {
        private PlayerActionStateMachine machine;

        [SetUp]
        public void SetUp()
        {
            machine = new PlayerActionStateMachine(new PlayerActionSettings(2, 2, 2, 2, 2, 2));
        }

        [Test]
        public void JumpAndNormalSwingMaintainAirborneAndStartupTogether()
        {
            PlayerActionFrame frame = machine.Step(Input(jump: true, swing: true), true);

            Assert.That(frame.JumpRequested, Is.True);
            Assert.That(frame.LocomotionState, Is.EqualTo(LocomotionState.Airborne));
            Assert.That(frame.SwingState, Is.EqualTo(SwingState.NormalStartup));
        }

        [Test]
        public void JumpAndSmashMaintainAirborneAndStartupTogether()
        {
            PlayerActionFrame frame = machine.Step(Input(jump: true, smash: true), true);

            Assert.That(frame.LocomotionState, Is.EqualTo(LocomotionState.Airborne));
            Assert.That(frame.SwingState, Is.EqualTo(SwingState.SmashStartup));
        }

        [Test]
        public void SimultaneousNormalAndSmashStartsOnlySmash()
        {
            PlayerActionFrame frame = machine.Step(Input(swing: true, smash: true), true);

            Assert.That(frame.SwingState, Is.EqualTo(SwingState.SmashStartup));
            Assert.That(frame.SwingKind, Is.EqualTo(SwingKind.Smash));
            Assert.That(frame.SwingId, Is.EqualTo(1));
        }

        [Test]
        public void RecoveryInputDoesNotAdvanceSwingId()
        {
            machine.Step(Input(swing: true), true);
            machine.Step(default, true);
            machine.Step(default, true);
            machine.Step(default, true);

            PlayerActionFrame recovery = machine.Step(Input(swing: true, smash: true), true);

            Assert.That(recovery.SwingState, Is.EqualTo(SwingState.NormalRecovery));
            Assert.That(recovery.SwingId, Is.EqualTo(1));
        }

        [Test]
        public void ActiveLastTickIsEligibleAndRecoveryFirstTickIsNot()
        {
            machine.Step(Input(swing: true), true);
            machine.Step(default, true);
            PlayerActionFrame activeFirst = machine.Step(default, true);
            PlayerActionFrame activeLast = machine.Step(default, true);
            PlayerActionFrame recoveryFirst = machine.Step(default, true);

            Assert.That(activeFirst.IsHitActive, Is.True);
            Assert.That(activeLast.IsHitActive, Is.True);
            Assert.That(recoveryFirst.SwingState, Is.EqualTo(SwingState.NormalRecovery));
            Assert.That(recoveryFirst.IsHitActive, Is.False);
        }

        [Test]
        public void SingleJumpRequiresLeavingAndReturningToGround()
        {
            Assert.That(machine.Step(Input(jump: true), true).JumpRequested, Is.True);
            Assert.That(machine.Step(Input(jump: true), true).JumpRequested, Is.False);
            Assert.That(machine.Step(Input(jump: true), false).JumpRequested, Is.False);
            Assert.That(machine.Step(default, true).LocomotionState, Is.EqualTo(LocomotionState.Grounded));
            Assert.That(machine.Step(Input(jump: true), true).JumpRequested, Is.True);
        }

        [Test]
        public void JumpWatchdogRecoversGroundedStateAfterTimeout()
        {
            Assert.That(machine.Step(Input(jump: true), true).JumpRequested, Is.True);
            for (int i = 0; i < 9; i++)
            {
                PlayerActionFrame frame = machine.Step(default, true);
                Assert.That(frame.LocomotionState, Is.EqualTo(LocomotionState.Airborne));
            }
            PlayerActionFrame landedFrame = machine.Step(default, true);
            Assert.That(landedFrame.LocomotionState, Is.EqualTo(LocomotionState.Grounded));
        }

        private static PlayerInputFrame Input(
            bool jump = false,
            bool swing = false,
            bool smash = false)
        {
            return new PlayerInputFrame(0f, jump, swing, smash);
        }
    }
}
