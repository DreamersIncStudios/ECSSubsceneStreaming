using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;

namespace AISenses.VisionSystems.Combat
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(TargetingSystem))]
    public partial class AttackTargetSystem : SystemBase
    {

        protected override void OnUpdate()
        {
            
            Entities.ForEach((ref AttackTarget attackTarget, ref DynamicBuffer<ScanPositionBuffer> buffer, ref Vision vision) =>
            {
               if(buffer.Length ==0)
                    return;
                if (attackTarget.IsTargeting && buffer.Length >= attackTarget.AttackTargetIndex) 
                {
                    attackTarget.AttackTargetLocation = buffer[attackTarget.AttackTargetIndex].target.LastKnownPosition;
                }
                else {
                    NativeArray<ScanPositionBuffer> scans = buffer.ToNativeArray(Allocator.Temp);
                    if (buffer.Length > 0)
                    {
                        //Attack in direction of point target
                        var visibleTargetInArea = buffer.ToNativeArray(Allocator.Temp);
                        visibleTargetInArea.Sort(new SortScanPositionByDistance());
                        if (vision.EngageRadius < visibleTargetInArea[0].dist)
                        {
                            attackTarget.AttackTargetLocation = visibleTargetInArea[0].target.LastKnownPosition;
                            attackTarget.TargetInRange = true;
                        }
                    }
                    else
                    {
                        attackTarget.TargetInRange = false;
                    }
                    scans.Dispose();
                }
            }).ScheduleParallel();
        }

    }
}