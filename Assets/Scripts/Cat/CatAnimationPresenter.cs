using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Translates player state into Animator parameters without owning gameplay.</summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class CatAnimationPresenter : MonoBehaviour
    {
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int JSwingId = Animator.StringToHash("JSwing");
        private static readonly int KSmashId = Animator.StringToHash("KSmash");

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerCatController player;
        [SerializeField] private OpponentAIController opponent;

        private long presentedSwingId = -1;
        private LocomotionState previousLocomotion = LocomotionState.Grounded;

        public PlayerCatController Player => player;

        private void Awake()
        {
            EnsureComponents();
        }

        private void LateUpdate()
        {
            if (player == null && opponent == null)
            {
                return;
            }

            PlayerActionFrame action = player != null ? player.CurrentAction : opponent.CurrentAction;
            animator.SetFloat(MoveXId, action.MoveX);
            animator.SetBool(GroundedId, action.LocomotionState == LocomotionState.Grounded);

            if (previousLocomotion == LocomotionState.Grounded &&
                action.LocomotionState == LocomotionState.Airborne)
            {
                animator.SetTrigger(JumpId);
            }

            if (action.SwingId != presentedSwingId && IsStartup(action.SwingState))
            {
                animator.SetTrigger(action.SwingKind == SwingKind.Smash ? KSmashId : JSwingId);
                presentedSwingId = action.SwingId;
            }

            previousLocomotion = action.LocomotionState;
        }

        public void Configure(PlayerCatController source)
        {
            player = source;
            ResetPresentation();
        }

        public void Configure(OpponentAIController source)
        {
            opponent = source; player = null; ResetPresentation();
        }

        public void ResetPresentation()
        {
            EnsureComponents();
            presentedSwingId = -1;
            previousLocomotion = LocomotionState.Grounded;
            animator.ResetTrigger(JumpId);
            animator.ResetTrigger(JSwingId);
            animator.ResetTrigger(KSmashId);
            animator.SetFloat(MoveXId, 0f);
            animator.SetBool(GroundedId, true);
        }

        private void EnsureComponents()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private static bool IsStartup(SwingState state)
        {
            return state == SwingState.NormalStartup || state == SwingState.SmashStartup;
        }
    }
}
