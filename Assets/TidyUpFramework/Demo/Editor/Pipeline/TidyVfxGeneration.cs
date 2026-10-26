using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyVfxGeneration
    {
        const string Folder = TidyDemoPaths.Defaults + "/VFX";
        const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        public static ParticleSystem Sparkle { get; private set; }
        public static ParticleSystem BigSparkle { get; private set; }
        public static ParticleSystem Dust { get; private set; }
        public static ParticleSystem Confetti { get; private set; }

        public static void Build()
        {
            TidyAssetFactory.EnsureFolder(Folder);
            var material = Material();
            Sparkle = Save("VFX_Sparkle", CreateSparkle(material, 14, 0.5f, 0.05f, new Color(1f, 0.92f, 0.6f)));
            BigSparkle = Save("VFX_SparkleBurst", CreateSparkle(material, 40, 1.1f, 0.08f, new Color(1f, 0.85f, 0.5f)));
            Dust = Save("VFX_DustPuff", CreateDust(material));
            Confetti = Save("VFX_Confetti", CreateConfetti(material));
            AssetDatabase.SaveAssets();
        }

        static Material Material()
        {
            var path = Folder + "/M_SoftParticle.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(ParticleShader)) { name = "M_SoftParticle" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", TidyUISprites.Glow != null ? TidyUISprites.Glow.texture : null);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_BLENDMODE_ADD");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreateSparkle(Material material, int count, float speed, float size, Color color)
        {
            var root = new GameObject("Sparkle");
            var system = root.AddComponent<ParticleSystem>();
            var main = system.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count * 2;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.06f;
            var alpha = system.colorOverLifetime;
            alpha.enabled = true;
            alpha.color = FadeGradient(Color.white);
            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            Configure(root, material);
            return root;
        }

        static GameObject CreateDust(Material material)
        {
            var root = new GameObject("Dust");
            var system = root.AddComponent<ParticleSystem>();
            var main = system.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(0.9f, 0.85f, 0.78f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var alpha = system.colorOverLifetime;
            alpha.enabled = true;
            alpha.color = FadeGradient(Color.white);
            Configure(root, material);
            return root;
        }

        static GameObject CreateConfetti(Material material)
        {
            var root = new GameObject("Confetti");
            var system = root.AddComponent<ParticleSystem>();
            var main = system.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0f),
                new GradientColorKey(new Color(0.5f, 0.85f, 1f), 0.33f),
                new GradientColorKey(new Color(1f, 0.55f, 0.75f), 0.66f),
                new GradientColorKey(new Color(0.6f, 1f, 0.6f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var alpha = system.colorOverLifetime;
            alpha.enabled = true;
            alpha.color = FadeGradient(Color.white);
            Configure(root, material);
            return root;
        }

        static ParticleSystem.MinMaxGradient FadeGradient(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        static void Configure(GameObject root, Material material)
        {
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static ParticleSystem Save(string prefabName, GameObject root)
        {
            var prefab = TidyAssetFactory.SavePrefab(root, Folder + "/" + prefabName + ".prefab");
            return prefab.GetComponent<ParticleSystem>();
        }
    }
}
