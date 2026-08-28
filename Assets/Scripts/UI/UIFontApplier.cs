using UnityEngine;
using UnityEngine.UI;

namespace BatteryGame
{
    /// <summary>
    /// 유니티 내장 레거시 폰트에는 한글 글리프가 없어서, 런타임에 OS 폰트로 갈아끼운다.
    /// (에디터 씬 뷰에서는 네모로 보일 수 있지만 플레이하면 정상 표시된다.)
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class UIFontApplier : MonoBehaviour
    {
        [SerializeField]
        string[] preferredFonts =
        {
            "Malgun Gothic", "맑은 고딕", "Noto Sans KR", "NanumGothic",
            "Gulim", "Dotum", "Arial Unicode MS",
        };

        void Awake()
        {
            Font font = ResolveFont();
            if (font == null) return;

            var texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = font;
                texts[i].SetAllDirty();
            }
        }

        Font ResolveFont()
        {
            string[] installed = Font.GetOSInstalledFontNames();
            for (int i = 0; i < preferredFonts.Length; i++)
            {
                for (int j = 0; j < installed.Length; j++)
                {
                    if (!string.Equals(installed[j], preferredFonts[i], System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    var f = Font.CreateDynamicFontFromOSFont(installed[j], 32);
                    if (f != null) return f;
                }
            }
            return null;
        }
    }
}
