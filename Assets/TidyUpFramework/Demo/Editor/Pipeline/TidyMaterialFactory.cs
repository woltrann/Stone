using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public sealed class TidyMaterialSpec
    {
        public Color BaseColor = Color.white;
        public Texture2D BaseMap;
        public Texture2D NormalMap;
        public float NormalStrength = 1f;
        public float Smoothness = 0.35f;
        public float Metallic;
        public Color Emission = Color.black;
        public Vector2 Tiling = Vector2.one;
        public bool Transparent;
        public float Alpha = 1f;
        public bool DoubleSided;
    }

    public static class TidyMaterialFactory
    {
        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        const int TransparentQueue = 3000;

        public static Material Lit(string assetPath, TidyMaterialSpec spec)
        {
            var material = LoadOrCreate(assetPath, LitShaderName);
            var color = spec.BaseColor;
            color.a = spec.Transparent ? spec.Alpha : 1f;
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", spec.BaseMap);
            material.SetTextureScale("_BaseMap", spec.Tiling);
            material.SetFloat("_Smoothness", spec.Smoothness);
            material.SetFloat("_Metallic", spec.Metallic);
            ApplyNormalMap(material, spec);
            ApplyEmission(material, spec.Emission);
            ApplySurface(material, spec.Transparent, spec.DoubleSided);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Unlit(string assetPath, Color color, Texture2D texture = null, bool transparent = false)
        {
            var material = LoadOrCreate(assetPath, UnlitShaderName);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            ApplySurface(material, transparent, false);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material WithShader(string assetPath, string shaderName)
        {
            var material = LoadOrCreate(assetPath, shaderName);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreate(string assetPath, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                if (material.shader != shader && shader != null)
                {
                    material.shader = shader;
                }

                return material;
            }

            TidyAssetFactory.EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(assetPath) };
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        static void ApplyNormalMap(Material material, TidyMaterialSpec spec)
        {
            material.SetTexture("_BumpMap", spec.NormalMap);
            material.SetTextureScale("_BumpMap", spec.Tiling);
            material.SetFloat("_BumpScale", spec.NormalStrength);
            if (spec.NormalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }
        }

        static void ApplyEmission(Material material, Color emission)
        {
            var emissive = emission.maxColorComponent > 0.001f;
            material.SetColor("_EmissionColor", emission);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        static void ApplySurface(Material material, bool transparent, bool doubleSided)
        {
            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            material.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", transparent ? (float)BlendMode.One : (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            material.renderQueue = transparent ? TransparentQueue : -1;
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            if (transparent)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetShaderPassEnabled("ShadowCaster", true);
            }

            material.doubleSidedGI = doubleSided;
        }
    }
}
