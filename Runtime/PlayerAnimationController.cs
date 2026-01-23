using UnityEngine;

namespace EGS.RoguelikeMovement4Dir
{
    /// <summary>
    /// Single-purpose animation controller: receives high-level calls from gameplay systems
    /// and updates Animator parameters / layers. Does NOT read input or move the character.
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        [Header("Animator Parameters")]
        [SerializeField] private string paramMoveX = "MoveX";
        [SerializeField] private string paramMoveY = "MoveY";
        [SerializeField] private string paramSpeed = "Speed";
        [SerializeField] private string paramIsGrounded = "IsGrounded";
        [SerializeField] private string paramJump = "Jump";
        [SerializeField] private string paramAttack = "Attack";

        [Header("Layer Settings")]
        [Tooltip("Index of the layer used for upper-body (attacks/aim).")]
        [SerializeField] private int upperBodyLayerIndex = 1;
        [Range(0f, 1f)] public float upperBodyDefaultWeight = 1f;
        [SerializeField] private float upperBodyBlendSpeed = 10f; 

        private const string PLACEHOLDER_CLIP_NAME = "BowShot";

        private AnimatorOverrideController _overrideController;
        private AnimationClip _defaultClip;
        private float targetUpperWeight = 0f;

        private void Awake()
        {
            if (_animator.runtimeAnimatorController is AnimatorOverrideController existingOverride)
            {
                _overrideController = existingOverride;
            }
            else
            {
                _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
                _animator.runtimeAnimatorController = _overrideController;
            }

            _defaultClip = _overrideController[PLACEHOLDER_CLIP_NAME];
        }



        void Update()
        {
            // Smoothly apply target weight to avoid pops when enabling attack layer
            if (_animator.layerCount > upperBodyLayerIndex)
            {
                float current = _animator.GetLayerWeight(upperBodyLayerIndex);
                float next = Mathf.Lerp(current, targetUpperWeight, Time.deltaTime * upperBodyBlendSpeed);
                _animator.SetLayerWeight(upperBodyLayerIndex, next);
            }
        }

        // -----------------------------
        // PUBLIC API (called by gameplay)
        // -----------------------------

        /// <summary>
        /// Update locomotion values. supply world-space movement vector (x,z).
        /// Animation controller will convert to local space for blending.
        /// </summary>
        public void UpdateMovement(Vector3 worldMove, Transform characterTransform, float dampTime = 0.08f)
        {
            if (_animator == null || characterTransform == null) return;

            // Convert world movement into local space relative to player's facing direction
            Vector3 local = characterTransform.InverseTransformDirection(worldMove);
            float moveX = Mathf.Clamp(local.x, -1f, 1f);
            float moveY = Mathf.Clamp(local.z, -1f, 1f);
            float speed = Mathf.Clamp01(worldMove.magnitude);

            _animator.SetFloat(paramMoveX, moveX, dampTime, Time.deltaTime);
            _animator.SetFloat(paramMoveY, moveY, dampTime, Time.deltaTime);
            _animator.SetFloat(paramSpeed, speed, dampTime, Time.deltaTime);
        }

        public void SetGrounded(bool grounded)
        {
            if (_animator == null) return;
            _animator.SetBool(paramIsGrounded, grounded);
        }

        public void TriggerJump()
        {
            if (_animator == null) return;
            _animator.SetTrigger(paramJump);
        }

        /// <summary>
        /// Toca o ataque. Se lOverrideClip for nulo, usa a animação padrão (BowShot).
        /// </summary>
        public void TriggerAttack(AnimationClip lOverrideClip = null)
        {
            // 1. Determina qual clipe usar: O novo (se existir) OU o Padrão (cacheado no Awake)
            AnimationClip lClipToPlay = (lOverrideClip != null) ? lOverrideClip : _defaultClip;

            // 2. Aplica a troca
            // Se for o custom, ele troca. Se for o default, ele restaura o BowShot.
            _overrideController[PLACEHOLDER_CLIP_NAME] = lClipToPlay;

            // 3. Lógica do Layer Weight
            targetUpperWeight = 1f;

            // Garante que o peso suba instantaneamente para o ataque não começar "fraco"
            // (Opcional: remova se quiser que o Update faça o blend suave de entrada)
            _animator.SetLayerWeight(upperBodyLayerIndex, 1f);

            // 4. Dispara o Trigger
            _animator.SetTrigger(paramAttack);

            // 5. Agenda o reset baseado na duração do clipe QUE VAI TOCAR
            float lDuration = lClipToPlay != null ? lClipToPlay.length : 0.6f;

            CancelInvoke(nameof(ResetUpperBodyToDefault));
            Invoke(nameof(ResetUpperBodyToDefault), lDuration);
        }

        public void EnableUpperBodyOverride(bool enable)
        {
            targetUpperWeight = enable ? 1f : upperBodyDefaultWeight;
        }

        private void ResetUpperBodyToDefault()
        {
            targetUpperWeight = upperBodyDefaultWeight;
        }
    }
}