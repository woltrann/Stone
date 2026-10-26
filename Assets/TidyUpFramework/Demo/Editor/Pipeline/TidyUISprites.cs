using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyUISprites
    {
        public const string Folder = "Assets/TidyUpFramework/UI/Sprites";

        public static Sprite RoundedPanel => Load("Tidy_RoundedPanel");
        public static Sprite RoundedOutline => Load("Tidy_RoundedOutline");
        public static Sprite RoundedSmall => Load("Tidy_RoundedSmall");
        public static Sprite Circle => Load("Tidy_Circle");
        public static Sprite Ring => Load("Tidy_Ring");
        public static Sprite Star => Load("Tidy_Star");
        public static Sprite Check => Load("Tidy_Check");
        public static Sprite Cross => Load("Tidy_Cross");
        public static Sprite Chevron => Load("Tidy_Chevron");
        public static Sprite Lock => Load("Tidy_Lock");
        public static Sprite Crosshair => Load("Tidy_Crosshair");
        public static Sprite Glow => Load("Tidy_Glow");
        public static Sprite Diamond => Load("Tidy_Diamond");
        public static Sprite Shadow => Load("Tidy_Shadow");
        public static Sprite Lightbulb => Load("Tidy_Lightbulb");
        public static Sprite Hourglass => Load("Tidy_Hourglass");
        public static Sprite Gear => Load("Tidy_Gear");

        static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + spriteName + ".png");
        }
    }
}
