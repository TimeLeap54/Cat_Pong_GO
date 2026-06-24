using CatTennis.Rebuild.Cat;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class PlayerHitZoneModelTests
    {
        private readonly PlayerHitZoneModel model = new PlayerHitZoneModel();
        private readonly HitZoneDefinition normal = new HitZoneDefinition(1f, 1f, 1f, 1f, true);

        [TestCase(0f, 0f)]
        [TestCase(2f, 2f)]
        [TestCase(1f, 1f)]
        public void InclusiveNormalZoneAcceptsBoundaries(float ballX, float ballY)
        {
            Assert.That(model.Contains(normal, 0f, 0f, 1, ballX, ballY), Is.True);
        }

        [Test]
        public void NormalZoneRejectsBallBehindPlayer()
        {
            Assert.That(model.Contains(normal, 0f, 0f, 1, -0.01f, 1f), Is.False);
        }

        [Test]
        public void FacingLeftMirrorsZone()
        {
            Assert.That(model.Contains(normal, 5f, 0f, -1, 4f, 1f), Is.True);
            Assert.That(model.Contains(normal, 5f, 0f, -1, 6f, 1f), Is.False);
        }

        [Test]
        public void SmashZoneCanUseDifferentHighArea()
        {
            HitZoneDefinition smash = new HitZoneDefinition(0.5f, 2f, 1.5f, 1f, false);
            Assert.That(model.Contains(smash, 0f, 0f, 1, -0.5f, 2.5f), Is.True);
            Assert.That(model.Contains(smash, 0f, 0f, 1, 0f, 0.5f), Is.False);
        }
    }
}
