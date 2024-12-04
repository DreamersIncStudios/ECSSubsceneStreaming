using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamersStudio.SubSceneStream
{

    public class SteamingSettingAuthoring : MonoBehaviour
    {

        public int LargeGridSize, MediumGridSize, SmallGridSize;

        class Baker : Baker<SteamingSettingAuthoring>
        {
            public override void Bake(SteamingSettingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new StreamingSettings(authoring));
                
            }
        }
    }
}