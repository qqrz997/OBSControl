using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace OBSControl.Managers;

internal class SceneManager : IInitializable, IDisposable
{
    private readonly EventManager eventManager;

    public SceneManager(EventManager eventManager)
    {
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

    public bool Contains(string sceneName) => sceneNames.Contains(sceneName);

    private void SceneNamesUpdated(IEnumerable<string> scenes)
    {
        sceneNames = scenes.ToHashSet();
    }
}