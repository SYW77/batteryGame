using UnityEngine;

namespace BatteryGame
{
    /// <summary>
    /// 월드 공간 라벨이 항상 카메라를 보게 한다.
    /// 기기가 많아지면 라벨이 서로 겹쳐 읽을 수 없으므로, 거리에 따라 흐려지게 할 수도 있다.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("이 거리부터 흐려지기 시작한다. end와 같거나 크면 페이드하지 않는다.")]
        [SerializeField] float fadeStartDistance;
        [SerializeField] float fadeEndDistance;

        Camera cam;
        CanvasGroup group;
        Canvas canvas;

        public void ConfigureFade(float start, float end)
        {
            fadeStartDistance = start;
            fadeEndDistance = end;
        }

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            canvas = GetComponent<Canvas>();
        }

        void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            Vector3 toCamera = transform.position - cam.transform.position;
            transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);

            if (group == null || fadeEndDistance <= fadeStartDistance) return;

            float distance = toCamera.magnitude;
            float alpha = 1f - Mathf.Clamp01((distance - fadeStartDistance) /
                                             (fadeEndDistance - fadeStartDistance));
            group.alpha = alpha;

            // 완전히 투명해지면 그리지 않는다.
            if (canvas != null) canvas.enabled = alpha > 0.01f;
        }
    }
}
