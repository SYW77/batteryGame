using UnityEngine;
using UnityEngine.InputSystem;

namespace BatteryGame
{
    /// <summary>카메라 정면 레이캐스트로 대상을 잡고, 손에 물건 하나를 든다.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform hand;
        [SerializeField] float reach = 4.2f;

        [Header("조준 보조")]
        [Tooltip("정밀 조준이 빗나갔을 때 쓰는 보조 판정 반경. 바닥에 떨어진 작은 건전지를 집기 쉽게 해준다.")]
        [SerializeField] float aimAssistRadius = 0.16f;
        [Tooltip("보조 판정을 카메라보다 이만큼 뒤에서 시작한다. 코앞에 붙은 물체는 스윕 시작 지점과 겹쳐서 안 잡히기 때문.")]
        [SerializeField] float aimAssistBackoff = 0.45f;

        [Header("내려놓기")]
        [SerializeField] float dropForwardForce = 0.7f;
        [Tooltip("내려놓을 때 눈높이에서 이만큼 아래에 놓는다. 높은 데서 떨어뜨리면 멀리 굴러간다.")]
        [SerializeField] float dropHeightBelowEye = 0.55f;

        readonly RaycastHit[] hitBuffer = new RaycastHit[24];

        Interactable focused;
        bool active;

        /// <summary>손에 든 물건은 Ignore Raycast 레이어로 옮겨두므로 기본 마스크에서 자동으로 빠진다.</summary>
        static int InteractMask { get { return Physics.DefaultRaycastLayers; } }

        public Carryable Held { get; private set; }
        public Interactable Focused { get { return focused; } }
        public Camera ViewCamera { get { return viewCamera; } }

        void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (hand == null && viewCamera != null) hand = viewCamera.transform;
        }

        /// <summary>메뉴가 떠 있는 동안에는 조준도 조작도 멈춘다. GameFlow가 제어한다.</summary>
        public void SetActiveState(bool on)
        {
            active = on;
            if (on) return;
            if (focused != null) focused.SetHighlighted(false);
            focused = null;
        }

        void Update()
        {
            if (!active) return;
            UpdateFocus();
            HandleInput();
        }

        void UpdateFocus()
        {
            Interactable hit = FindFocus();

            if (hit == focused) return;
            if (focused != null) focused.SetHighlighted(false);
            focused = hit;
            if (focused != null) focused.SetHighlighted(true);
        }

        Interactable FindFocus()
        {
            if (viewCamera == null) return null;
            Transform eye = viewCamera.transform;

            // 1) 정밀 조준. 겨냥한 그대로 잡히는 게 우선이다.
            RaycastHit info;
            if (Physics.Raycast(eye.position, eye.forward, out info, reach, InteractMask, QueryTriggerInteraction.Ignore))
            {
                var direct = info.collider.GetComponentInParent<Interactable>();
                if (direct != null) return direct;
            }

            // 2) 보조 판정. 굵은 구를 쏴서 근처에 있는 것까지 잡아준다.
            //    시작점을 뒤로 뺀 이유: 스윕 시작 지점에 겹쳐 있는 콜라이더는 감지되지 않아서,
            //    수거함처럼 바짝 붙어 선 물체가 오히려 안 잡히는 문제가 생긴다.
            Vector3 origin = eye.position - eye.forward * aimAssistBackoff;
            int count = Physics.SphereCastNonAlloc(origin, aimAssistRadius, eye.forward, hitBuffer,
                                                   reach + aimAssistBackoff, InteractMask,
                                                   QueryTriggerInteraction.Ignore);

            Interactable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = hitBuffer[i].collider.GetComponentInParent<Interactable>();
                if (candidate == null || candidate == (Interactable)Held) continue;
                if (hitBuffer[i].distance >= bestDistance) continue;
                bestDistance = hitBuffer[i].distance;
                best = candidate;
            }
            return best;
        }

        void HandleInput()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            var mouse = Mouse.current;
            var kb = Keyboard.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && focused != null)
                focused.Interact(this);

            if (mouse != null && mouse.rightButton.wasPressedThisFrame && focused != null)
                focused.AltInteract(this);

            if (kb != null && kb.gKey.wasPressedThisFrame)
                DropHeld();
        }

        public void PickUp(Carryable item)
        {
            if (item == null || Held != null) return;
            if (focused == item)
            {
                focused.SetHighlighted(false);
                focused = null;
            }
            Held = item;
            item.OnPickedUp(hand);
        }

        /// <summary>손에 든 것을 기기/쓰레기통 등에 넘겼을 때. 물리 복구 없이 참조만 비운다.</summary>
        public void ClearHeld()
        {
            Held = null;
        }

        public void DropHeld()
        {
            if (Held == null) return;
            Carryable item = Held;
            Held = null;

            // 눈높이에서 그대로 놓으면 1.6m를 굴러떨어져 멀리 튀어 나간다. 낮게 내려놓는다.
            Transform eye = viewCamera.transform;
            Vector3 flat = Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized;
            if (flat.sqrMagnitude < 0.01f) flat = eye.forward;

            Vector3 pos = eye.position + flat * 0.5f - Vector3.up * dropHeightBelowEye;
            Vector3 vel = flat * dropForwardForce;
            item.OnDropped(pos, vel);
        }
    }
}
