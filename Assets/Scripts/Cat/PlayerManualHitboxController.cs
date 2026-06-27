using CatTennis.Rebuild.Flow;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class PlayerManualHitboxController : MonoBehaviour
    {
        [SerializeField] private Transform manualHitboxesRoot;
        [SerializeField] private ManualHitboxTrigger normalHitbox;
        [SerializeField] private ManualHitboxTrigger smashHitbox;
        [SerializeField] private PlayerHitDetector hitDetector;
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private string lastRejectReason = string.Empty;

        private ManualHitboxKind? activeKind;
        private readonly Collider2D[] overlapBuffer = new Collider2D[8];

        public string LastRejectReason => lastRejectReason;
        public ManualHitboxKind? ActiveKind => activeKind;
        public bool HasGameplayHitboxes => normalHitbox != null && smashHitbox != null;
        public Transform ManualHitboxesRoot => manualHitboxesRoot;
        public ManualHitboxTrigger NormalHitbox => normalHitbox;
        public ManualHitboxTrigger SmashHitbox => smashHitbox;

        public void Configure(
            ManualHitboxTrigger normal,
            ManualHitboxTrigger smash,
            PlayerHitDetector detector,
            Transform hitboxesRoot = null)
        {
            normalHitbox = normal;
            smashHitbox = smash;
            hitDetector = detector;
            if (hitboxesRoot != null)
            {
                manualHitboxesRoot = hitboxesRoot;
            }
            ConfigureTrigger(normalHitbox, ManualHitboxKind.Normal);
            ConfigureTrigger(smashHitbox, ManualHitboxKind.Smash);
            DisableAll();
        }

        public void Bind(
            PlayerHitDetector detector,
            Transform hitboxesRoot = null)
        {
            hitDetector = detector;
            if (hitboxesRoot != null)
            {
                manualHitboxesRoot = hitboxesRoot;
            }

            ConfigureTrigger(normalHitbox, ManualHitboxKind.Normal);
            ConfigureTrigger(smashHitbox, ManualHitboxKind.Smash);
            DisableAll();
        }

        public void ApplyAction(PlayerActionFrame action, int facingDirection)
        {
            MirrorRoot(facingDirection);

            if (action.SwingState == SwingState.NormalActive &&
                action.SwingKind == SwingKind.Normal)
            {
                if (hitDetector != null && hitDetector.IsServeToss)
                {
                    EnableSmash();
                }
                else
                {
                    EnableNormal();
                }
                ProbeActiveOverlaps();
                return;
            }

            if (action.SwingState == SwingState.SmashActive &&
                action.SwingKind == SwingKind.Smash)
            {
                EnableSmash();
                ProbeActiveOverlaps();
                return;
            }

            DisableAll();
        }

        public void DisableAll()
        {
            SetEnabled(normalHitbox, false);
            SetEnabled(smashHitbox, false);
            activeKind = null;
        }

        public void EnableNormal()
        {
            DisableAll();
            SetEnabled(normalHitbox, true);
            activeKind = ManualHitboxKind.Normal;
        }

        public void EnableSmash()
        {
            DisableAll();
            SetEnabled(smashHitbox, true);
            activeKind = ManualHitboxKind.Smash;
        }

        public void HandleOverlap(ManualHitboxKind kind, Collider2D other)
        {
            if (other == null)
            {
                lastRejectReason = "NotBall";
                return;
            }

            if (activeKind != kind)
            {
                lastRejectReason = "WrongHitboxKind";
                return;
            }

            if (hitDetector == null || !hitDetector.TryHandleManualHitboxOverlap(kind, other))
            {
                lastRejectReason = hitDetector == null ? "NoManualHitbox" : hitDetector.LastRejectReason;
                return;
            }

            lastRejectReason = string.Empty;
        }

        private void Awake()
        {
            ConfigureTrigger(normalHitbox, ManualHitboxKind.Normal);
            ConfigureTrigger(smashHitbox, ManualHitboxKind.Smash);
            DisableAll();
        }

        private void OnDisable()
        {
            DisableAll();
        }

        private void MirrorRoot(int facingDirection)
        {
            if (manualHitboxesRoot == null || (facingDirection != -1 && facingDirection != 1))
            {
                return;
            }

            Vector3 scale = manualHitboxesRoot.localScale;
            float magnitude = Mathf.Abs(scale.x);
            manualHitboxesRoot.localScale = new Vector3(
                magnitude * facingDirection,
                scale.y,
                scale.z);
        }

        private void ConfigureTrigger(ManualHitboxTrigger trigger, ManualHitboxKind kind)
        {
            if (trigger != null)
            {
                trigger.Configure(kind, this);
            }
        }

        private void ProbeActiveOverlaps()
        {
            ManualHitboxTrigger active = GetActiveHitbox();
            if (active == null || active.Box == null || !active.Box.enabled)
            {
                return;
            }

            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = true
            };
            filter.SetLayerMask(Physics2D.AllLayers);
            int count = active.Box.OverlapCollider(filter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                HandleOverlap(active.Kind, overlapBuffer[i]);
                overlapBuffer[i] = null;
            }
        }

        private ManualHitboxTrigger GetActiveHitbox()
        {
            if (activeKind == ManualHitboxKind.Normal) return normalHitbox;
            if (activeKind == ManualHitboxKind.Smash) return smashHitbox;
            return null;
        }

        private static void SetEnabled(ManualHitboxTrigger hitbox, bool enabled)
        {
            if (hitbox == null || hitbox.Box == null)
            {
                return;
            }

            hitbox.Box.enabled = enabled;
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            DrawHitbox(normalHitbox, ManualHitboxKind.Normal);
            DrawHitbox(smashHitbox, ManualHitboxKind.Smash);
        }

        private void DrawHitbox(ManualHitboxTrigger hitbox, ManualHitboxKind kind)
        {
            if (hitbox == null || hitbox.Box == null)
            {
                return;
            }

            Gizmos.color = activeKind == kind
                ? new Color(0.1f, 1f, 0.1f, 0.85f)
                : new Color(1f, 1f, 1f, 0.35f);
            Bounds bounds = hitbox.Box.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
