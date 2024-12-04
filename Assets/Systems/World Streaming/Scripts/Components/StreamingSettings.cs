using Unity.Entities;
using Unity.Mathematics;


namespace DreamersStudio.SubSceneStream
{

    public struct StreamingSettings : IComponentData
    {
        public int LargeGridSize, MediumGridSize, SmallGridSize;

        public StreamingSettings(SteamingSettingAuthoring authoring)
        {
            LargeGridSize = authoring.LargeGridSize;
            MediumGridSize = authoring.MediumGridSize;
            SmallGridSize = authoring.SmallGridSize;
        }
    }
}