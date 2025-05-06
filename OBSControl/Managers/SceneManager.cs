using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using Zenject;

namespace OBSControl.Managers;

internal class SceneManager : IInitializable, IDisposable
{
    private readonly IOBSWebsocket obsWebsocket;
    private readonly EventManager eventManager;

    public SceneManager(IOBSWebsocket obsWebsocket, EventManager eventManager)
    {
        this.obsWebsocket = obsWebsocket;
        this.eventManager = eventManager;
    }

    private HashSet<string> sceneNames = [];
    
    public IEnumerable<string> SceneNames => sceneNames;

    public void Initialize()
    {
        eventManager.SceneNamesUpdated += SceneNamesUpdated;
    }

    public void Dispose()
    {
        eventManager.SceneNamesUpdated -= SceneNamesUpdated;
    }

    public bool Contains(string sceneName) => !string.IsNullOrEmpty(sceneName) && sceneNames.Contains(sceneName);

    public async Task<bool> TransitionToScene(string sceneName)
    {
        try
        {
            if (!obsWebsocket.IsConnected || !Contains(sceneName)) return false;

            var transitionDuration = obsWebsocket.GetCurrentSceneTransition().Duration ?? 0;
            obsWebsocket.SetCurrentProgramScene(sceneName);

            // Wait for the transition to finish before finishing the task
            if (transitionDuration > 0) await Task.Delay(transitionDuration);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Encountered a problem while trying to transition to {sceneName}: {ex}");
            return false;
        }
    }

    private void SceneNamesUpdated(IEnumerable<string> scenes)
    {
        sceneNames = scenes.ToHashSet();
    }
}