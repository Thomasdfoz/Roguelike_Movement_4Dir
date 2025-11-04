using UnityEngine;

/// <summary>
/// Single-purpose animation controller: receives high-level calls from gameplay systems
/// and updates Animator parameters / layers. Does NOT read input or move the character.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animator Parameters")]
    public string paramMoveX = "MoveX";
    public string paramMoveY = "MoveY";
    public string paramSpeed = "Speed";
    public string paramIsGrounded = "IsGrounded";
    public string paramJump = "Jump";
    public string paramAttack = "Attack";

    [Header("Layer Settings")]
    [Tooltip("Index of the layer used for upper-body (attacks/aim).")]
    public int upperBodyLayerIndex = 1;
    [Range(0f, 1f)] public float upperBodyDefaultWeight = 1f;
    public float upperBodyBlendSpeed = 10f; // smoothing when enabling/disabling upper body overrides

    private Animator animator;
    private float targetUpperWeight = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        // Ensure initial weight
        if (animator.layerCount > upperBodyLayerIndex)
            animator.SetLayerWeight(upperBodyLayerIndex, upperBodyDefaultWeight);
    }

    void Update()
    {
        // Smoothly apply target weight to avoid pops when enabling attack layer
        if (animator.layerCount > upperBodyLayerIndex)
        {
            float current = animator.GetLayerWeight(upperBodyLayerIndex);
            float next = Mathf.Lerp(current, targetUpperWeight, Time.deltaTime * upperBodyBlendSpeed);
            animator.SetLayerWeight(upperBodyLayerIndex, next);
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
        if (animator == null || characterTransform == null) return;

        // Convert world movement into local space relative to player's facing direction
        Vector3 local = characterTransform.InverseTransformDirection(worldMove);
        float moveX = Mathf.Clamp(local.x, -1f, 1f);
        float moveY = Mathf.Clamp(local.z, -1f, 1f);
        float speed = Mathf.Clamp01(worldMove.magnitude);

        animator.SetFloat(paramMoveX, moveX, dampTime, Time.deltaTime);
        animator.SetFloat(paramMoveY, moveY, dampTime, Time.deltaTime);
        animator.SetFloat(paramSpeed, speed, dampTime, Time.deltaTime);
    }

    public void SetGrounded(bool grounded)
    {
        if (animator == null) return;
        animator.SetBool(paramIsGrounded, grounded);
    }

    public void TriggerJump()
    {
        if (animator == null) return;
        animator.SetTrigger(paramJump);
    }

    /// <summary>
    /// Trigger a regular upper-body attack animation.  
    /// It will raise the upper body layer weight during attack and return to default after.
    /// </summary>
    public void TriggerAttack()
    {
        if (animator == null) return;

        // raise upper-body layer weight immediately (target) so the upper-body animation overrides lower body
        targetUpperWeight = 1f;
        animator.SetTrigger(paramAttack);
        // keep weight staying while the attack plays; you can also animate weight via animation events
        // here we schedule fallback to default weight after a small delay (safety)
        CancelInvoke(nameof(ResetUpperBodyToDefault));
        Invoke(nameof(ResetUpperBodyToDefault), 0.6f); // tweak duration to match attack anim length
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
