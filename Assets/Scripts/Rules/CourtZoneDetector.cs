using System;
using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Rules
{
    /// <summary>Converts physical results into ordered observations without judging rules.</summary>
    public sealed class CourtZoneDetector : MonoBehaviour
    {
        private static readonly IReadOnlyList<CourtObservation> NoObservations =
            Array.Empty<CourtObservation>();

        [SerializeField] private CourtGeometryConfig geometry;

        private bool groundLatched;
        private bool settleLatched;
        private bool boundaryLatched;
        private long lastProcessedStepIndex = -1;

        public void Initialize(CourtGeometryConfig courtGeometry)
        {
            geometry = courtGeometry;
            geometry?.ValidateOrThrow();
            ResetLatches();
        }

        public IReadOnlyList<CourtObservation> Evaluate(BallStepResult result)
        {
            RequireGeometry();
            BallSnapshot snapshot = result.NextSnapshot;
            if (snapshot.StepIndex <= lastProcessedStepIndex)
            {
                return NoObservations;
            }

            lastProcessedStepIndex = snapshot.StepIndex;
            bool insideBoundary = IsInsideWorld(snapshot);
            ReleaseLatches(result, snapshot, insideBoundary);

            bool realGroundContact = result.DidBounce || result.DidSettle;
            bool emitGround = realGroundContact && !groundLatched;
            bool emitSettled = result.DidSettle && !settleLatched;
            bool emitBoundary = !emitGround && !insideBoundary && !boundaryLatched;

            if (!emitGround && !emitSettled && !emitBoundary)
            {
                return NoObservations;
            }

            List<CourtObservation> observations = new List<CourtObservation>(3);
            if (emitGround)
            {
                groundLatched = true;
                observations.Add(new CourtObservation(
                    CourtObservationType.GroundTouch,
                    snapshot.StepIndex,
                    ClassifyGround(snapshot.PositionX)));
            }

            if (emitSettled)
            {
                settleLatched = true;
                observations.Add(new CourtObservation(
                    CourtObservationType.BallSettled,
                    snapshot.StepIndex));
            }

            if (emitBoundary)
            {
                boundaryLatched = true;
                observations.Add(new CourtObservation(
                    CourtObservationType.BoundaryExit,
                    snapshot.StepIndex,
                    boundaryType: ClassifyBoundary(snapshot)));
            }

            return observations;
        }

        public void ResetLatches()
        {
            groundLatched = false;
            settleLatched = false;
            boundaryLatched = false;
            lastProcessedStepIndex = -1;
        }

        private void ReleaseLatches(
            BallStepResult result,
            BallSnapshot snapshot,
            bool insideBoundary)
        {
            if (groundLatched && !result.HadGroundContact && snapshot.IsActive)
            {
                groundLatched = false;
            }

            if (boundaryLatched && insideBoundary)
            {
                boundaryLatched = false;
            }
        }

        private CourtArea ClassifyGround(float positionX)
        {
            if (positionX >= geometry.PlayerCourtMinX - geometry.LineTolerance &&
                positionX <= geometry.PlayerCourtMaxX + geometry.LineTolerance)
            {
                return CourtArea.PlayerCourt;
            }

            if (positionX >= geometry.OpponentCourtMinX - geometry.LineTolerance &&
                positionX <= geometry.OpponentCourtMaxX + geometry.LineTolerance)
            {
                return CourtArea.OpponentCourt;
            }

            return CourtArea.Out;
        }

        private bool IsInsideWorld(BallSnapshot snapshot)
        {
            bool insideX = snapshot.PositionX >= geometry.WorldMinX &&
                           snapshot.PositionX <= geometry.WorldMaxX;
            bool aboveKillPlane = !geometry.UseKillPlane ||
                                  snapshot.PositionY >= geometry.KillPlaneY;
            return insideX && aboveKillPlane;
        }

        private BoundaryType ClassifyBoundary(BallSnapshot snapshot)
        {
            if (snapshot.PositionX < geometry.WorldMinX)
            {
                return BoundaryType.PlayerBack;
            }

            if (snapshot.PositionX > geometry.WorldMaxX)
            {
                return BoundaryType.OpponentBack;
            }

            return BoundaryType.KillPlane;
        }

        private void RequireGeometry()
        {
            if (geometry == null)
            {
                throw new InvalidOperationException("CourtGeometryConfig is required.");
            }

            geometry.ValidateOrThrow();
        }
    }
}
