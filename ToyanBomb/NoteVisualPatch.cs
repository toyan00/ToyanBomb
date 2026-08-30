using HarmonyLib;

namespace ToyanBomb
{
    [HarmonyPatch(typeof(ColorNoteVisuals), "HandleNoteControllerDidInit")]
    [HarmonyPriority(Priority.Last)]
    internal static class NoteVisualPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ColorNoteVisuals __instance, NoteControllerBase noteController)
        {
            GameNoteController gameNote =
                noteController as GameNoteController ??
                __instance?.GetComponentInParent<GameNoteController>();

            if (gameNote == null)
                return;

            BombVisualFactory.CleanupPrevious(gameNote);

            // Do not arm any new note while the gameplay toggle is OFF.
            if (!BombSettings.Enabled)
                return;

            if (!BombQueue.TryTake(out BombRequest request))
                return;

            if (BombVisualFactory.TryApply(gameNote, __instance, request))
            {
                Plugin.Log.Info(
                    $"Assigned !bomb from {request.UserName} to next note; pending={BombQueue.Pending}"
                );
            }
            else
            {
                BombQueue.Requeue(request);
                Plugin.Log.Warn(
                    $"Bomb visual failed; requeued {request.UserName}; pending={BombQueue.Pending}"
                );
            }
        }
    }
}
