using UnityEngine;

namespace CatTennis.Rebuild.Shot
{
    public sealed class ShotIntentResolver
    {
        public ShotIntent Resolve(Vector2 aim, int facingDirection, bool smash,
            float deadZone, float verticalThreshold)
        {
            if (smash)
            {
                return ShotIntent.Smash;
            }

            if (aim.y > verticalThreshold)
            {
                return ShotIntent.Lob;
            }

            float localForward = aim.x * facingDirection;
            if (localForward > deadZone)
            {
                return ShotIntent.Deep;
            }

            return localForward < -deadZone ? ShotIntent.Drop : ShotIntent.SafeReturn;
        }
    }
}
