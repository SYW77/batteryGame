using UnityEngine;

namespace BatteryGame
{
    public enum BatterySize
    {
        AAA = 0,
        AA = 1,
        C = 2,
        D = 3,
        NineVolt = 4,
    }

    /// <summary>건전지 규격별 표시 이름 / 실측 치수 / 색상 테이블.</summary>
    public static class BatterySizes
    {
        /// <summary>실물 대비 확대 배율. 1인칭 시야에서 알아보기 쉬우라고 키워 둔 값.</summary>
        public const float Scale = 2f;

        public static readonly BatterySize[] All =
        {
            BatterySize.AAA, BatterySize.AA, BatterySize.C, BatterySize.D, BatterySize.NineVolt,
        };

        public static string Label(BatterySize size)
        {
            switch (size)
            {
                case BatterySize.AAA: return "AAA";
                case BatterySize.AA: return "AA";
                case BatterySize.C: return "C";
                case BatterySize.D: return "D";
                case BatterySize.NineVolt: return "9V";
                default: return size.ToString();
            }
        }

        public static bool IsBoxShaped(BatterySize size)
        {
            return size == BatterySize.NineVolt;
        }

        /// <summary>원통형 지름(m). 9V는 가로 폭.</summary>
        public static float Width(BatterySize size)
        {
            switch (size)
            {
                case BatterySize.AAA: return 0.0105f * Scale;
                case BatterySize.AA: return 0.0145f * Scale;
                case BatterySize.C: return 0.0262f * Scale;
                case BatterySize.D: return 0.0342f * Scale;
                case BatterySize.NineVolt: return 0.0265f * Scale;
                default: return 0.02f * Scale;
            }
        }

        public static float Height(BatterySize size)
        {
            switch (size)
            {
                case BatterySize.AAA: return 0.0445f * Scale;
                case BatterySize.AA: return 0.0505f * Scale;
                case BatterySize.C: return 0.0500f * Scale;
                case BatterySize.D: return 0.0615f * Scale;
                case BatterySize.NineVolt: return 0.0485f * Scale;
                default: return 0.05f * Scale;
            }
        }

        /// <summary>9V만 앞뒤 두께가 따로 있고, 나머지는 지름과 같다.</summary>
        public static float Depth(BatterySize size)
        {
            return size == BatterySize.NineVolt ? 0.0175f * Scale : Width(size);
        }

        public static Color BodyColor(BatterySize size)
        {
            switch (size)
            {
                case BatterySize.AAA: return new Color(0.86f, 0.36f, 0.16f);
                case BatterySize.AA: return new Color(0.16f, 0.42f, 0.80f);
                case BatterySize.C: return new Color(0.20f, 0.62f, 0.34f);
                case BatterySize.D: return new Color(0.58f, 0.26f, 0.70f);
                case BatterySize.NineVolt: return new Color(0.85f, 0.68f, 0.12f);
                default: return Color.gray;
            }
        }
    }
}
