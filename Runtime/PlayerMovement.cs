using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EGS.RoguelikeMovement4Dir
{

    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 15f;
        public float jumpHeight = 1.5f;
        public float gravity = -20f;

        [Header("References")]
        public Camera mainCamera;                 // assign MainCamera in inspector
        public LayerMask groundMask = ~0;         // set to "Ground" layer in inspector
        public PlayerAnimationController animController; // decoupled animation controller

        [Header("Input")]
        public PlayerInput playerInput; // optional: drag PlayerInput or let Awake find it
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction attackAction; // optional, if you have an attack binding

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            if (mainCamera == null)
                mainCamera = Camera.main;

            // Actions must exist in the InputAction asset: "Move", "Look", "Jump" (optional "Attack")
            if (playerInput != null && playerInput.actions != null)
            {
                moveAction = playerInput.actions["Move"];
                lookAction = playerInput.actions["Look"];
                jumpAction = playerInput.actions["Jump"];
                attackAction = playerInput.actions["Attack"];
            }
        }

        void Update()
        {
            Vector2 input = Vector2.zero;
            if (moveAction != null) input = moveAction.ReadValue<Vector2>();

            // Build world-space movement vector (WASD = world forward on +Z)
            Vector3 moveWorld = new Vector3(input.x, 0f, input.y);

            // Update animation system with world movement (decoupled)
            if (animController != null)
                animController.UpdateMovement(moveWorld, transform);

            // Movement (CharacterController)
            if (moveWorld.sqrMagnitude > 0.0001f)
            {
                Vector3 move = moveWorld.normalized * moveSpeed;
                controller.Move(move * Time.deltaTime);
            }

            // Rotation: player faces mouse point on ground (no camera rotation)
            HandleRotationToMouse();

            // Gravity and jump
            ApplyGravityAndJump();

            // Update anim grounded state
            if (animController != null)
                animController.SetGrounded(isGrounded);

            // Attack trigger (example): animation controller handles upper-body layer
            if (attackAction != null && attackAction.triggered)
            {
                animController?.TriggerAttack();
                // your attack system should be triggered by animation event or here
            }
        }

        void HandleRotationToMouse()
        {
            if (mainCamera == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
            {
                Vector3 dir = hit.point - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion target = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
                }
            }
        }

        void ApplyGravityAndJump()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f; // small negative to keep grounded

            if (jumpAction != null && jumpAction.triggered && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animController?.TriggerJump();
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}