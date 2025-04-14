using OBSControl.Managers;
using SiraUtil.Affinity;

namespace OBSControl.HarmonyPatches;

internal class StandardLevelEndHook : IAffinity
{
    private readonly RecordingManager recordingManager;

    public StandardLevelEndHook(RecordingManager recordingManager)
    {
        this.recordingManager = recordingManager;
    }

    [AffinityPatch(typeof(GameplayLevelSceneTransitionEvents), nameof(GameplayLevelSceneTransitionEvents.HandleStandardLevelDidFinish))]
    public void StandardLevelDidFinish(
        StandardLevelScenesTransitionSetupDataSO standardLevelScenesTransitionSetupData,
        LevelCompletionResults levelCompletionResults)
    {
        recordingManager.OnStandardLevelFinished(standardLevelScenesTransitionSetupData, levelCompletionResults);
    }
}