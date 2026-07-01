using CatTennis.Rebuild.Shot;
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
        [SerializeField] private Transform racketPivot;
        [SerializeField] private SpriteRenderer racketRenderer;
        [SerializeField] private Sprite racketSprite;
        [SerializeField] private Vector2 racketPivotLocalPosition = new Vector2(0.24f, 0.28f);
        [SerializeField] private Vector2 racketSpriteLocalPosition = new Vector2(0f, 0.28f);
        [SerializeField] private float racketScale = 0.18f;
        [SerializeField] private float racketIdleAngle = -34f;
        [SerializeField] private float smashSwingStartAngle = 115f;
        [SerializeField] private float smashSwingEndAngle = -85f;
        [SerializeField] private float racketSwingDuration = 0.26f;
        [SerializeField] private float normalSwingSpinDegrees = 360f;

        private long presentedSwingId = -1;
        private long racketSwingId = -1;
        private float racketSwingStartTime;
        private SwingKind racketVisualSwingKind = SwingKind.None;
        private Vector3 authoredRacketPivotLocalPosition;
        private LocomotionState previousLocomotion = LocomotionState.Grounded;
        private bool capturedRacketIdleAngle;

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
            animator.speed = 1f;
            animator.SetFloat(MoveXId, action.MoveX);
            animator.SetBool(GroundedId, action.LocomotionState == LocomotionState.Grounded);

            if (previousLocomotion == LocomotionState.Grounded &&
                action.LocomotionState == LocomotionState.Airborne)
            {
                animator.SetTrigger(JumpId);
            }

            if (action.SwingId != presentedSwingId && IsStartup(action.SwingState))
            {
                SwingKind visualSwingKind = GetRacketVisualSwingKind(action);
                if (!IsOpponentJumpReceive(action))
                {
                    animator.SetTrigger(visualSwingKind == SwingKind.Smash ? KSmashId : JSwingId);
                }

                presentedSwingId = action.SwingId;
            }

            UpdateRacket(action);
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
            animator.speed = 1f;
            racketSwingId = -1;
            racketSwingStartTime = 0f;
            racketVisualSwingKind = SwingKind.None;
            UpdateRacket(new PlayerActionFrame(
                LocomotionState.Grounded,
                SwingState.Ready,
                SwingKind.None,
                0,
                false,
                0f));
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

            EnsureRacketVisual();
            CaptureAuthoredRacketIdleAngle();
        }

        private static bool IsStartup(SwingState state)
        {
            return state == SwingState.NormalStartup || state == SwingState.SmashStartup;
        }

        private void EnsureRacketVisual()
        {
            if (racketRenderer != null)
            {
                if (racketSprite != null && racketRenderer.sprite == null)
                {
                    racketRenderer.sprite = racketSprite;
                }

                if (racketPivot == null)
                {
                    racketPivot = racketRenderer.transform.parent;
                }

                return;
            }

            if (racketSprite == null)
            {
                return;
            }

            GameObject pivotObject = new GameObject("RacketPivot");
            pivotObject.transform.SetParent(transform, false);
            racketPivot = pivotObject.transform;

            GameObject spriteObject = new GameObject("RacketSprite");
            spriteObject.transform.SetParent(racketPivot, false);
            racketRenderer = spriteObject.AddComponent<SpriteRenderer>();
            racketRenderer.sprite = racketSprite;
            racketRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
            racketRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 3;
            racketPivot.localPosition = new Vector3(
                racketPivotLocalPosition.x,
                racketPivotLocalPosition.y,
                -0.01f);
            racketPivot.localRotation = Quaternion.Euler(0f, 0f, racketIdleAngle);
            racketRenderer.transform.localPosition = new Vector3(
                racketSpriteLocalPosition.x,
                racketSpriteLocalPosition.y,
                0f);
            racketRenderer.transform.localScale = Vector3.one * racketScale;
            racketRenderer.enabled = false;
        }

        private void UpdateRacket(PlayerActionFrame action)
        {
            EnsureRacketVisual();
            if (racketPivot == null || racketRenderer == null)
            {
                return;
            }

            CaptureAuthoredRacketIdleAngle();

            if (action.SwingId > 0 && action.SwingId != racketSwingId && IsStartup(action.SwingState))
            {
                racketSwingId = action.SwingId;
                racketSwingStartTime = Time.time;
                racketVisualSwingKind = GetRacketVisualSwingKind(action);
            }

            bool visible = ShouldShowRacket(action);
            racketRenderer.enabled = visible;
            if (!visible)
            {
                racketPivot.localPosition = authoredRacketPivotLocalPosition;
                racketPivot.localRotation = Quaternion.Euler(0f, 0f, racketIdleAngle);
                return;
            }

            float angle = GetRacketAngle(action);
            racketPivot.localPosition = authoredRacketPivotLocalPosition;
            racketPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private float GetRacketAngle(PlayerActionFrame action)
        {
            float duration = Mathf.Max(0.05f, racketSwingDuration);
            bool completingNormalSpin =
                racketVisualSwingKind == SwingKind.Normal &&
                racketSwingId > 0 &&
                Time.time - racketSwingStartTime < duration;

            if (!IsSwinging(action.SwingState) && !completingNormalSpin)
            {
                return racketIdleAngle;
            }

            float t = Mathf.Clamp01((Time.time - racketSwingStartTime) / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            SwingKind visualKind = action.SwingKind != SwingKind.None
                ? GetRacketVisualSwingKind(action)
                : racketVisualSwingKind;
            if (visualKind == SwingKind.Smash)
            {
                return Mathf.Lerp(smashSwingStartAngle, smashSwingEndAngle, t);
            }

            return racketIdleAngle + normalSwingSpinDegrees * t;
        }

        private bool ShouldShowRacket(PlayerActionFrame action)
        {
            float duration = Mathf.Max(0.05f, racketSwingDuration);
            bool completingNormalSpin =
                racketVisualSwingKind == SwingKind.Normal &&
                racketSwingId > 0 &&
                Time.time - racketSwingStartTime < duration;

            return IsSwinging(action.SwingState) || completingNormalSpin;
        }

        private SwingKind GetRacketVisualSwingKind(PlayerActionFrame action)
        {
            if (IsOpponentJumpReceive(action))
            {
                return SwingKind.Normal;
            }

            return action.SwingKind;
        }

        private bool IsOpponentJumpReceive(PlayerActionFrame action)
        {
            if (opponent == null || action.SwingKind != SwingKind.Smash)
            {
                return false;
            }

            AISwingPlan plan = opponent.CurrentPlan;
            return plan != null &&
                   plan.JumpRequired &&
                   plan.Intent != ShotIntent.Smash &&
                   plan.Intent != ShotIntent.Serve;
        }

        private void CaptureAuthoredRacketIdleAngle()
        {
            if (capturedRacketIdleAngle || racketPivot == null)
            {
                return;
            }

            racketIdleAngle = NormalizeAngle(racketPivot.localEulerAngles.z);
            authoredRacketPivotLocalPosition = racketPivot.localPosition;
            capturedRacketIdleAngle = true;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static bool IsSwinging(SwingState state)
        {
            return state == SwingState.NormalStartup ||
                   state == SwingState.NormalActive ||
                   state == SwingState.NormalRecovery ||
                   state == SwingState.SmashStartup ||
                   state == SwingState.SmashActive ||
                   state == SwingState.SmashRecovery;
        }
    }
}
