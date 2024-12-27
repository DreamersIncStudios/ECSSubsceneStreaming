using System.Collections.Generic;
using Sirenix.Utilities;
using Streaming.SceneManagement.SectionMetadata;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial class SubSceneBakingSystem : SystemBase
{
    private Dictionary<Hash128, List<SectionData>> sectionDataDictionary;

    private int GetSectionID(float3 position, int layer)
    {
        if (layer == 6) return 0;

        var quadrantCellSize = layer switch
        {
            26 => 300,
            27 => 150,
            28 => 75,
            _ => 600
        };
        var quadrantYMultiplier = layer switch
        {
            28 => 10,
            27 => 100,
            26 => 1000,
            _ => 2000
        };

        return (int)(Mathf.FloorToInt(position.x / quadrantCellSize)
                     + (quadrantYMultiplier * Mathf.Floor(position.z / quadrantCellSize)));
    }

    protected override void OnCreate()
    {
        sectionDataDictionary = new Dictionary<Hash128, List<SectionData>>();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        Entities.WithoutBurst().ForEach(
            (Entity entity, SceneSection section, in LocalToWorld transform, in RenderFilterSettings settings) =>
            {
                if (sectionDataDictionary.TryGetValue(section.SceneGUID , out var sectionData))
                {
                    if (TryUpdateExistingSection(sectionData, settings, transform, entity, ecb, section.SceneGUID))
                        return;

                    AddNewSection(sectionData, settings, transform, entity, ecb, section.SceneGUID);
                }
                else
                {
                    AddNewSceneSection(settings, transform, entity, ecb, section.SceneGUID);
                }
            }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
        
     //  CleanupBakingComponents();
    }

    private bool TryUpdateExistingSection(List<SectionData> sectionData, RenderFilterSettings settings,
        LocalToWorld transform, Entity entity, EntityCommandBuffer ecb, Hash128 sceneGuid)
    {
        var positionHashKey = GetSectionID(transform.Position, settings.Layer);

        foreach (var data in sectionData)
        {
            if (data.Layer != settings.Layer || data.PositionHashKey != positionHashKey) continue;
            ecb.AddSharedComponent(entity, new SceneSection
            {
                SceneGUID = sceneGuid,
                Section = data.Section
            });
            return true;
        }

        return false;
    }

    private void AddNewSection(List<SectionData> sectionData, RenderFilterSettings settings,
        LocalToWorld transform, Entity entity, EntityCommandBuffer ecb, Hash128 sceneGuid)
    {
        var positionHashKey = GetSectionID(transform.Position, settings.Layer);
        var newSectionCount = sectionData.IsNullOrEmpty() ? 1: sectionData.Count + 1;

        sectionData.Add(new SectionData
        {
            Layer = settings.Layer,
            PositionHashKey = positionHashKey,
            Section = newSectionCount
        });

        ecb.AddSharedComponent(entity, new SceneSection
        {
            SceneGUID = sceneGuid,
            Section = newSectionCount
        });
        ecb.AddComponent(entity, new Circle()
        {
            Radius = settings.Layer switch
            {
                6 => 750,
                26 => 250,
                27 => 100,
                28 => 75,
                _ => 100
            },
            Center = GetSectionCenter(positionHashKey, settings.Layer)
        });
    }

    private void AddNewSceneSection(RenderFilterSettings settings, LocalToWorld transform, Entity entity,
        EntityCommandBuffer ecb, Hash128 sceneGuid)
    {
        var sectionData = new List<SectionData>();
        sectionDataDictionary.Add(sceneGuid, sectionData);

        AddNewSection(sectionData, settings, transform, entity, ecb, sceneGuid);
    }
    
    private float3 GetSectionCenter(int sectionID, int layer)
    {
        if (layer == 6)
            return new float3(0, 0, 0);

        var quadrantCellSize = layer switch
        {
            26 => 300,
            27 => 150,
            28 => 75,
            _ => 600
        };
        var quadrantYMultiplier = layer switch
        {
            28 => 10,
            27 => 100,
            26 => 1000,
            _ => 2000
        };

        // Reverse the ID calculation to find the center
        var xIndex = sectionID % quadrantYMultiplier;
        var zIndex = sectionID / quadrantYMultiplier;

        // Compute center position
        var centerX = (xIndex + 0.5f) * quadrantCellSize;
        var centerZ = (zIndex + 0.5f) * quadrantCellSize;

        return new float3(centerX, 0, centerZ); // Assuming Y is 0 for the center height
    }
}
    


public struct SectionData 
{
    public int Layer;
    public int PositionHashKey;
    public int Section;
    
}
