using System;
using System.Collections.Generic;
using TidyUpFramework.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Interaction
{
    [Serializable]
    public sealed class TidyHighlightAppearance
    {
        public Color Color = Color.white;
        [Min(0f)] public float WidthPixels = 3f;
        [Min(0f)] public float PulseSpeed;
        public bool VisibleThroughWalls;
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Interaction/Outline Highlighter")]
    public sealed class TidyOutlineHighlighter : MonoBehaviour, ITidyHighlighter
    {
        const string OutlineShaderName = "TidyUpFramework/Outline";
        static readonly int StyleCount = Enum.GetValues(typeof(TidyHighlightStyle)).Length;
        static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
        static readonly int PulseId = Shader.PropertyToID("_PulseSpeed");
        static readonly int ZTestId = Shader.PropertyToID("_ZTest");

        [SerializeField]
        Shader outlineShader;

        [SerializeField]
        TidyHighlightAppearance hover = new TidyHighlightAppearance { Color = new Color(1f, 0.93f, 0.7f, 0.95f), WidthPixels = 2.5f };

        [SerializeField]
        TidyHighlightAppearance hint = new TidyHighlightAppearance { Color = new Color(0.45f, 0.85f, 1f, 1f), WidthPixels = 4f, PulseSpeed = 5f, VisibleThroughWalls = true };

        [SerializeField]
        TidyHighlightAppearance valid = new TidyHighlightAppearance { Color = new Color(0.45f, 1f, 0.55f, 1f), WidthPixels = 3f };

        [SerializeField]
        TidyHighlightAppearance invalid = new TidyHighlightAppearance { Color = new Color(1f, 0.35f, 0.3f, 1f), WidthPixels = 3f };

        sealed class Entry
        {
            public IReadOnlyList<Renderer> Renderers;
            public readonly Dictionary<object, TidyHighlightStyle> StylesByOwner = new Dictionary<object, TidyHighlightStyle>();
            public Material AppearanceOverride;
            public object AppearanceOwner;

            public TidyHighlightStyle Strongest
            {
                get
                {
                    var strongest = TidyHighlightStyle.None;
                    foreach (var style in StylesByOwner.Values)
                    {
                        if (style > strongest)
                        {
                            strongest = style;
                        }
                    }

                    return strongest;
                }
            }
        }

        readonly Dictionary<Component, Entry> entries = new Dictionary<Component, Entry>();
        readonly Dictionary<Renderer, Mesh> meshByRenderer = new Dictionary<Renderer, Mesh>();
        readonly List<Component> staleKeys = new List<Component>();
        Material[] materialsByStyle;

        public void SetHighlight(Component key, IReadOnlyList<Renderer> renderers, TidyHighlightStyle style, object owner)
        {
            if (key == null || owner == null)
            {
                return;
            }

            if (style == TidyHighlightStyle.None)
            {
                ClearHighlight(key, owner);
                return;
            }

            if (!entries.TryGetValue(key, out var entry))
            {
                entry = new Entry();
                entries[key] = entry;
            }

            entry.Renderers = renderers;
            entry.StylesByOwner[owner] = style;
        }

        // The caller owns the material. Existing highlight calls keep their shared style materials.
        public void SetHighlight(Component key, IReadOnlyList<Renderer> renderers, TidyHighlightStyle style, object owner, Material appearance)
        {
            SetHighlight(key, renderers, style, owner);
            if (key != null && entries.TryGetValue(key, out var entry))
            {
                entry.AppearanceOverride = appearance;
                entry.AppearanceOwner = owner;
            }
        }

        public void ClearHighlight(Component key, object owner)
        {
            if (key == null || !entries.TryGetValue(key, out var entry))
            {
                return;
            }

            entry.StylesByOwner.Remove(owner);
            if (entry.AppearanceOwner == owner) { entry.AppearanceOverride = null; entry.AppearanceOwner = null; }
            if (entry.StylesByOwner.Count == 0)
            {
                entries.Remove(key);
            }
        }

        public void ClearOwner(object owner)
        {
            staleKeys.Clear();
            foreach (var pair in entries)
            {
                pair.Value.StylesByOwner.Remove(owner);
                if (pair.Value.AppearanceOwner == owner) { pair.Value.AppearanceOverride = null; pair.Value.AppearanceOwner = null; }
                if (pair.Value.StylesByOwner.Count == 0)
                {
                    staleKeys.Add(pair.Key);
                }
            }

            foreach (var key in staleKeys)
            {
                entries.Remove(key);
            }
        }

        void OnEnable()
        {
            TidyServices.Register<ITidyHighlighter>(this);
            BuildMaterials();
        }

        void OnDisable()
        {
            TidyServices.Unregister<ITidyHighlighter>(this);
        }

        void OnDestroy()
        {
            if (materialsByStyle == null)
            {
                return;
            }

            foreach (var material in materialsByStyle)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        void BuildMaterials()
        {
            if (materialsByStyle != null && materialsByStyle.Length == StyleCount)
            {
                return;
            }

            var shader = outlineShader != null ? outlineShader : Shader.Find(OutlineShaderName);
            if (shader == null)
            {
                Debug.LogWarning("Tidy Up Framework: outline shader not found. Highlights are disabled.", this);
                return;
            }

            materialsByStyle = new Material[StyleCount];
            materialsByStyle[(int)TidyHighlightStyle.Hover] = CreateMaterial(shader, hover);
            materialsByStyle[(int)TidyHighlightStyle.Hint] = CreateMaterial(shader, hint);
            materialsByStyle[(int)TidyHighlightStyle.Valid] = CreateMaterial(shader, valid);
            materialsByStyle[(int)TidyHighlightStyle.Invalid] = CreateMaterial(shader, invalid);
        }

        static Material CreateMaterial(Shader shader, TidyHighlightAppearance appearance)
        {
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
            material.SetColor(ColorId, appearance.Color);
            material.SetFloat(WidthId, appearance.WidthPixels);
            material.SetFloat(PulseId, appearance.PulseSpeed);
            material.SetFloat(ZTestId, appearance.VisibleThroughWalls ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            return material;
        }

        void LateUpdate()
        {
            // Unity hot reload can restore a non-null but empty runtime material array.
            if (materialsByStyle == null || materialsByStyle.Length != StyleCount)
                BuildMaterials();
            if (materialsByStyle == null || entries.Count == 0)
            {
                return;
            }

            staleKeys.Clear();
            foreach (var pair in entries)
            {
                if (pair.Key == null)
                {
                    staleKeys.Add(pair.Key);
                    continue;
                }

                var material = pair.Value.AppearanceOverride != null ? pair.Value.AppearanceOverride : materialsByStyle[(int)pair.Value.Strongest];
                if (material != null && pair.Key.gameObject.activeInHierarchy)
                {
                    Draw(pair.Value.Renderers, material);
                }
            }

            foreach (var key in staleKeys)
            {
                entries.Remove(key);
            }
        }

        void Draw(IReadOnlyList<Renderer> renderers, Material material)
        {
            if (renderers == null)
            {
                return;
            }

            var renderParams = new RenderParams(material)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                layer = gameObject.layer
            };

            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var mesh = ResolveMesh(renderer);
                if (mesh == null)
                {
                    continue;
                }

                var matrix = renderer.localToWorldMatrix;
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    Graphics.RenderMesh(renderParams, mesh, subMesh, matrix);
                }
            }
        }

        Mesh ResolveMesh(Renderer renderer)
        {
            if (meshByRenderer.TryGetValue(renderer, out var mesh))
            {
                return mesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            meshByRenderer[renderer] = mesh;
            return mesh;
        }
    }
}
