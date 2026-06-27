using CatTennis.Rebuild.Shot;
using UnityEngine;
namespace CatTennis.Rebuild.Cat
{
    public sealed class AISwingPlan
    {
        public AISwingPlan(long pointId,long planId,long observationId,long expectedStep,
            SwingKind kind,ShotIntent intent,float remaining,Vector2 intercept,bool jump)
        { PointId=pointId;PlanId=planId;SourceObservationId=observationId;ExpectedBallStepIndex=expectedStep;
          SwingKind=kind;Intent=intent;RemainingTime=remaining;InitialRemainingTime=remaining;InterceptPosition=intercept;JumpRequired=jump; }
        public long PointId{get;} public long PlanId{get;} public long SourceObservationId{get;}
        public long ExpectedBallStepIndex{get;} public SwingKind SwingKind{get;}
        public ShotIntent Intent{get;} public float RemainingTime{get;set;}
        public float InitialRemainingTime{get;}
        public Vector2 InterceptPosition{get;} public bool JumpRequired{get;} public bool Consumed{get;set;}
    }
}
