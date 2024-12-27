using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamersInc.SceneManagement
{
    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        private SceneGroup activeSceneGroup;

        public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScene = true)
        {
            activeSceneGroup = group;
            var loadedScenes = new List<string>();
           
            await UnloadScene();
            
            var sceneCount = SceneManager.sceneCount;
            
            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalSceneToLoad = activeSceneGroup.Scenes.Count;
            
            var operationGroup = new AsyncOperationGroup(totalSceneToLoad);

            for (var i = 0; i < totalSceneToLoad; i++)
            {
                var sceneData = group.Scenes[i];
                if(reloadDupScene == false && loadedScenes.Contains(sceneData.Name)) continue;
                
                if (sceneData.Reference.State == SceneReferenceState.Regular)
                {
                    var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    operationGroup.Operations.Add(operation);
                }
             

                
                OnSceneLoaded.Invoke(sceneData.Name);
            }
            
            while(!operationGroup.IsDone){
                progress?.Report(operationGroup.Progress);
                await Task.Delay(150);
            }

            Scene activeScene =
                SceneManager.GetSceneByName(activeSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
           
            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
            
            OnSceneGroupLoaded.Invoke();
        }
        
            public async Task ReplaceActiveScene(SceneGroup group, IProgress<float> progress, bool reloadDupScene = true)
        {
            activeSceneGroup = group;
            var loadedScenes = new List<string>();
           
            await UnloadActiveScene();
            
            var sceneCount = SceneManager.sceneCount;
            
            
            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalSceneToLoad = activeSceneGroup.Scenes.Count;
            
            var operationGroup = new AsyncOperationGroup(totalSceneToLoad);

            for (var i = 0; i < totalSceneToLoad; i++)
            {
                var sceneData = group.Scenes[i];
                if(reloadDupScene == false && loadedScenes.Contains(sceneData.Name)) continue;
                
                if (sceneData.Reference.State == SceneReferenceState.Regular)
                {
                    var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    operationGroup.Operations.Add(operation);
                }
             

                
                OnSceneLoaded.Invoke(sceneData.Name);
            }
            
            while(!operationGroup.IsDone){
                progress?.Report(operationGroup.Progress);
                await Task.Delay(150);
            }

            Scene activeScene =
                SceneManager.GetSceneByName(activeSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
           
            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
            
            OnSceneGroupLoaded.Invoke();
        }

        private async Task UnloadScene()
        {
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;
            int sceneCount = SceneManager.sceneCount;

            for (int i = sceneCount-1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if(!sceneAt.isLoaded) continue;
                var sceneName = sceneAt.name;
                if( sceneName.Equals(activeScene) || sceneName == "Bootstrapper") continue;
                scenes.Add(sceneName);
            }

            var operationGroup = new AsyncOperationGroup(scenes.Count);
            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if(operation== null) continue;
                
                operationGroup.Operations.Add(operation);
                OnSceneLoaded.Invoke(scene);
            }

            while (!operationGroup.IsDone)
            {
                await Task.Delay(150);
            }
            
            //Optional Unload UnusedAssets
            await Resources.UnloadUnusedAssets();
        }

        private async Task UnloadActiveScene()
        {
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;
            var sceneCount = SceneManager.sceneCount;

            for (var i = sceneCount-1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if(!sceneAt.isLoaded) continue;
                var sceneName = sceneAt.name;
                if( !sceneName.Equals(activeScene) || sceneName == "Bootstrapper") continue;
                scenes.Add(sceneName);
            }

            var operationGroup = new AsyncOperationGroup(scenes.Count);
            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if(operation== null) continue;
                
                operationGroup.Operations.Add(operation);
                OnSceneLoaded.Invoke(scene);
            }

            while (!operationGroup.IsDone)
            {
                await Task.Delay(150);
            }
            
            //Optional Unload UnusedAssets
            await Resources.UnloadUnusedAssets();
        }
        

        public readonly struct AsyncOperationGroup
        {
            public readonly List<AsyncOperation> Operations;

            public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
            public bool IsDone => Operations.All(o => o.isDone);

            public AsyncOperationGroup(int initialCapacity) {
                Operations = new List<AsyncOperation>(initialCapacity);
            }
        }
    }
}