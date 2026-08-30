using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ToyanBomb
{
    internal static class BombOverlayMaterials
    {
        private static Material _spriteMaterial;
        private static readonly Dictionary<int, Material> _textMaterials = new Dictionary<int, Material>();

        internal static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial != null)
                    return _spriteMaterial;

                Shader shader =
                    Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Transparent")
                    ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");

                if (shader == null)
                {
                    Plugin.Log.Warn("No non-emissive sprite shader found for ToyanBomb overlay");
                    return null;
                }

                _spriteMaterial = new Material(shader);
                _spriteMaterial.name = "__ToyanBomb_NoBloom_Sprite";
                _spriteMaterial.hideFlags = HideFlags.HideAndDontSave;

                // Keep HDR-ish material colour at plain white in LDR range.
                if (_spriteMaterial.HasProperty("_Color"))
                    _spriteMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 1f));

                Plugin.Log.Info(
                    $"ToyanBomb emote material created: shader={shader.name}"
                );

                return _spriteMaterial;
            }
        }

        internal static Material CreateTextMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null)
                return null;

            int key = font.GetInstanceID();
            if (_textMaterials.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            try
            {
                // Clone the actual font material so glyph atlas bindings remain valid,
                // then explicitly remove common glow/underlay/emission-style features.
                Material mat = new Material(font.material);
                mat.name = "__ToyanBomb_NoBloom_Text";
                mat.hideFlags = HideFlags.HideAndDontSave;

                DisableKeyword(mat, "GLOW_ON");
                DisableKeyword(mat, "UNDERLAY_ON");
                DisableKeyword(mat, "UNDERLAY_INNER");
                DisableKeyword(mat, "BEVEL_ON");

                SetFloatIfExists(mat, "_GlowPower", 0f);
                SetFloatIfExists(mat, "_GlowOuter", 0f);
                SetFloatIfExists(mat, "_GlowInner", 0f);
                SetFloatIfExists(mat, "_GlowOffset", 0f);
                SetFloatIfExists(mat, "_UnderlaySoftness", 0f);
                SetFloatIfExists(mat, "_UnderlayDilate", 0f);

                // Ensure all primary colours stay in LDR 0..1.
                SetColorIfExists(mat, "_FaceColor", Color.white);
                SetColorIfExists(mat, "_OutlineColor", new Color(0f, 0f, 0f, 0.92f));
                SetColorIfExists(mat, "_GlowColor", Color.clear);

                _textMaterials[key] = mat;

                Plugin.Log.Info(
                    $"ToyanBomb text material created from font={font.name} shader={mat.shader?.name ?? "unknown"}"
                );

                return mat;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn($"Failed to create ToyanBomb text material: {ex.Message}");
                return null;
            }
        }


        internal static void Shutdown()
        {
            if (_spriteMaterial != null)
            {
                UnityEngine.Object.Destroy(_spriteMaterial);
                _spriteMaterial = null;
            }

            foreach (Material mat in _textMaterials.Values)
            {
                if (mat != null)
                    UnityEngine.Object.Destroy(mat);
            }

            _textMaterials.Clear();
        }

        private static void DisableKeyword(Material mat, string keyword)
        {
            try
            {
                if (mat.IsKeywordEnabled(keyword))
                    mat.DisableKeyword(keyword);
            }
            catch { }
        }

        private static void SetFloatIfExists(Material mat, string name, float value)
        {
            try
            {
                if (mat.HasProperty(name))
                    mat.SetFloat(name, value);
            }
            catch { }
        }

        private static void SetColorIfExists(Material mat, string name, Color value)
        {
            try
            {
                if (mat.HasProperty(name))
                    mat.SetColor(name, value);
            }
            catch { }
        }
    }
}
