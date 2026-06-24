using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Immutable authored values required to begin one Phase 3 point.</summary>
    public readonly struct NextPointRequest
    {
        public NextPointRequest(HitterType server, Vector2 resetPosition, Vector2 launchVelocity)
        {
            Server = server;
            ResetPosition = resetPosition;
            LaunchVelocity = launchVelocity;
        }

        public HitterType Server { get; }
        public Vector2 ResetPosition { get; }
        public Vector2 LaunchVelocity { get; }
    }
}
