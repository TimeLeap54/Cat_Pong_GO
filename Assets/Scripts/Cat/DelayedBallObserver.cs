using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;

namespace CatTennis.Rebuild.Cat
{
    public readonly struct DelayedBallObservation
    {
        public DelayedBallObservation(long pointId, long observationId, float time,
            BallSnapshot snapshot, BallPlayMode mode)
        { PointId=pointId; ObservationId=observationId; Time=time; Snapshot=snapshot; PlayMode=mode; }
        public long PointId { get; } public long ObservationId { get; } public float Time { get; }
        public BallSnapshot Snapshot { get; } public BallPlayMode PlayMode { get; }
    }
    public sealed class DelayedBallObserver
    {
        private readonly Queue<DelayedBallObservation> buffer = new Queue<DelayedBallObservation>();
        private long nextId;
        public void Record(long pointId, float time, BallSnapshot snapshot, BallPlayMode mode)
            => buffer.Enqueue(new DelayedBallObservation(pointId, ++nextId, time, snapshot, mode));
        public bool TryGet(float now, float delay, out DelayedBallObservation observation)
        {
            observation = default; bool found=false;
            while (buffer.Count > 0 && buffer.Peek().Time <= now-delay)
            { observation=buffer.Dequeue(); found=true; }
            return found;
        }
        public void Reset() { buffer.Clear(); nextId=0; }
    }
}
