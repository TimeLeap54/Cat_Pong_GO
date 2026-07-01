using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class Phase6AIModelTests
    {
        [Test]
        public void DelayedObserverDoesNotRevealCurrentSnapshotEarly()
        {
            var observer=new DelayedBallObserver();
            observer.Record(1,0f,new BallSnapshot(1,2,3,4,true,1),BallPlayMode.Rally);
            Assert.That(observer.TryGet(.1f,.2f,out _),Is.False);
            Assert.That(observer.TryGet(.21f,.2f,out var observed),Is.True);
            Assert.That(observed.Snapshot.StepIndex,Is.EqualTo(1));
        }

        [Test]
        public void PredictorReturnsDeterministicTimeOrderedCandidates()
        {
            var predictor=new BallArrivalPredictor();
            var settings=new BallPhysicsSettings(-9.81f,.15f,.7f,1f,30f,30f,30f,.1f,.01f);
            var start=new BallSnapshot(-2f,2f,6f,3f,true,0);
            IReadOnlyList<BallArrivalCandidate> first=predictor.Predict(start,settings,0f,.02f,2.5f,.5f,8f,1.65f);
            IReadOnlyList<BallArrivalCandidate> second=predictor.Predict(start,settings,0f,.02f,2.5f,.5f,8f,1.65f);
            Assert.That(first,Is.Not.Empty); Assert.That(second.Count,Is.EqualTo(first.Count));
            for(int i=0;i<first.Count;i++)
            {
                Assert.That(second[i].Position,Is.EqualTo(first[i].Position));
                if(i>0) Assert.That(first[i].ArrivalTime,Is.GreaterThan(first[i-1].ArrivalTime));
            }
        }

        [Test]
        public void PlannerRejectsPhysicallyUnreachableCandidates()
        {
            var planner=new AIInterceptPlanner();
            var candidates=new[]{new BallArrivalCandidate(new UnityEngine.Vector2(8f,1f),.2f,1,0,false)};
            Assert.That(planner.TrySelect(candidates,1f,3f,0f,.05f,.35f,false,false,out _),Is.False);
        }

        [Test]
        public void PlannerPrefersOneBounceDeepLobDefenseOverEarlyVolley()
        {
            var planner=new AIInterceptPlanner();
            var candidates=new[]
            {
                new BallArrivalCandidate(new UnityEngine.Vector2(5.2f,2.6f),.35f,1,0,true),
                new BallArrivalCandidate(new UnityEngine.Vector2(6.3f,1.1f),1.05f,2,1,false)
            };

            Assert.That(planner.TrySelect(candidates,5f,4f,0f,.05f,.35f,false,
                AiDefenseStance.DeepLobDefense,false,out var selected),Is.True);
            Assert.That(selected.BounceCountBeforeArrival,Is.EqualTo(1));
        }

        [Test]
        public void PlannerPrefersOneBounceServeReceiveOverEarlyVolley()
        {
            var planner=new AIInterceptPlanner();
            var candidates=new[]
            {
                new BallArrivalCandidate(new UnityEngine.Vector2(7.7f,2.0f),.45f,1,0,false),
                new BallArrivalCandidate(new UnityEngine.Vector2(6.9f,1.0f),.95f,2,1,false)
            };

            Assert.That(planner.TrySelect(candidates,7.5f,4f,0f,.05f,.35f,false,
                AiDefenseStance.ServeReceive,false,out var selected),Is.True);
            Assert.That(selected.BounceCountBeforeArrival,Is.EqualTo(1));
        }
    }
}
