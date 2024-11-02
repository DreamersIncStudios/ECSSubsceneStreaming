using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    public struct Builder : IComponentData
    {
        public Entity ArcherTowerPrefab;
        public int ArcherTowerGoldCost;
        public Entity BarracksTowerPrefab;
        public int BarracksTowerGoldCost;
    }

    public class BuilderAuthoring : MonoBehaviour
    {
        public GameObject ArcherTowerPrefab;
        public int ArcherTowerGoldCost;
        public GameObject BarracksTowerPrefab;
        public int BarracksTowerGoldCost;

        internal class BuildingsAuthoringBaker : Baker<BuilderAuthoring>
        {
            public override void Bake(BuilderAuthoring authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Builder
                {
                    ArcherTowerPrefab = GetEntity(authoring.ArcherTowerPrefab, TransformUsageFlags.Dynamic),
                    ArcherTowerGoldCost = authoring.ArcherTowerGoldCost,
                    BarracksTowerPrefab = GetEntity(authoring.BarracksTowerPrefab, TransformUsageFlags.Dynamic),
                    BarracksTowerGoldCost = authoring.BarracksTowerGoldCost,
                });
            }
        }
    }
}
