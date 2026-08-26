using UnityEngine;

namespace BatteryGame
{
    /// <summary>월드 공간 라벨이 항상 카메라를 보게 한다.</summary>
    public class Billboard : MonoBehaviour
    {
        Camera cam;

        void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }
    }
}
