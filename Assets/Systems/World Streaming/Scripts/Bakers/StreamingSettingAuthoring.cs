using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamersStudio.SubSceneStream
{

    public class StreamingSettingAuthoring : MonoBehaviour
    {

        private const int quadrantYMultiplier = 1000;
        public int layer => gameObject.layer;

        private int GetPositionHashMapKey(float3 position)
        {
            int quadrantCellSize = gameObject.layer switch
            {
                26 => 50,
                27 => 100,
                28 => 250,
                _ => 100
            };
            return (int)(Mathf.Floor(position.x / quadrantCellSize) 
                         + (quadrantYMultiplier * Mathf.Floor(position.z / quadrantCellSize)));
        }
        public Vector3 Position => transform.position;
        [SerializeField]public int Section=> GetPositionHashMapKey(Position);
        class Baker : Baker<StreamingSettingAuthoring>
        {
            public override void Bake(StreamingSettingAuthoring authoring)
            {
                // Adding a section doesn't require any transform usage flags
                var entity = GetEntity(TransformUsageFlags.None);
                AddSharedComponent(entity, new SceneSection { SceneGUID = GetSceneGUID(),
                    Section =  authoring.GetPositionHashMapKey(authoring.Position)
                    
                });;
            }
        }
    }
}