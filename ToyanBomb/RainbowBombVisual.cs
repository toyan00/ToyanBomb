using System;
using UnityEngine;

namespace ToyanBomb
{
    /// <summary>
    /// Makes only the ToyanBomb render copy cycle through a vivid rainbow.
    /// No gameplay data or note judgement is touched.
    /// </summary>
    internal sealed class RainbowBombVisual : MonoBehaviour
    {
        private Renderer _renderer;
        private Material[] _materials;
        private MaterialPropertyBlock _block;
        private float _hueOffset;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SimpleColorId = Shader.PropertyToID("_SimpleColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int Color0Id = Shader.PropertyToID("_Color0");
        private static readonly int Color1Id = Shader.PropertyToID("_Color1");

        internal void Initialize(Renderer renderer, MaterialPropertyBlock sourceBlock)
        {
            _renderer = renderer;
            _block = new MaterialPropertyBlock();

            if (sourceBlock != null)
                CopyKnownSourceProperties(sourceBlock);

            // Give each simultaneous bomb a slightly different phase.
            _hueOffset = UnityEngine.Random.value;

            // Make private material instances so animation can never recolor vanilla bombs.
            Material[] shared = renderer.sharedMaterials;
            _materials = new Material[shared.Length];

            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null)
                    continue;

                _materials[i] = new Material(shared[i]);
                _materials[i].name = shared[i].name + " [ToyanRainbow]";
            }

            renderer.materials = _materials;
            ApplyRainbow(0f);
        }

        private void Update()
        {
            // About one full rainbow cycle per 1.6 seconds.
            ApplyRainbow(Time.time * 0.62f + _hueOffset);
        }

        private void ApplyRainbow(float hue)
        {
            if (_renderer == null)
                return;

            Color color = Color.HSVToRGB(Mathf.Repeat(hue, 1f), 0.95f, 1f);
            Color bright = color * 2.4f;
            bright.a = 1f;

            // Update material properties that actually exist on the bomb shader.
            if (_materials != null)
            {
                foreach (Material material in _materials)
                {
                    if (material == null)
                        continue;

                    SetIfPresent(material, ColorId, color);
                    SetIfPresent(material, BaseColorId, color);
                    SetIfPresent(material, SimpleColorId, color);
                    SetIfPresent(material, GlowColorId, bright);
                    SetIfPresent(material, EmissionColorId, bright);
                    SetIfPresent(material, Color0Id, color);
                    SetIfPresent(material, Color1Id, Color.HSVToRGB(Mathf.Repeat(hue + 0.18f, 1f), 0.95f, 1f));

                    if (material.HasProperty(EmissionColorId))
                        material.EnableKeyword("_EMISSION");
                }
            }

            // Also push common color names through the property block. This is important
            // for Beat Saber's HD note/bomb shaders, which use runtime property blocks.
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(SimpleColorId, color);
            _block.SetColor(GlowColorId, bright);
            _block.SetColor(EmissionColorId, bright);
            _block.SetColor(Color0Id, color);
            _block.SetColor(Color1Id, Color.HSVToRGB(Mathf.Repeat(hue + 0.18f, 1f), 0.95f, 1f));
            _renderer.SetPropertyBlock(_block);
        }

        private static void SetIfPresent(Material material, int propertyId, Color color)
        {
            if (material.HasProperty(propertyId))
                material.SetColor(propertyId, color);
        }

        private void CopyKnownSourceProperties(MaterialPropertyBlock source)
        {
            // Unity has no public "clone all MPB values" API. BombVisualFactory already
            // applies the complete source block before this component runs; this local
            // block is then populated from the renderer each frame via GetPropertyBlock().
        }

        private void OnDestroy()
        {
            if (_materials == null)
                return;

            foreach (Material material in _materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }
}
