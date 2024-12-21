using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;

namespace DreamersStudio.SubSceneStream
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial struct SceneSectionBakingSystem : ISystem
    {
        private const int quadrantYMultiplier = 1000;
        private const int quadrantCellSize = 100;
        
         static int GetPositionHashMapKey(float3 position)
        {
            return (int)(Mathf.Floor(position.x / quadrantCellSize) + (quadrantYMultiplier * Mathf.Floor(position.y / quadrantCellSize)));
        }

        public void Update(ref SystemState state)
        {

            foreach (var (transform, section, entity) in SystemAPI.Query<LocalTransform,SceneSection>().WithEntityAccess().WithAll<SceneSection>())
            {
                var guid = section.SceneGUID;
                state.EntityManager.SetSharedComponent( entity, new SceneSection(){SceneGUID  = guid,
                    Section = GetPositionHashMapKey(transform.Position)});
            }

        }
    }
}