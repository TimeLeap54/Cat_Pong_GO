using UnityEngine;
namespace CatTennis.Rebuild.Cat
{
    public readonly struct BallArrivalCandidate
    {
        public BallArrivalCandidate(Vector2 position, float arrivalTime, long stepIndex,
            int bounceCount, bool requiresJump)
        { Position=position; ArrivalTime=arrivalTime; StepIndex=stepIndex;
          BounceCountBeforeArrival=bounceCount; RequiresJump=requiresJump; }
        public Vector2 Position { get; } public float ArrivalTime { get; }
        public long StepIndex { get; } public int BounceCountBeforeArrival { get; }
        public bool RequiresJump { get; }
    }
}
