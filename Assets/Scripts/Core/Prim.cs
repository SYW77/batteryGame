using UnityEngine;

namespace BatteryGame
{
    /// <summary>
    /// 프리미티브로 프로토타입 형태를 조립하기 위한 헬퍼.
    /// 나중에 실제 모델로 교체할 때는 여기서 만든 오브젝트만 갈아끼우면 된다.
    /// </summary>
    public static class Prim
    {
        public static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        public static Shader LitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        /// <summary>씬/프리팹에 저장하지 않는 임시 머티리얼. 에디터에서는 MatLib를 쓸 것.</summary>
        public static Material NewMaterial(Color color, float smoothness = 0.25f, float metallic = 0f)
        {
            var m = new Material(LitShader());
            ApplyColor(m, color, smoothness, metallic);
            return m;
        }

        public static void ApplyColor(Material m, Color color, float smoothness = 0.25f, float metallic = 0f)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        }

        public static void SetEmission(Material m, Color emission)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        public static GameObject Shape(PrimitiveType type, string name, Transform parent,
                                       Vector3 localPos, Vector3 localScale, Material mat,
                                       bool keepCollider = false, Quaternion? localRot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (!keepCollider && col != null) SafeDestroy(col);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;

            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size,
                                     Material mat, bool keepCollider = false, Quaternion? localRot = null)
        {
            return Shape(PrimitiveType.Cube, name, parent, localPos, size, mat, keepCollider, localRot);
        }

        /// <summary>지름/높이를 실제 단위(m)로 받는 원통. 유니티 실린더는 높이 2단위라 절반으로 넣는다.</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 localPos,
                                          float diameter, float height, Material mat,
                                          bool keepCollider = false, Quaternion? localRot = null)
        {
            return Shape(PrimitiveType.Cylinder, name, parent, localPos,
                         new Vector3(diameter, height * 0.5f, diameter), mat, keepCollider, localRot);
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 localPos, float diameter,
                                        Material mat, bool keepCollider = false)
        {
            return Shape(PrimitiveType.Sphere, name, parent, localPos, Vector3.one * diameter, mat, keepCollider);
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }
    }
}
