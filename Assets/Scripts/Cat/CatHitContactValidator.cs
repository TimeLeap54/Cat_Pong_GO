using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class CatHitContactValidator
    {
        private readonly PlayerHitZoneModel zoneModel = new PlayerHitZoneModel();
        public bool TryCreate(long pointId, PlayerActionFrame action, HitterType hitter,
            ShotIntent intent, Vector2 actorPosition, int facing, BallSnapshot ball,
            BallPlayMode playMode, HitZoneDefinition normal, HitZoneDefinition smash,
            out HitContact contact, float hitHeightRatio = 0.5f, bool isCounteringSmash = false)
        {
            contact = default;
            if (playMode != BallPlayMode.Rally || !ball.IsActive || !action.IsHitActive)
                return false;
            HitZoneDefinition zone = action.SwingKind == SwingKind.Smash ? smash : normal;
            if (!zoneModel.Contains(zone, actorPosition.x, actorPosition.y, facing,
                    ball.PositionX, ball.PositionY)) return false;
            contact = new HitContact(pointId, action.SwingId, ball.StepIndex, hitter,
                intent, actorPosition, ball, facing, action.InputTick, playMode == BallPlayMode.ServeToss, isCounteringSmash, hitHeightRatio);
            return true;
        }
    }
}
