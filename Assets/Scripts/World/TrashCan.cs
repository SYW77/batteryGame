using UnityEngine;

namespace BatteryGame
{
    /// <summary>방전된 건전지만 받는 폐건전지 수거함.</summary>
    public class TrashCan : Interactable
    {
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
                HUD.Toast("아직 잔량 " + Mathf.RoundToInt(battery.Charge * 100f) + "%가 남아 있습니다. 기기에 넣으세요.",
                          ToastKind.Error);
                return;
            }

            player.ClearHeld();
            GameManager.Instance.NotifyBatteryDiscarded(battery);
            Destroy(battery.gameObject);
            HUD.Toast("방전 건전지를 버렸습니다.", ToastKind.Success);
        }
    }
}
