using System;
using System.Linq;
using UnityEngine;

namespace ToyanBomb
{
    internal static class BombVisualFactory
    {
        private const string VisualName = "__ToyanBombVisual";

        private static Mesh _bombMesh;
        private static Material[] _bombMaterials;
        private static MaterialPropertyBlock _bombPropertyBlock;
        private static Vector3 _sourceLocalPosition;
        private static Quaternion _sourceLocalRotation;
        private static Vector3 _sourceLocalScale;
        private static bool _sourceCached;

        internal static bool TryApply(
            GameNoteController note,
            ColorNoteVisuals colorVisuals,
            BombRequest request)
        {
            if (note == null || request == null)
                return false;

            CleanupPrevious(note);

            if (!TryCacheBombVisualSource())
            {
                Plugin.Log.Warn("Could not cache bomb Mesh/Material");
                return false;
            }

            GameObject visual = null;
            // Keep the original gameplay note untouched. ToyanBomb adds only a child bomb visual.
            try
            {
                int originalRendererCount = note.GetComponentsInChildren<Renderer>(true)
                    .Count(r => r != null && r.enabled &&
                                !(r.gameObject.name ?? string.Empty).StartsWith("__ToyanBomb", StringComparison.Ordinal));

                visual = new GameObject(VisualName);
                visual.layer = note.gameObject.layer;
                visual.transform.SetParent(note.transform, false);
                visual.transform.localPosition = _sourceLocalPosition;
                visual.transform.localRotation = _sourceLocalRotation;
                // v0.6.7: restore the ToyanBomb shell to its original bomb size.
                visual.transform.localScale = _sourceLocalScale * BombSettings.BombSize;


                MeshFilter mf = visual.AddComponent<MeshFilter>();
                mf.sharedMesh = _bombMesh;

                MeshRenderer mr = visual.AddComponent<MeshRenderer>();
                mr.sharedMaterials = _bombMaterials;

                // BombNoteHD relies on runtime shader properties. Copy the property block
                // from an actual vanilla Bomb renderer instead of just the material.
                if (_bombPropertyBlock != null)
                    mr.SetPropertyBlock(_bombPropertyBlock);

                // v0.2.0: animate only the replacement Bomb visual through the rainbow.
                RainbowBombVisual rainbow = visual.AddComponent<RainbowBombVisual>();
                rainbow.Initialize(mr, _bombPropertyBlock);

                // v0.8.4: initialize the rainbow first, then override its final
                // renderer PropertyBlock/material glow values.
                ApplyBombGlow(mr);

                visual.SetActive(true);

                var marker = note.gameObject.AddComponent<BombVisualMarker>();
                marker.Initialize(request, visual);

Plugin.Log.Info(
                    $"Hybrid Rainbow Bomb armed: user={request.UserName}, mesh={_bombMesh.name}, " +
                    $"materials=[{string.Join(",", _bombMaterials.Select(m => m != null ? m.name : "<null>"))}], " +
                    $"originalRenderers={originalRendererCount}, noteVisualUntouched=true, shellScale={BombSettings.BombSize:0.00}"
                );

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Bomb visual apply failed: {ex}");

                if (visual != null)
                    UnityEngine.Object.Destroy(visual);

                return false;
            }
        }

        internal static void CleanupPrevious(GameNoteController note)
        {
            if (note == null)
                return;

            foreach (BombVisualMarker marker in note.GetComponents<BombVisualMarker>())
            {
                if (marker == null)
                    continue;

                marker.Restore();
                UnityEngine.Object.Destroy(marker);
            }

            Transform stale = note.transform.Find(VisualName);
            if (stale != null)
                UnityEngine.Object.Destroy(stale.gameObject);
        }

