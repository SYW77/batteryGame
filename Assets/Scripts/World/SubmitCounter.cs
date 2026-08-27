using System.Collections.Generic;
using UnityEngine;

namespace BatteryGame
{
    /// <summary>작동 중인 전자기기를 제출하는 곳.</summary>
    public class SubmitCounter : Interactable
    {
        [SerializeField] List<Transform> placementAnchors = new List<Transform>();
        int nextAnchor;

        public override string HoverTitle { get { return "제출대"; } }
        public override string HoverDetail { get { return "정상 작동하는 전자기기를 올려놓으세요"; } }

        public void BindAnchors(List<Transform> anchors)
        {
            placementAnchors = anchors;
        }

        public override void Interact(PlayerInteractor player)
        {
            if (player.Held == null)
            {
                HUD.Toast("작동하는 기기를 들고 오세요.", ToastKind.Warning);
                return;
            }

            var device = player.Held as Device;
            if (device == null)
            {
                HUD.Toast("여기는 전자기기를 제출하는 곳입니다.", ToastKind.Error);
                return;
            }

            if (device.IsSubmitted)
            {
                HUD.Toast("이미 제출된 기기입니다.", ToastKind.Warning);
                return;
            }

            if (!device.IsPowered)
            {
                HUD.Toast(device.DeviceName + "이(가) 작동하지 않습니다. 필요: " + device.Requirement +
                          " (현재 " + device.FilledCount + "개, 방전 건전지 확인)", ToastKind.Error);
                return;
            }

            player.ClearHeld();
            PlaceOnCounter(device);
            device.MarkSubmitted();
            GameManager.Instance.NotifyDeviceSubmitted(device);
            HUD.Toast(device.DeviceName + " 제출 완료!", ToastKind.Success);
        }

        /// <summary>세이브 복원용. 알림이나 집계 없이 제출된 모습만 재현한다.</summary>
        public void RestoreSubmitted(Device device)
        {
            if (device == null || device.IsSubmitted) return;
            PlaceOnCounter(device);
            device.MarkSubmitted();
        }

        /// <summary>새 게임을 시작할 때 자리 배정을 처음부터 다시 하도록 되돌린다.</summary>
        public void ResetPlacements()
        {
            nextAnchor = 0;
        }

        void PlaceOnCounter(Device device)
        {
            Transform anchor = null;
            if (placementAnchors != null && placementAnchors.Count > 0)
            {
                anchor = placementAnchors[Mathf.Min(nextAnchor, placementAnchors.Count - 1)];
                nextAnchor++;
            }

            float lift = HalfHeight(device);
            if (anchor != null)
            {
                device.AttachTo(anchor);
                device.transform.localPosition = Vector3.up * lift;
            }
            else
            {
                device.AttachTo(transform);
                device.transform.localPosition = new Vector3(0f, 0.98f + lift, 0f);
            }
        }

        /// <summary>기기 중심에서 바닥까지의 거리. 앵커 위에 정확히 올려놓기 위해 쓴다.</summary>
        static float HalfHeight(Device device)
        {
            var renderers = device.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return 0.05f;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return Mathf.Max(0.01f, device.transform.position.y - b.min.y);
        }
    }
}
