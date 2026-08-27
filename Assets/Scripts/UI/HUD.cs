using UnityEngine;
using UnityEngine.UI;

namespace BatteryGame
{
    public enum ToastKind { Info, Success, Warning, Error }

    /// <summary>화면 하단/중앙 정보 표시. LevelBuilder가 참조를 연결해 준다.</summary>
    public class HUD : MonoBehaviour
    {
        static HUD instance;

        // 도메인 리로드가 꺼져 있으면 static이 이전 플레이의 값을 그대로 들고 있다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
        }

        [SerializeField] PlayerInteractor player;

        [Header("조준 정보")]
        [SerializeField] Text hoverTitle;
        [SerializeField] Text hoverDetail;
        [SerializeField] Graphic crosshair;

        [Header("손에 든 물건")]
        [SerializeField] GameObject heldPanel;
        [SerializeField] Text heldTitle;
        [SerializeField] Text heldDetail;
        [SerializeField] RectTransform chargeBarFill;
        [SerializeField] Graphic chargeBarGraphic;

        [Header("진행도")]
        [SerializeField] Text progressText;

        [Header("알림")]
        [SerializeField] Text toastText;
        [SerializeField] CanvasGroup toastGroup;
        [SerializeField] float toastDuration = 2.4f;

        float toastTimer;
        Canvas canvas;

        static readonly Color InfoColor = new Color(0.92f, 0.94f, 0.98f);
        static readonly Color SuccessColor = new Color(0.55f, 1f, 0.62f);
        static readonly Color WarningColor = new Color(1f, 0.83f, 0.42f);
        static readonly Color ErrorColor = new Color(1f, 0.48f, 0.45f);

        void Awake()
        {
            instance = this;
            if (player == null) player = FindFirstObjectByType<PlayerInteractor>();
            canvas = GetComponent<Canvas>();
            if (toastGroup != null) toastGroup.alpha = 0f;
            if (heldPanel != null) heldPanel.SetActive(false);
        }

        void Start()
        {
            var gm = GameManager.Instance;
            gm.ProgressChanged += RefreshProgress;
            RefreshProgress();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Update()
        {
            RefreshHover();
            RefreshHeld();
            RefreshToast();
        }

        void RefreshHover()
        {
            Interactable f = player != null ? player.Focused : null;
            if (hoverTitle != null) hoverTitle.text = f != null ? f.HoverTitle : string.Empty;
            if (hoverDetail != null) hoverDetail.text = f != null ? (f.HoverDetail ?? string.Empty) : string.Empty;
            if (crosshair != null)
                crosshair.color = f != null ? new Color(1f, 0.85f, 0.35f, 0.95f) : new Color(1f, 1f, 1f, 0.55f);
        }

        void RefreshHeld()
        {
            Carryable held = player != null ? player.Held : null;
            if (heldPanel != null) heldPanel.SetActive(held != null);
            if (held == null) return;

            var battery = held as Battery;
            if (battery != null)
            {
                if (heldTitle != null) heldTitle.text = BatterySizes.Label(battery.Size) + " 건전지";
                int pct = Mathf.RoundToInt(battery.Charge * 100f);
                if (heldDetail != null)
                {
                    heldDetail.text = battery.IsDead ? "잔량 0% · 방전 → 수거함" : "잔량 " + pct + "%";
                    heldDetail.color = battery.IsDead ? ErrorColor : SuccessColor;
                }
                if (chargeBarFill != null)
                {
                    chargeBarFill.gameObject.SetActive(true);
                    chargeBarFill.localScale = new Vector3(Mathf.Max(0.0001f, battery.Charge), 1f, 1f);
                }
                if (chargeBarGraphic != null)
                    chargeBarGraphic.color = battery.IsDead ? ErrorColor
                                           : Color.Lerp(WarningColor, SuccessColor, battery.Charge);
                return;
            }

            var device = held as Device;
            if (device != null)
            {
                if (heldTitle != null) heldTitle.text = device.DeviceName;
                if (heldDetail != null)
                {
                    heldDetail.text = device.IsPowered
                        ? "작동 중 · 제출대로"
                        : "필요 " + device.Requirement + " · 현재 " + device.FilledCount + "개";
                    heldDetail.color = device.IsPowered ? SuccessColor : InfoColor;
                }
                if (chargeBarFill != null) chargeBarFill.gameObject.SetActive(false);
            }
        }

        void RefreshProgress()
        {
            if (progressText == null) return;
            var gm = GameManager.Instance;
            progressText.text = "남은 건전지  " + gm.RemainingBatteries + " / " + gm.TotalBatteries +
                                "\n제출한 기기  " + gm.SubmittedDevices + " / " + gm.TotalDevices;
        }

        void RefreshToast()
        {
            if (toastGroup == null) return;
            if (toastTimer <= 0f) return;
            toastTimer -= Time.deltaTime;
            toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.5f);
        }

        /// <summary>메뉴가 떠 있는 동안 게임플레이 HUD를 통째로 숨긴다.</summary>
        public void SetVisible(bool on)
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = on;
        }

        public static void Toast(string message, ToastKind kind = ToastKind.Info)
        {
            if (instance == null) return;
            instance.ShowToast(message, kind);
        }

        void ShowToast(string message, ToastKind kind)
        {
            if (toastText == null || toastGroup == null) return;
            toastText.text = message;
            switch (kind)
            {
                case ToastKind.Success: toastText.color = SuccessColor; break;
                case ToastKind.Warning: toastText.color = WarningColor; break;
                case ToastKind.Error: toastText.color = ErrorColor; break;
                default: toastText.color = InfoColor; break;
            }
            toastGroup.alpha = 1f;
            toastTimer = toastDuration;
        }

        public void Bind(PlayerInteractor interactor, Text hTitle, Text hDetail, Graphic cross,
                         GameObject held, Text heldT, Text heldD, RectTransform barFill, Graphic barGraphic,
                         Text progress, Text toast, CanvasGroup toastCg)
        {
            player = interactor;
            hoverTitle = hTitle;
            hoverDetail = hDetail;
            crosshair = cross;
            heldPanel = held;
            heldTitle = heldT;
            heldDetail = heldD;
            chargeBarFill = barFill;
            chargeBarGraphic = barGraphic;
            progressText = progress;
            toastText = toast;
            toastGroup = toastCg;
        }
    }
}
