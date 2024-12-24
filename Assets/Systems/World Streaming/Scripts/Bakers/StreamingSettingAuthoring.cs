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


    public class LayerBaker : Baker<Transform> // Replace Transform with any GameObject that must be baked
    {

        private int GetSectionID(float3 position, int layer)
        {
            if(layer == 6)return 0;

            var quadrantCellSize = layer switch
            {
                26 => 300,
                27 => 150,
                28 => 75,
                _ => 300
            };
            var quadrantYMultiplier =  layer switch
            {
                26 => 10,
                27 => 100,
                28 => 1000,
                _ => 1000
            };
        
            return (int)(Mathf.FloorToInt(position.x / quadrantCellSize) 
                         + (quadrantYMultiplier * Mathf.Floor(position.z / quadrantCellSize)));
        }
        public override void Bake(Transform authoring)
        {
            if(!authoring.gameObject.isStatic)return;
            // Get the layer value from the GameObject
            int layer = authoring.gameObject.layer;

            // Attach it as a component to the ECS entity
       
            var entity = GetEntity(TransformUsageFlags.Renderable );
       
       
            AddSharedComponent(entity, new SceneSection(){
                SceneGUID = GetSceneGUID(),
                Section = GetSectionID(authoring.transform.position,layer)
            
            });
   
        }
    }
}