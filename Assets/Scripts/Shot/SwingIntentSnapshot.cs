using System;
using UnityEngine;

namespace CatTennis.Rebuild.Shot
{
    public readonly struct SwingIntentSnapshot
    {
        public SwingIntentSnapshot(long pointId, long swingId, long inputTick,
            Vector2 aimDirection, int facingDirection, ShotIntent intent)
        {
            if (pointId <= 0 || swingId <= 0 || inputTick < 0 ||
                (facingDirection != -1 && facingDirection != 1) ||
                intent == ShotIntent.Undefined)
            {
                throw new ArgumentException("Swing intent snapshot is invalid.");
            }

            PointId = pointId;
            SwingId = swingId;
            InputTick = inputTick;
            AimDirection = aimDirection;
            FacingDirection = facingDirection;
            Intent = intent;
        }

        public long PointId { get; }
        public long SwingId { get; }
        public long InputTick { get; }
        public Vector2 AimDirection { get; }
        public int FacingDirection { get; }
        public ShotIntent Intent { get; }
    }
}
