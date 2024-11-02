using UnityEngine;
using Unity.Entities;
using System;

namespace ProjectDawn.Navigation.Sample.Crowd
{
    public struct StaticMeshRenderer : ISharedComponentData, System.IEquatable<StaticMeshRenderer>
    {
        public Mesh Value;

        public bool Equals(StaticMeshRenderer other)
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

    [Obsolete("StaticMeshRendererAuthoring here is for sample purpose, please use regular MeshRenderer with `com.entities.graphics` package.")]
    [ExecuteAlways]
    public class StaticMeshRendererAuthoring : MonoBehaviour
    {
        public Mesh Mesh;

        private void Update()
        {
            Material material = null;
            if (TryGetComponent(out StaticMaterialAuthoring staticMaterial))
            {
                material = staticMaterial.Material;
            }
            Graphics.DrawMesh(Mesh, transform.localToWorldMatrix, material, 0);
        }
    }

    [Obsolete("StaticMeshRendererBaker here is for sample purpose, please use regular MeshRenderer with `com.entities.graphics` package.")]
    public class StaticMeshRendererBaker : Baker<StaticMeshRendererAuthoring>
    {
        public override void Bake(StaticMeshRendererAuthoring authoring)
        {
            AddSharedComponentManaged(GetEntity(TransformUsageFlags.Dynamic), new StaticMeshRenderer
            {
                Value = authoring.Mesh,
            });
        }
    }


}
