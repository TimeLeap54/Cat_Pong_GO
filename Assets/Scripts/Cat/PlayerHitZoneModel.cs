namespace CatTennis.Rebuild.Cat
{
    /// <summary>Pure local-space hit-zone test with inclusive boundaries.</summary>
    public sealed class PlayerHitZoneModel
    {
        public bool Contains(
            HitZoneDefinition zone,
            float playerX,
            float playerY,
            int facingDirection,
            float ballX,
            float ballY)
        {
            if (facingDirection != -1 && facingDirection != 1)
            {
                return false;
            }

            float localX = (ballX - playerX) * facingDirection;
            float localY = ballY - playerY;
            if (zone.RequireForward && localX < 0f)
            {
                return false;
            }

            return localX >= zone.CenterX - zone.HalfWidth &&
                   localX <= zone.CenterX + zone.HalfWidth &&
                   localY >= zone.CenterY - zone.HalfHeight &&
                   localY <= zone.CenterY + zone.HalfHeight;
        }
    }
}
