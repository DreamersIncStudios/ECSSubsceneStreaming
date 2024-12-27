using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamersInc.SceneManagement;
using Sirenix.Utilities;
using UnityEngine;
using Unity.Scenes;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Transforms;
// ReSharper disable Unity.BurstLoadingManagedType
// ReSharper disable Unity.BurstFunctionSignatureContainsManagedTypes

namespace DreamersInc.SceneStreaming
{
    public class SceneLoaderAsset : MonoBehaviour
    {
        public static SceneLoaderAsset Instance;
        public float GetLoadRange { get; private set; }
        [SerializeField] private float LoadRange = 150f;

        public SceneData[] SceneDataArray => sceneDataArray;
        [SerializeField] SceneData[] sceneDataArray;

        private void Awake()
        {
            Instance = this;
            GetLoadRange = LoadRange;
        }
    }

    [System.Serializable]
    public struct SceneData {
        public SubScene SubScene;
        public int SceneID;
    }
    public partial class SceneLoadingSystem : SystemBase
    {
        HashSet<int> loadedScenes;
        protected override void OnCreate()
        {
          loadedScenes = new HashSet<int>();
        }

        protected override void OnUpdate()
        {
            loadedScenes ??= new HashSet<int>();
            if(!SceneLoaderAsset.Instance) return;
            //TODO add check all to only consider Local Player
                Entities.WithStructuralChanges().WithAll<Player_Control>().ForEach((ref LocalTransform transform) =>
                 {
                     
                     var subScenesToLoad = new List<SubScene>();
                     var scenesToLoad = new List<int>();
                     var sceneToUnload = new List<SubScene>();
                     foreach (var sceneData in SceneLoaderAsset.Instance.SceneDataArray)
                     {
                         var isNearby = Vector3.Distance(transform.Position, sceneData.SubScene.transform.position) < SceneLoaderAsset.Instance.GetLoadRange;

                         switch (isNearby)
                         {
                             case true when !loadedScenes.Contains(sceneData.SceneID):
                                 subScenesToLoad.Add(sceneData.SubScene);
                                 scenesToLoad.Add(sceneData.SceneID);
                                 loadedScenes.Add(sceneData.SceneID);
                                 break;
                             case false when loadedScenes.Contains(sceneData.SceneID):
                                if(subScenesToLoad.Contains(sceneData.SubScene)) return;
                                sceneToUnload.Add(sceneData.SubScene);
                                 UnloadScene(sceneData.SceneID);
                                 loadedScenes.Remove(sceneData.SceneID);
                                 break;
                         }
                     }
                     if(!sceneToUnload.IsNullOrEmpty())
                        UnloadSubScene(sceneToUnload);
                     if (subScenesToLoad.IsNullOrEmpty()) return;
                     LoadSubScene(subScenesToLoad);
                     LoadScene(scenesToLoad);
                 }).Run();
        }

        private void LoadSubScene(List<SubScene> subScene)
        {
            foreach (var t in subScene)
            {
                SceneSystem.LoadSceneAsync(World.Unmanaged, t.SceneGUID);
            }

        }

        private void LoadScene(List<int> scenes)
        {
            var operationGroup = new SceneGroupManager.AsyncOperationGroup(scenes.Count);
            foreach (var scene in scenes)
            {

                var test = SceneManager.GetSceneByBuildIndex(scene);

                if (!test.isLoaded)
                {
                    var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                    operationGroup.Operations.Add(operation);
                }
            }
        }

        void UnloadSubScene(List<SubScene> subScene)
        {
            foreach (var scene in subScene)
            {
                SceneSystem.UnloadScene(World.Unmanaged, scene.SceneGUID);
            }
        }

        void UnloadScene(int scene)
        {
            var test = SceneManager.GetSceneByBuildIndex(scene);
            if (test.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
        }

        public async Task LoadSubScene(SubScene scene)
        {
           var sceneEntity = SceneSystem.LoadSceneAsync(World.Unmanaged, scene.SceneGUID);

           while (!SceneSystem.IsSceneLoaded(World.Unmanaged, sceneEntity))
               await Task.Yield();


        }
    }
}