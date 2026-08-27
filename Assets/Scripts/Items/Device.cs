using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BatteryGame
{
    /// <summary>건전지를 요구하는 전자기기. 규격/개수가 모두 맞고 전부 잔량이 있어야 작동한다.</summary>
    public class Device : Carryable
    {
        [Header("사양")]
        [SerializeField] string deviceName = "전자기기";
        [SerializeField] BatterySize requiredSize = BatterySize.AA;
        [SerializeField] int requiredCount = 2;

        [Header("참조")]
        [SerializeField] List<Transform> slotAnchors = new List<Transform>();
        [SerializeField] Renderer indicator;
        [SerializeField] Material indicatorOffMaterial;
        [SerializeField] Material indicatorOnMaterial;
        [SerializeField] Light indicatorLight;
        [SerializeField] Text worldLabel;

        readonly List<Battery> installed = new List<Battery>();

        public string DeviceName { get { return deviceName; } }
        public BatterySize RequiredSize { get { return requiredSize; } }
        public int RequiredCount { get { return requiredCount; } }
        public int FilledCount { get { return installed.Count; } }
        public bool IsPowered { get; private set; }
        public bool IsSubmitted { get; private set; }
        public IReadOnlyList<Battery> Installed { get { return installed; } }

        public string Requirement
        {
            get { return BatterySizes.Label(requiredSize) + " x" + requiredCount; }
        }

        public override Vector3 HoldOffset { get { return new Vector3(0.24f, -0.22f, 0.55f); } }
        public override Quaternion HoldRotation { get { return Quaternion.Euler(10f, -25f, 0f); } }

        public override string HoverTitle { get { return deviceName; } }

        public override string HoverDetail
        {
            get
            {
                if (IsSubmitted) return "제출 완료";
                if (IsPowered) return "작동 중! 제출대로 가져가세요";
                return "필요: " + Requirement + "  (" + installed.Count + "/" + requiredCount + " 장착)";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Evaluate();
        }

        public void Configure(string newName, BatterySize size, int count)
        {
            deviceName = newName;
            requiredSize = size;
            requiredCount = Mathf.Max(1, count);
        }

        public void BindVisuals(List<Transform> anchors, Renderer ind, Material off, Material on, Light lamp, Text label)
        {
            slotAnchors = anchors;
            indicator = ind;
            indicatorOffMaterial = off;
            indicatorOnMaterial = on;
            indicatorLight = lamp;
            worldLabel = label;
        }

        public override void Interact(PlayerInteractor player)
        {
            if (player.Held == null)
            {
                // 제출한 기기를 다시 집으면 중복 제출이 되어 집계가 망가진다.
                if (IsSubmitted)
                {
                    HUD.Toast("이미 제출한 기기입니다.", ToastKind.Warning);
                    return;
                }
                player.PickUp(this);
                return;
            }

            var battery = player.Held as Battery;
            if (battery == null)
            {
                HUD.Toast("한 번에 하나만 들 수 있습니다. (G: 내려놓기)", ToastKind.Warning);
                return;
            }

            string message;
            if (TryInsert(battery, out message))
            {
                player.ClearHeld();
                HUD.Toast(message, IsPowered ? ToastKind.Success : ToastKind.Info);
            }
            else
            {
                HUD.Toast(message, ToastKind.Error);
            }
        }

        public override void AltInteract(PlayerInteractor player)
        {
            if (IsSubmitted) return;
            if (installed.Count == 0)
            {
                HUD.Toast("빼낼 건전지가 없습니다.", ToastKind.Warning);
                return;
            }
            if (player.Held != null)
            {
                HUD.Toast("손이 비어 있어야 건전지를 뺄 수 있습니다. (G: 내려놓기)", ToastKind.Warning);
                return;
            }

            Battery b = installed[installed.Count - 1];
            installed.RemoveAt(installed.Count - 1);
            b.InstalledIn = null;
            Evaluate();
            RefreshHighlightCache();

            player.PickUp(b);
            b.MarkInspected();
            HUD.Toast(BatterySizes.Label(b.Size) + " 건전지를 뺐습니다.", ToastKind.Info);
        }

        public bool TryInsert(Battery battery, out string message)
        {
            if (IsSubmitted)
            {
                message = "이미 제출된 기기입니다.";
                return false;
            }
            if (battery.Size != requiredSize)
            {
                message = deviceName + "은(는) " + BatterySizes.Label(requiredSize) + " 규격만 들어갑니다. (" +
                          BatterySizes.Label(battery.Size) + " 아님)";
                return false;
            }
            if (installed.Count >= requiredCount)
            {
                message = deviceName + "의 건전지 칸이 가득 찼습니다. (" + Requirement + ")";
                return false;
            }

            Transform anchor = SlotAnchor(installed.Count);
            battery.AttachTo(anchor);
            battery.InstalledIn = this;
            installed.Add(battery);
            Evaluate();
            RefreshHighlightCache();

            if (battery.IsDead)
                message = "방전된 건전지입니다. 이걸로는 작동하지 않습니다.";
            else if (IsPowered)
                message = deviceName + " 작동 시작! 제출대로 가져가세요.";
            else
                message = deviceName + " " + installed.Count + "/" + requiredCount + " 장착";

            return true;
        }

        Transform SlotAnchor(int index)
        {
            if (slotAnchors != null && index < slotAnchors.Count && slotAnchors[index] != null)
                return slotAnchors[index];
            return transform;
        }

        void Evaluate()
        {
            bool powered = installed.Count >= requiredCount;
            for (int i = 0; i < installed.Count && powered; i++)
                if (installed[i] == null || installed[i].IsDead) powered = false;

            if (powered != IsPowered)
            {
                IsPowered = powered;
                ApplyPowerVisual();
            }
            UpdateWorldLabel();
        }

        void ApplyPowerVisual()
        {
            if (indicator != null)
            {
                var m = IsPowered ? indicatorOnMaterial : indicatorOffMaterial;
                if (m != null) indicator.sharedMaterial = m;
            }
            if (indicatorLight != null) indicatorLight.enabled = IsPowered;
        }

        void UpdateWorldLabel()
        {
            if (worldLabel == null) return;
            if (IsSubmitted)
            {
                worldLabel.text = deviceName + "\n제출 완료";
                worldLabel.color = new Color(0.6f, 0.9f, 0.6f);
            }
            else if (IsPowered)
            {
                worldLabel.text = deviceName + "\n작동 중";
                worldLabel.color = new Color(0.55f, 1f, 0.6f);
            }
            else
            {
                worldLabel.text = deviceName + "\n" + Requirement + "  (" + installed.Count + "/" + requiredCount + ")";
                worldLabel.color = Color.white;
            }
        }

        /// <summary>제출대에서 호출. 더 이상 건드릴 수 없는 상태로 잠근다.</summary>
        public void MarkSubmitted()
        {
            IsSubmitted = true;
            // 제출대에 기기가 쌓이면 표시등 광원이 겹쳐서 주변이 온통 초록으로 물든다.
            // 발광 머티리얼은 그대로 두고 실시간 광원만 끈다.
            if (indicatorLight != null) indicatorLight.enabled = false;
            UpdateWorldLabel();
        }
    }
}
