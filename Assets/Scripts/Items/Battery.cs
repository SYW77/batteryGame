using UnityEngine;

namespace BatteryGame
{
    /// <summary>건전지 한 개. 규격과 잔량을 가지며, 잔량은 집어봐야(inspect) 알 수 있다.</summary>
    public class Battery : Carryable
    {
        [Header("사양")]
        [SerializeField] BatterySize size = BatterySize.AA;
        [Range(0f, 1f)]
        [SerializeField] float charge = 1f;

        [Header("검사 표시")]
        [Tooltip("한 번이라도 집어본 건전지에 켜지는 띠. 다시 집지 않아도 방전 여부를 알 수 있게 해준다.")]
        [SerializeField] Renderer statusBand;
        [SerializeField] Material aliveBandMaterial;
        [SerializeField] Material deadBandMaterial;

        bool inspected;

        /// <summary>
        /// 통 없이 작업대 위에 쌓이므로, 원통이 굴러 떨어지지 않게 마찰을 높인다.
        /// 에셋으로 굽지 않고 런타임에 하나만 만들어 모든 건전지가 공유한다.
        /// </summary>
        static PhysicsMaterial pileFriction;

        static PhysicsMaterial PileFriction()
        {
            if (pileFriction == null)
            {
                pileFriction = new PhysicsMaterial("BatteryPile");
                pileFriction.dynamicFriction = 0.9f;
                pileFriction.staticFriction = 1f;
                pileFriction.bounciness = 0f;
                pileFriction.frictionCombine = PhysicsMaterialCombine.Maximum;
                pileFriction.bounceCombine = PhysicsMaterialCombine.Minimum;
                pileFriction.hideFlags = HideFlags.HideAndDontSave;
            }
            return pileFriction;
        }

        public BatterySize Size { get { return size; } }
        public float Charge { get { return charge; } }
        public bool IsDead { get { return charge <= 0.0001f; } }
        public bool Inspected { get { return inspected; } }

        /// <summary>현재 꽂혀 있는 기기. 없으면 null.</summary>
        public Device InstalledIn { get; set; }

        /// <summary>
        /// 수거함에 버려져 파괴가 예약된 상태.
        /// Destroy는 프레임 끝에 처리되어 그 사이 FindObjectsByType에 잡히므로,
        /// 같은 프레임에 저장이 일어나면 버린 건전지가 세이브에 되살아난다. 그것을 막는 표식.
        /// </summary>
        public bool IsDiscarded { get; private set; }

        public void MarkDiscarded()
        {
            IsDiscarded = true;
        }

        public override Vector3 HoldOffset { get { return new Vector3(0.16f, -0.13f, 0.42f); } }
        public override Quaternion HoldRotation { get { return Quaternion.Euler(70f, 0f, 12f); } }

        public override string HoverTitle
        {
            get { return inspected ? BatterySizes.Label(size) + " 건전지" : "건전지"; }
        }

        public override string HoverDetail
        {
            get
            {
                if (!inspected) return "집어서 규격과 잔량을 확인하세요";
                return IsDead ? "방전됨 · 쓰레기통으로" : "잔량 " + Mathf.RoundToInt(charge * 100f) + "%";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            var col = GetComponent<Collider>();
            if (col != null) col.sharedMaterial = PileFriction();
            ApplyBandVisual();
        }

        public void Configure(BatterySize newSize, float newCharge)
        {
            size = newSize;
            charge = Mathf.Clamp01(newCharge);
            inspected = false;
            ApplyBandVisual();
        }

        public override void Interact(PlayerInteractor player)
        {
            if (InstalledIn != null) return; // 기기에 꽂힌 건전지는 기기 쪽에서 처리한다

            if (player.Held != null)
            {
                HUD.Toast("이미 " + player.Held.HoverTitle + "을(를) 들고 있습니다. (G: 내려놓기)", ToastKind.Warning);
                return;
            }

            player.PickUp(this);
            MarkInspected();
            HUD.Toast(BatterySizes.Label(size) + " · " +
                      (IsDead ? "방전 (잔량 0%)" : "잔량 " + Mathf.RoundToInt(charge * 100f) + "%"),
                      IsDead ? ToastKind.Warning : ToastKind.Info);
        }

        public void MarkInspected()
        {
            if (inspected) return;
            inspected = true;
            ApplyBandVisual();
            RefreshHighlightCache();
        }

        void ApplyBandVisual()
        {
            if (statusBand == null) return;
            statusBand.enabled = inspected;
            if (!inspected) return;
            var m = IsDead ? deadBandMaterial : aliveBandMaterial;
            if (m != null) statusBand.sharedMaterial = m;
        }

        /// <summary>프리팹 팩토리에서 시각 요소를 물려줄 때 사용.</summary>
        public void BindVisuals(Renderer band, Material alive, Material dead)
        {
            statusBand = band;
            aliveBandMaterial = alive;
            deadBandMaterial = dead;
        }
    }
}
