using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidySpriteGeneration
    {
        const int PanelSize = 128;
        const int IconSize = 128;
        const float PanelRadius = 24f;
        const float SmallRadius = 14f;

        public static void Build()
        {
            TidyAssetFactory.EnsureFolder(TidyUISprites.Folder);
            Save("Tidy_RoundedPanel", TidySpriteBuilder.RoundedRect(PanelSize, PanelRadius), TidySpriteBuilder.RoundedRectBorder(PanelRadius));
            Save("Tidy_RoundedOutline", TidySpriteBuilder.RoundedRect(PanelSize, PanelRadius, 3f), TidySpriteBuilder.RoundedRectBorder(PanelRadius, 3f));
            Save("Tidy_RoundedSmall", TidySpriteBuilder.RoundedRect(64, SmallRadius), TidySpriteBuilder.RoundedRectBorder(SmallRadius));
            Save("Tidy_Shadow", TidySpriteBuilder.Shadow(PanelSize, PanelRadius, 20f), TidySpriteBuilder.ShadowBorder(PanelRadius, 20f));
            Save("Tidy_Circle", TidySpriteBuilder.Circle(IconSize), Vector4.zero);
            Save("Tidy_Ring", TidySpriteBuilder.Ring(IconSize, 6f), Vector4.zero);
            Save("Tidy_Star", TidySpriteBuilder.Star(IconSize), Vector4.zero);
            Save("Tidy_Check", TidySpriteBuilder.Check(IconSize), Vector4.zero);
            Save("Tidy_Cross", TidySpriteBuilder.Cross(IconSize), Vector4.zero);
            Save("Tidy_Chevron", TidySpriteBuilder.Chevron(IconSize), Vector4.zero);
            Save("Tidy_Lock", TidySpriteBuilder.Lock(IconSize), Vector4.zero);
            Save("Tidy_Crosshair", TidySpriteBuilder.Crosshair(64), Vector4.zero);
            Save("Tidy_Glow", TidySpriteBuilder.SoftGlow(IconSize), Vector4.zero);
            Save("Tidy_Diamond", TidySpriteBuilder.Diamond(IconSize), Vector4.zero);
            Save("Tidy_Lightbulb", TidySpriteBuilder.Lightbulb(IconSize), Vector4.zero);
            Save("Tidy_Hourglass", TidySpriteBuilder.Hourglass(IconSize), Vector4.zero);
            Save("Tidy_Gear", TidySpriteBuilder.Gear(IconSize), Vector4.zero);
            AssetDatabase.SaveAssets();
        }

        static void Save(string spriteName, Texture2D texture, Vector4 border)
        {
            TidySpriteBuilder.SaveSprite(texture, TidyUISprites.Folder + "/" + spriteName + ".png", border);
            Object.DestroyImmediate(texture);
        }
    }
}
