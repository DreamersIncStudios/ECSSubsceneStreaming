using System;
using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation.Sample.Crowd
{
    public struct StaticMaterial : ISharedComponentData, System.IEquatable<StaticMaterial>
    {
        public Material Value;

        public bool Equals(StaticMaterial other)
        {
            if (Value == null)
                return false;
            return Value.GetHashCode() == other.Value.GetHashCode();
        }

        public override int GetHashCode()
        {
            if (Value == null)
                return 0;
            return Value.GetHashCode();
        }
    }

    [Obsolete("StaticMaterialAuthoring here is for sample purpose, please use regular MeshRenderer with `com.entities.graphics` package.")]
    public class StaticMaterialAuthoring : MonoBehaviour
    {
        public Material Material;
    }

    [Obsolete("StaticMaterialBaker here is for sample purpose, please use regular MeshRenderer with `com.entities.graphics` package.")]
    public class StaticMaterialBaker : Baker<StaticMaterialAuthoring>
    {
        public override void Bake(StaticMaterialAuthoring authoring)
        {
            AddSharedComponentManaged(GetEntity(TransformUsageFlags.Dynamic), new StaticMaterial
            {
                Value = authoring.Material
            });
        }
    }
}
