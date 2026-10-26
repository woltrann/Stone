using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyColor
    {
        public static Color Shade(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        public static Color Opaque(Color color)
        {
            return new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), 1f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color Vary(Color color, float hueShift, float saturationScale, float valueScale)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            Color result = Color.HSVToRGB(TidyMath.Frac(hue + hueShift), Mathf.Clamp01(saturation * saturationScale), Mathf.Clamp01(value * valueScale));
            result.a = color.a;
            return result;
        }

        public static Color Multiply(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
        }
    }
}