        private static bool TryCacheBombVisualSource()
        {
            if (_sourceCached && _bombMesh != null && _bombMaterials != null)
                return true;

            var candidates = Resources
                .FindObjectsOfTypeAll<BombNoteController>()
                .Where(x => x != null && x.gameObject != null)
                .ToList();

            Renderer bestRenderer = null;
            int bestScore = int.MinValue;

            foreach (BombNoteController candidate in candidates)
            {
                foreach (Renderer renderer in candidate.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;

                    MeshFilter mf = renderer.GetComponent<MeshFilter>();
                    Mesh mesh = mf?.sharedMesh;
                    if (mesh == null)
                        continue;

                    int score = 0;
                    string meshName = mesh.name ?? "";

                    if (string.Equals(meshName, "Bomb", StringComparison.OrdinalIgnoreCase))
                        score += 1000;
                    else if (meshName.IndexOf("bomb", StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 500;

                    if (candidate.gameObject.scene.name == "GameCore")
                        score += 100;

                    if (candidate.gameObject.scene.isLoaded)
                        score += 50;

                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if ((mat?.name ?? "").IndexOf("BombNoteHD", StringComparison.OrdinalIgnoreCase) >= 0)
                            score += 200;
                    }

                    // Prefer a live pooled instance over the immutable prefab, because
                    // its MaterialPropertyBlock has already been initialized by Beat Saber.
                    if (renderer.gameObject.scene.IsValid() && renderer.gameObject.scene.isLoaded)
                        score += 50;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestRenderer = renderer;
                    }
                }
            }

            if (bestRenderer == null)
                return false;

            MeshFilter sourceFilter = bestRenderer.GetComponent<MeshFilter>();
            _bombMesh = sourceFilter.sharedMesh;
            _bombMaterials = bestRenderer.sharedMaterials.ToArray();

            _sourceLocalPosition = bestRenderer.transform.localPosition;
            _sourceLocalRotation = bestRenderer.transform.localRotation;
            _sourceLocalScale = bestRenderer.transform.localScale;

            _bombPropertyBlock = new MaterialPropertyBlock();
            bestRenderer.GetPropertyBlock(_bombPropertyBlock);

            _sourceCached = true;

            Plugin.Log.Info(
                $"Cached bomb render source: renderer={bestRenderer.name}, mesh={_bombMesh.name}, " +
                $"materials=[{string.Join(",", _bombMaterials.Select(m => m != null ? m.name : "<null>"))}], " +
                $"propertyBlockEmpty={_bombPropertyBlock.isEmpty}, score={bestScore}"
            );

            return true;
        }
    


        private static void ApplyBombGlow(Renderer renderer)
        {
            if (renderer == null)
                return;

            const float glow01 = 1.0f;

            try
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                int simpleColorId = Shader.PropertyToID("_SimpleColor");
                Color c = block.GetColor(simpleColorId);

                if (c.r == 0f && c.g == 0f && c.b == 0f && c.a == 0f)
                    c = Color.white;

                // 0% keeps normal visible RGB; 100% restores a strong HDR multiplier.
                float hdr = Mathf.Lerp(1.0f, 4.0f, glow01);
                c = new Color(c.r * hdr, c.g * hdr, c.b * hdr, c.a > 0f ? c.a : 1f);
                block.SetColor(simpleColorId, c);

                block.SetFloat(Shader.PropertyToID("_Bloom"), glow01);
                block.SetFloat(Shader.PropertyToID("_BloomIntensity"), glow01);
                block.SetFloat(Shader.PropertyToID("_Glow"), glow01);
                block.SetFloat(Shader.PropertyToID("_GlowIntensity"), glow01);

                renderer.SetPropertyBlock(block);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Bomb glow PropertyBlock failed: {ex.Message}");
            }

            try
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat == null) continue;

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color e = mat.GetColor("_EmissionColor");
                        mat.SetColor("_EmissionColor", e * glow01);
                    }
                    if (mat.HasProperty("_Bloom")) mat.SetFloat("_Bloom", glow01);
                    if (mat.HasProperty("_BloomIntensity")) mat.SetFloat("_BloomIntensity", glow01);
                    if (mat.HasProperty("_Emission")) mat.SetFloat("_Emission", glow01);
                    if (mat.HasProperty("_Glow")) mat.SetFloat("_Glow", glow01);
                    if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", glow01);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Bomb glow material failed: {ex.Message}");
            }

            Plugin.Log?.Info("Applied bomb visual glow defaults");
        }


}


}