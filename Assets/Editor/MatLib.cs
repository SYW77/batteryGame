using System.IO;
using UnityEditor;
using UnityEngine;

namespace BatteryGame.EditorTools
{
    /// <summary>
    /// 프로토타입용 단색 머티리얼을 에셋으로 만들어 재사용한다.
    /// (씬에 new Material()을 그대로 붙이면 저장이 안 되므로 반드시 에셋으로 굽는다.)
    /// </summary>
    public static class MatLib
    {
        public const string Dir = "Assets/Materials/Generated";

        public static Material Get(string name, Color color, float smoothness = 0.25f,
                                   float metallic = 0f, Color? emission = null)
        {
            EnsureDir();
            string path = Dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Prim.LitShader());
                AssetDatabase.CreateAsset(m, path);
            }
            Prim.ApplyColor(m, color, smoothness, metallic);
            Prim.SetEmission(m, emission ?? Color.black);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void EnsureDir()
        {
            EnsureFolder(Dir);
        }

        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace(Path.DirectorySeparatorChar, '/');
            string leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
