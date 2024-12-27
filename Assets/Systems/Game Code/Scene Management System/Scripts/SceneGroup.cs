using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;

namespace DreamersInc.SceneManagement{

    [Serializable]
    public class SceneGroup
    {
        public string GroupName = "New Group Data";
        public List<SceneData> Scenes;

        public string FindSceneNameByType(SceneType type)
        {
            return Scenes.FirstOrDefault(scene => scene.SceneType == type)?.Reference.Name;
        }
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public string Name => Reference.Name;
        public SceneType SceneType;
    }

    public enum SceneType
    { ActiveScene, HUD, UI, MainMenu,Cinematic, Environment, Tooling}

}
