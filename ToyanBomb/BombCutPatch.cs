using HarmonyLib;
using UnityEngine;

namespace ToyanBomb
{
    /// <summary>
    /// Runs AFTER the original normal-note HandleCut.
    /// We never alter cut scoring, saber/color checks, direction, or NoteData.
    /// </summary>
    [HarmonyPatch(typeof(GameNoteController), "HandleCut")]
    [HarmonyPriority(Priority.Low)]
    internal static class BombCutPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            GameNoteController __instance,
            Saber saber,
            Vector3 cutPoint,
            Quaternion orientation,
            Vector3 cutDirVec,
            bool allowBadCut)
        {
            if (__instance == null)
                return;

            // OFF means fully OFF: no celebration from a note that happened to be
            // armed just before the toggle was changed.
            if (!BombSettings.Enabled)
                return;

            BombVisualMarker marker = __instance.GetComponent<BombVisualMarker>();
            if (marker == null)
                return;

            if (!marker.TryMarkCut(out BombRequest request))
                return;

            BombStatus.Resolved();

            string userName = request?.UserName ?? marker.UserName ?? string.Empty;
            string logLabel = string.IsNullOrWhiteSpace(userName)
                ? "(emote-only)"
                : userName;

            Plugin.Log.Info(
                $"ToyanBomb CUT! user={logLabel} emotes={request?.Emotes?.Count ?? 0} point={cutPoint}"
            );

            // Hide/remove our replacement visual without restoring the cube.
            marker.DetachForCut();

            BombCelebration.Spawn(cutPoint, request);
        }
    }
}
