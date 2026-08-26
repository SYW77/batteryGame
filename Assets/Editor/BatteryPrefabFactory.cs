using UnityEditor;
using UnityEngine;

namespace BatteryGame.EditorTools
{
    /// <summary>규격별 건전지 프리팹을 프리미티브로 생성한다. 나중에 실제 모델로 교체할 지점.</summary>
    public static class BatteryPrefabFactory
    {
        public const string Dir = "Assets/Prefabs/Batteries";

        public static Battery[] BuildAll()
        {
            MatLib.EnsureFolder(Dir);

            Material aliveBand = MatLib.Get("Band_Alive", new Color(0.25f, 0.95f, 0.4f), 0.5f, 0f,
                                            new Color(0.10f, 0.45f, 0.16f));
            Material deadBand = MatLib.Get("Band_Dead", new Color(0.95f, 0.22f, 0.2f), 0.5f, 0f,
                                           new Color(0.45f, 0.06f, 0.05f));
            Material capMat = MatLib.Get("Battery_Cap", new Color(0.78f, 0.78f, 0.80f), 0.75f, 0.9f);

            var result = new Battery[BatterySizes.All.Length];
            for (int i = 0; i < BatterySizes.All.Length; i++)
            {
                BatterySize size = BatterySizes.All[i];
                result[(int)size] = Build(size, aliveBand, deadBand, capMat);
            }
            return result;
        }

        static Battery Build(BatterySize size, Material aliveBand, Material deadBand, Material capMat)
        {
            float w = BatterySizes.Width(size);
            float h = BatterySizes.Height(size);
            float d = BatterySizes.Depth(size);
            bool box = BatterySizes.IsBoxShaped(size);

            Material bodyMat = MatLib.Get("Battery_" + BatterySizes.Label(size),
                                          BatterySizes.BodyColor(size), 0.35f, 0.1f);

            var root = new GameObject("Battery_" + BatterySizes.Label(size));
            Renderer band;

            if (box)
            {
                Prim.Box("Body", root.transform, Vector3.zero, new Vector3(w, h, d), bodyMat);
                Prim.Cylinder("Terminal_A", root.transform, new Vector3(-w * 0.22f, h * 0.5f + 0.004f, 0f),
                              w * 0.30f, 0.008f, capMat);
                Prim.Cylinder("Terminal_B", root.transform, new Vector3(w * 0.22f, h * 0.5f + 0.004f, 0f),
                              w * 0.30f, 0.008f, capMat);
                var bandGo = Prim.Box("StatusBand", root.transform, Vector3.zero,
                                      new Vector3(w * 1.02f, h * 0.16f, d * 1.03f), aliveBand);
                band = bandGo.GetComponent<Renderer>();

                var bc = root.AddComponent<BoxCollider>();
                bc.size = new Vector3(w, h, d);
            }
            else
            {
                Prim.Cylinder("Body", root.transform, Vector3.zero, w, h * 0.96f, bodyMat);
                Prim.Cylinder("PositiveCap", root.transform, new Vector3(0f, h * 0.5f, 0f),
                              w * 0.42f, h * 0.07f, capMat);
                Prim.Cylinder("NegativeCap", root.transform, new Vector3(0f, -h * 0.48f, 0f),
                              w * 0.94f, h * 0.04f, capMat);
                var bandGo = Prim.Cylinder("StatusBand", root.transform, Vector3.zero, w * 1.04f, h * 0.18f, aliveBand);
                band = bandGo.GetComponent<Renderer>();

                var cc = root.AddComponent<CapsuleCollider>();
                cc.direction = 1; // Y
                cc.radius = w * 0.5f;
                cc.height = h;
            }

            band.enabled = false;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.015f, w * h * 220f);
            // 통이 없으므로 굴러다니지 않게 감쇠를 크게 준다.
            rb.angularDamping = 4f;
            rb.linearDamping = 0.15f;
            rb.interpolation = RigidbodyInterpolation.None;

            var battery = root.AddComponent<Battery>();
            battery.Configure(size, 1f);
            battery.BindVisuals(band, aliveBand, deadBand);

            string path = Dir + "/Battery_" + BatterySizes.Label(size) + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            return prefab.GetComponent<Battery>();
        }
    }
}
