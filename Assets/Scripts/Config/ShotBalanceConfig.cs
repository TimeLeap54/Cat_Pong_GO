using System;
using CatTennis.Rebuild.Shot;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    /// <summary>Stores player shot tuning data only.</summary>
    public sealed class ShotBalanceConfig : ScriptableObject
    {
        [SerializeField] private float safeHorizontalSpeed = 6f;
        [SerializeField] private float safeVerticalSpeed = 6f;
        [SerializeField] private float smashHorizontalSpeed = 9f;
        [SerializeField] private float smashVerticalSpeed = -2f;
        [SerializeField] private float maxHorizontalSpeed = 20f;
        [SerializeField] private float maxRiseSpeed = 20f;
        [SerializeField] private float maxFallSpeed = 20f;

        public ShotSettings CreateSettings()
        {
            return new ShotSettings(
                safeHorizontalSpeed,
                safeVerticalSpeed,
                smashHorizontalSpeed,
                smashVerticalSpeed,
                maxHorizontalSpeed,
                maxRiseSpeed,
                maxFallSpeed);
        }

        public void Configure(
            float safeX,
            float safeY,
            float smashX,
            float smashY,
            float maxX,
            float maxRise,
            float maxFall)
        {
            safeHorizontalSpeed = safeX;
            safeVerticalSpeed = safeY;
            smashHorizontalSpeed = smashX;
            smashVerticalSpeed = smashY;
            maxHorizontalSpeed = maxX;
            maxRiseSpeed = maxRise;
            maxFallSpeed = maxFall;
        }

        public void ValidateOrThrow()
        {
            CreateSettings().Validate();
        }

        private void OnValidate()
        {
            try
            {
                ValidateOrThrow();
            }
            catch (Exception exception)
            {
                Debug.LogError($"ShotBalanceConfig '{name}' is invalid: {exception.Message}", this);
            }
        }
    }
}
