using UnityEngine;
using UnityEngine.InputSystem;

namespace BatteryGame
{
    /// <summary>카메라 정면 레이캐스트로 대상을 잡고, 손에 물건 하나를 든다.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform hand;
        [SerializeField] float reach = 3.2f;
        [SerializeField] float dropForwardForce = 1.6f;

        Interactable focused;

        public Carryable Held { get; private set; }
        public Interactable Focused { get { return focused; } }
        public Camera ViewCamera { get { return viewCamera; } }

        void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (hand == null && viewCamera != null) hand = viewCamera.transform;
        }

        void Update()
        {
            UpdateFocus();
            HandleInput();
        }

        void UpdateFocus()
        {
            Interactable hit = null;
            if (viewCamera != null)
            {
                RaycastHit info;
                Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
                if (Physics.Raycast(ray, out info, reach, ~0, QueryTriggerInteraction.Ignore))
                    hit = info.collider.GetComponentInParent<Interactable>();
            }

            if (hit == focused) return;
            if (focused != null) focused.SetHighlighted(false);
            focused = hit;
            if (focused != null) focused.SetHighlighted(true);
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

            Vector3 pos = viewCamera.transform.position + viewCamera.transform.forward * 0.6f;
            Vector3 vel = viewCamera.transform.forward * dropForwardForce;
            item.OnDropped(pos, vel);
        }
    }
}
