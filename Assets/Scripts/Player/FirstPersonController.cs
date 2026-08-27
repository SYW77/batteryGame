using UnityEngine;
using UnityEngine.InputSystem;

namespace BatteryGame
{
    /// <summary>WASD 이동 + 마우스 시점. Input System 패키지 직접 폴링(액션 에셋 불필요).</summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;

        [Header("이동")]
        [SerializeField] float walkSpeed = 3.2f;
        [SerializeField] float sprintSpeed = 5.4f;
        [SerializeField] float acceleration = 14f;
        [SerializeField] float gravity = -18f;
        [SerializeField] float jumpHeight = 0.9f;

        [Header("시점")]
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float minPitch = -85f;
        [SerializeField] float maxPitch = 85f;

        CharacterController cc;
        Vector3 planarVelocity;
        float verticalVelocity;
        float pitch;

        public bool LookEnabled { get; set; }

        /// <summary>메뉴가 떠 있는 동안에는 꺼진다. GameFlow가 제어한다.</summary>
        public bool ControlEnabled { get; private set; }

        /// <summary>세이브에 기록할 카메라 상하 각도.</summary>
        public float Pitch { get { return pitch; } }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
            LookEnabled = true;
            ControlEnabled = false;
        }

        void OnDisable()
        {
            LockCursor(false);
        }

        public void SetControlEnabled(bool on)
        {
            ControlEnabled = on;
            LockCursor(on);
            if (!on)
            {
                planarVelocity = Vector3.zero;
                verticalVelocity = 0f;
            }
        }

        /// <summary>이어하기로 위치를 복원할 때 사용.</summary>
        public void Teleport(Vector3 position, float yaw, float cameraPitchDegrees)
        {
            bool wasEnabled = cc.enabled;
            cc.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitch = Mathf.Clamp(cameraPitchDegrees, minPitch, maxPitch);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cc.enabled = wasEnabled;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        void Update()
        {
            if (!ControlEnabled) return;
            HandleCursorToggle();
            if (LookEnabled && Cursor.lockState == CursorLockMode.Locked) HandleLook();
            HandleMove();
        }

        void HandleCursorToggle()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) LockCursor(false);

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                LockCursor(true);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void HandleLook()
        {
            var mouse = Mouse.current;
            if (mouse == null || cameraPivot == null) return;

            Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(Vector3.up, delta.x, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleMove()
        {
            var kb = Keyboard.current;
            Vector2 input = Vector2.zero;
            bool sprint = false;
            bool jump = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                sprint = kb.leftShiftKey.isPressed;
                jump = kb.spaceKey.wasPressedThisFrame;
            }

            if (input.sqrMagnitude > 1f) input.Normalize();

            Vector3 wish = (transform.right * input.x + transform.forward * input.y) *
                           (sprint ? sprintSpeed : walkSpeed);
            planarVelocity = Vector3.MoveTowards(planarVelocity, wish, acceleration * Time.deltaTime);

            if (cc.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (jump) verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = planarVelocity + Vector3.up * verticalVelocity;
            cc.Move(motion * Time.deltaTime);
        }
    }
}
