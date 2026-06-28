using System;
using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    public sealed class ShotTrajectoryModel
    {
        public ShotTrajectoryResult ResolveApex(float x, float y, float targetX, float targetY,
            float apexY, ShotSettings settings)
        {
            if (apexY <= Math.Max(y, targetY)) return Invalid(FailureReason.InvalidEvent);
            float g = settings.Gravity;
            float vy = (float)Math.Sqrt(2f * -g * (apexY - y));
            float discriminant = vy * vy - 2f * g * (y - targetY);
            if (discriminant < 0f) return Invalid(FailureReason.InvalidEvent);
            float time = (-vy - (float)Math.Sqrt(discriminant)) / g;
            return Build(x, y, targetX, targetY, time, vy, settings);
        }

        public ShotTrajectoryResult ResolveTime(float x, float y, float targetX, float targetY,
            float flightTime, ShotSettings settings)
        {
            if (flightTime <= 0f) return Invalid(FailureReason.InvalidEvent);
            float vy = (targetY - y - 0.5f * settings.Gravity * flightTime * flightTime) / flightTime;
            return Build(x, y, targetX, targetY, flightTime, vy, settings);
        }

        private static ShotTrajectoryResult Build(float x, float y, float targetX, float targetY,
            float time, float vy, ShotSettings settings)
        {
            float vx = (targetX - x) / time;
            float apex = vy > 0f ? y - vy * vy / (2f * settings.Gravity) : y;
            float netTime = Math.Abs(vx) < 0.0001f ? -1f : (settings.NetX - x) / vx;
            float netY = netTime > 0f && netTime < time
                ? y + vy * netTime + 0.5f * settings.Gravity * netTime * netTime
                : float.PositiveInfinity;
            bool speedValid = Math.Abs(vx) <= settings.MaxHorizontalSpeed &&
                              vy <= settings.MaxRiseSpeed && vy >= -settings.MaxFallSpeed;
            bool netValid = netY >= settings.NetHeight + 0.15f + settings.Clearance;
            bool valid = speedValid && netValid && IsFinite(vx) && IsFinite(vy);
            return new ShotTrajectoryResult(vx, vy, targetX, targetY, time, apex, netY,
                valid, valid ? FailureReason.None : netValid ? FailureReason.InvalidEvent : FailureReason.FailedToClear);
        }

        private static ShotTrajectoryResult Invalid(FailureReason reason) =>
            new ShotTrajectoryResult(0f, 0f, 0f, 0f, 0f, 0f, 0f, false, reason);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
