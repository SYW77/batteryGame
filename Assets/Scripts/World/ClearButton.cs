using UnityEngine;

namespace BatteryGame
{
    /// <summary>
    /// 제출대 앞에 놓인 물리 버튼. 건전지를 모두 처리하기 전에는 꺼져 있고,
    /// 정리가 끝나면 켜져서 누를 수 있게 된다.
    /// </summary>
    public class ClearButton : Interactable
    {
        [SerializeField] Renderer lamp;
        [SerializeField] Material lampOffMaterial;
        [SerializeField] Material lampOnMaterial;
        [SerializeField] Light lampLight;

        GameFlow flow;

        public bool Armed { get; private set; }

        public override string HoverTitle { get { return "정리 완료 버튼"; } }
        public override string HoverDetail { get { return "눌러서 결과 보기"; } }

        public void Bind(GameFlow gameFlow, Renderer lampRenderer, Material off, Material on, Light light)
        {
            flow = gameFlow;
            lamp = lampRenderer;
            lampOffMaterial = off;
            lampOnMaterial = on;
            lampLight = light;
        }

        public void Arm()
        {
            gameObject.SetActive(true);
            Armed = true;
            if (lamp != null && lampOnMaterial != null) lamp.sharedMaterial = lampOnMaterial;
            if (lampLight != null) lampLight.enabled = true;
        }

        public void Disarm()
        {
            Armed = false;
            if (lamp != null && lampOffMaterial != null) lamp.sharedMaterial = lampOffMaterial;
            if (lampLight != null) lampLight.enabled = false;
            gameObject.SetActive(false);
        }

        public override void Interact(PlayerInteractor player)
        {
            if (!Armed) return;
            if (player.Held != null)
            {
                HUD.Toast("손에 든 것을 내려놓고 눌러주세요. (G)", ToastKind.Warning);
                return;
            }
            if (flow == null) flow = FindFirstObjectByType<GameFlow>();
            if (flow != null) flow.ShowClearScreen();
        }
    }
}
