using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class OpponentManualHitboxTrigger : MonoBehaviour
    {
        [SerializeField] private ManualHitboxKind kind;
        [SerializeField] private BoxCollider2D box;
        [SerializeField] private OpponentManualHitboxController controller;

        public ManualHitboxKind Kind => kind;
        public BoxCollider2D Box => box;

        public void Configure(ManualHitboxKind newKind, OpponentManualHitboxController newController)
        {
            kind = newKind;
            controller = newController;
            EnsureCollider();
        }

        private void Reset()
        {
            box = GetComponent<BoxCollider2D>();
        }

        private void Awake()
        {
            EnsureCollider();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ForwardOverlap(other);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ForwardOverlap(other);
        }

        private void ForwardOverlap(Collider2D other)
        {
            if (box == null || !box.enabled || controller == null)
            {
                return;
            }

            controller.HandleOverlap(kind, other);
        }

        private void EnsureCollider()
        {
            if (box == null)
            {
                box = GetComponent<BoxCollider2D>();
            }

            if (box == null)
            {
                return;
            }

            box.isTrigger = true;
            box.enabled = false;
        }
    }
}
