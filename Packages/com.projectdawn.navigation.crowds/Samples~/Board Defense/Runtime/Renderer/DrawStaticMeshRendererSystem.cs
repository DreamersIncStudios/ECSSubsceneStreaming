using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

namespace ProjectDawn.Navigation.Sample.Crowd
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DrawStaticMeshRendererSystem : SystemBase
    {
        Matrix4x4[] m_Transforms = new Matrix4x4[1023];

        protected override void OnUpdate()
        {
            StaticMeshRenderer sharedRenderer = new();
            StaticMaterial shaderMaterial = new();
            int count = 0;
            Entities.ForEach((in StaticMeshRenderer renderer, in StaticMaterial material, in LocalTransform transform, in LocalToWorld localToWorld) =>
            {
                if (count == 1023 || sharedRenderer.Value != renderer.Value || shaderMaterial.Value != material.Value)
                {
                    if (count > 0)
                        Graphics.DrawMeshInstanced(sharedRenderer.Value, 0, shaderMaterial.Value, m_Transforms, count);

                    // Prepare for new batch
                    sharedRenderer = renderer;
                    shaderMaterial = material;
                    count = 0;
                }

                m_Transforms[count++] = localToWorld.Value;
            }).WithoutBurst().Run();

            if (count > 0)
                Graphics.DrawMeshInstanced(sharedRenderer.Value, 0, shaderMaterial.Value, m_Transforms, count);
        }
    }
}
