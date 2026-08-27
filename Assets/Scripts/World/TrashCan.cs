using UnityEngine;

namespace BatteryGame
{
    /// <summary>방전된 건전지만 받는 폐건전지 수거함.</summary>
    public class TrashCan : Interactable
    {
        [Header("페널티")]
        [Tooltip("잔량이 남은 건전지를 버리려 했을 때 쏟아지는 폐건전지 개수.")]
        [SerializeField] int penaltyCount = 5;
        [SerializeField] float penaltyScatter = 0.45f;
        [Tooltip("쏟아지는 높이. 수거함 위에서 떨어져 주변에 흩어진다.")]
        [SerializeField] float penaltyHeight = 1.35f;

        Vector3 PenaltySpawnPoint()
        {
            return transform.position + Vector3.up * penaltyHeight;
        }

        public override string HoverTitle { get { return "폐건전지 수거함"; } }
        public override string HoverDetail { get { return "방전된 건전지(잔량 0%)만 버릴 수 있습니다"; } }

        public override void Interact(PlayerInteractor player)
        {
            if (player.Held == null)
            {
                HUD.Toast("방전된 건전지를 들고 오세요.", ToastKind.Warning);
                return;
            }

            var battery = player.Held as Battery;
            if (battery == null)
            {
                HUD.Toast("건전지만 버릴 수 있습니다.", ToastKind.Error);
                return;
            }

            if (!battery.IsDead)
            {
                // 건전지는 손에 그대로 둔다. 여기서 없애버리면 그 규격을 쓰는 기기를
                // 영영 켤 수 없게 되어 클리어가 불가능해진다.
                GameManager.Instance.SpawnPenaltyBatteries(penaltyCount, PenaltySpawnPoint(), penaltyScatter);
                HUD.Toast("잔량 " + Mathf.RoundToInt(battery.Charge * 100f) + "%가 남아 있습니다! " +
                          "수거함이 폐건전지 " + penaltyCount + "개를 쏟아냈습니다.", ToastKind.Error);
                return;
            }

            player.ClearHeld();
            GameManager.Instance.NotifyBatteryDiscarded(battery);
            Destroy(battery.gameObject);
            HUD.Toast("방전 건전지를 버렸습니다.", ToastKind.Success);
        }
    }
}
