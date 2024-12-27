using System;
using System.Threading.Tasks;
using DreamersInc.ServiceLocatorSystem;
using UnityEngine;
using UnityEngine.UI;

namespace DreamersInc.SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private Image loadingBar;
        [SerializeField] private float fillSpeed = .5f;
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private Camera loadingCamera;
        [SerializeField] private SceneGroup[] sceneGroups;
        
        private float targetProgress;
        private bool isLoading;

        private readonly SceneGroupManager manager = new SceneGroupManager();
        

        private async void Start()
        {
            ServiceLocator.Global.Register(this.GetType(), this);
            #if UNITY_EDITOR
            await LoadSceneGroup(0);
            #else
            await LoadSceneGroup(0);
#endif
        }

        private void Update()
        {
            if(isLoading)return;
            float currentFillAmount = loadingBar.fillAmount;
            float progressDiff = Mathf.Abs(currentFillAmount - targetProgress);
            float dynamicFillSpeed = progressDiff * fillSpeed;
            loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, targetProgress, Time.deltaTime * dynamicFillSpeed);
            
        }
        
        private void OnDestroy()
        {
            if(ServiceLocator.Global.TryGet<SceneLoader>(out _ )) 
                ServiceLocator.Global.Unregister(GetType(), this);
        }
        
        public async Task LoadSceneGroup(int index)
        {
            loadingBar.fillAmount = 0f;
            targetProgress = 1.0f;

            if (index < 0 || index >= sceneGroups.Length)
            {
                Debug.LogError($"Invalid scene group index {index}");
                return;
            }

            LoadingProgress progress = new LoadingProgress();
            progress.Progressed += target => targetProgress = Mathf.Max(target, targetProgress);
            
            EnableLoadingCanvas();
            await manager.LoadScenes(sceneGroups[index], progress);
            EnableLoadingCanvas(false);
            
        }
        public async Task ReplaceSceneGroup(int index)
        {
            loadingBar.fillAmount = 0f;
            targetProgress = 1.0f;

            if (index < 0 || index >= sceneGroups.Length)
            {
                Debug.LogError($"Invalid scene group index {index}");
                return;
            }

            LoadingProgress progress = new LoadingProgress();
            progress.Progressed += target => targetProgress = Mathf.Max(target, targetProgress);
            
            EnableLoadingCanvas();
            await manager.ReplaceActiveScene(sceneGroups[index], progress,false);
            EnableLoadingCanvas(false);
            
        }
        void EnableLoadingCanvas(bool enabled = true)
        {
            isLoading = enabled;
            loadingCanvas.gameObject.SetActive(enabled);
            loadingCanvas.gameObject.SetActive(enabled);
        }
    }
}