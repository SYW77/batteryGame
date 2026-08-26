using UnityEngine;

namespace BatteryGame
{
    /// <summary>손에 들 수 있는 물건(건전지, 전자기기)의 공통 동작.</summary>
    public abstract class Carryable : Interactable
    {
        public const int IgnoreRaycastLayer = 2;

        Rigidbody body;
        Collider[] colliders;
        int[] savedLayers;
        Transform[] savedLayerTargets;

        public bool IsHeld { get; private set; }

        /// <summary>손에 들었을 때 카메라 기준 오프셋/회전. 물건마다 다르게 잡는다.</summary>
        public virtual Vector3 HoldOffset { get { return new Vector3(0.22f, -0.16f, 0.42f); } }
        public virtual Quaternion HoldRotation { get { return Quaternion.Euler(15f, -20f, 8f); } }

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>(true);
        }

        public Rigidbody Body { get { return body; } }

        public virtual void OnPickedUp(Transform hand)
        {
            IsHeld = true;
            SetPhysicsEnabled(false);
            PushLayers(IgnoreRaycastLayer);

            transform.SetParent(hand, false);
            transform.localPosition = HoldOffset;
            transform.localRotation = HoldRotation;
        }

        public virtual void OnDropped(Vector3 position, Vector3 velocity)
        {
            IsHeld = false;
            transform.SetParent(null, true);
            transform.position = position;
            PopLayers();
            SetPhysicsEnabled(true);
            if (body != null)
            {
                body.linearVelocity = velocity;
                body.angularVelocity = Random.insideUnitSphere * 2f;
            }
        }

        /// <summary>기기 슬롯에 꽂히는 등, 물리 없이 다른 트랜스폼에 붙을 때.</summary>
        public virtual void AttachTo(Transform anchor)
        {
            IsHeld = false;
            SetPhysicsEnabled(false);
            PushLayers(IgnoreRaycastLayer);
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        protected void SetPhysicsEnabled(bool on)
        {
            if (body != null)
            {
                body.isKinematic = !on;
                body.detectCollisions = on;
                if (on) body.WakeUp();
            }
            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null) colliders[i].enabled = on;
            }
        }

        void PushLayers(int layer)
        {
            if (savedLayers != null) return; // 이미 눌러둔 상태면 원본을 덮어쓰지 않는다
            var all = GetComponentsInChildren<Transform>(true);
            savedLayerTargets = all;
            savedLayers = new int[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                savedLayers[i] = all[i].gameObject.layer;
                all[i].gameObject.layer = layer;
            }
        }

        void PopLayers()
        {
            if (savedLayers == null) return;
            for (int i = 0; i < savedLayerTargets.Length; i++)
                if (savedLayerTargets[i] != null)
                    savedLayerTargets[i].gameObject.layer = savedLayers[i];
            savedLayers = null;
            savedLayerTargets = null;
        }
    }
}
