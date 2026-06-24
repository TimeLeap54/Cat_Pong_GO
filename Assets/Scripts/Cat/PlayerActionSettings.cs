using System;

namespace CatTennis.Rebuild.Cat
{
    public readonly struct PlayerActionSettings
    {
        public PlayerActionSettings(
            int normalStartupTicks,
            int normalActiveTicks,
            int normalRecoveryTicks,
            int smashStartupTicks,
            int smashActiveTicks,
            int smashRecoveryTicks)
        {
            NormalStartupTicks = normalStartupTicks;
            NormalActiveTicks = normalActiveTicks;
            NormalRecoveryTicks = normalRecoveryTicks;
            SmashStartupTicks = smashStartupTicks;
            SmashActiveTicks = smashActiveTicks;
            SmashRecoveryTicks = smashRecoveryTicks;
            Validate();
        }

        public int NormalStartupTicks { get; }
        public int NormalActiveTicks { get; }
        public int NormalRecoveryTicks { get; }
        public int SmashStartupTicks { get; }
        public int SmashActiveTicks { get; }
        public int SmashRecoveryTicks { get; }

        public void Validate()
        {
            if (NormalStartupTicks <= 0 || NormalActiveTicks <= 0 || NormalRecoveryTicks <= 0 ||
                SmashStartupTicks <= 0 || SmashActiveTicks <= 0 || SmashRecoveryTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(PlayerActionSettings));
            }
        }
    }
}
