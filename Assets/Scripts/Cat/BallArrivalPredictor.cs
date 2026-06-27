using System.Collections.Generic;
using CatTennis.BallPhysics.Core;

namespace CatTennis.Rebuild.Cat
{
    public sealed class BallArrivalPredictor
    {
        private readonly BallPhysicsModel model = new BallPhysicsModel();
        public IReadOnlyList<BallArrivalCandidate> Predict(BallSnapshot start,
            BallPhysicsSettings settings, float groundY, float fixedStep, float horizon,
            float courtMinX, float courtMaxX, float jumpThreshold)
        {
            var result = new List<BallArrivalCandidate>();
            BallSnapshot current=start; int bounces=0; int count=(int)(horizon/fixedStep);
            for(int i=1;i<=count && current.IsActive;i++)
            {
                BallStepResult step=model.Step(current,settings,new BallStepInput(fixedStep,true,groundY));
                current=step.NextSnapshot; if(step.DidBounce||step.DidSettle)bounces++;
                if(bounces>=2) break;
                if(current.PositionX>=courtMinX && current.PositionX<=courtMaxX &&
                   current.PositionY>=groundY+settings.BallRadius && current.PositionY<=groundY+3.4f)
                    result.Add(new BallArrivalCandidate(
                        new UnityEngine.Vector2(current.PositionX,current.PositionY),i*fixedStep,
                        current.StepIndex,bounces,current.PositionY>jumpThreshold));
            }
            return result;
        }
    }
}
