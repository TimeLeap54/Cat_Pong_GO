using UnityEngine;

namespace CatTennis.Rebuild.State
{
    public struct AiTacticalContext
    {
        public Vector2 playerPosition;
        public Vector2 opponentPosition;
        public Vector2 ballPosition;
        public Vector2 predictedBallArrival;
        public bool playerNearNet;
        public bool playerDeepCourt;
        public bool playerLeftSide;
        public bool playerRightSide;
        public bool playerRecentlyJumped;
        public bool playerOutOfPosition;
        public int rallyCount;
        public bool ballArrivalRequiresJump;
        public bool opponentNearNet;
    }
}
