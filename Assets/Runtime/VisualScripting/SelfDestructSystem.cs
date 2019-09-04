using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Microsoft.CSharp;
using UnityEngine;

public class SelfDestructSystem : ComponentSystem
{
    private Unity.Entities.EntityQuery Component_QueryEnter;
    private Unity.Entities.EntityQuery Component_Query0;
    public struct Component_QueryTracking : Unity.Entities.ISystemStateComponentData
    {
    }

    protected override void OnCreate()
    {
        Component_QueryEnter = GetEntityQuery(ComponentType.Exclude<Component_QueryTracking>(), ComponentType.ReadWrite<TimedSelfDestruct>());
        Component_Query0 = GetEntityQuery(ComponentType.ReadWrite<Component_QueryTracking>(), ComponentType.ReadOnly<TimedSelfDestruct>());
    }

    protected override void OnUpdate()
    {
        {
            Entities.With(Component_QueryEnter).ForEach((Unity.Entities.Entity Component_QueryEntity, ref TimedSelfDestruct Component_QueryEnterTimedSelfDestruct) =>
            {
                Component_QueryEnterTimedSelfDestruct.TimeStarted = Time.time;
                PostUpdateCommands.AddComponent<Component_QueryTracking>(Component_QueryEntity, default (Component_QueryTracking));
            }

            );
        }

        {
            Entities.With(Component_Query0).ForEach((Unity.Entities.Entity Component_QueryEntity, ref TimedSelfDestruct Component_Query0TimedSelfDestruct) =>
            {
                if (((Time.time - Component_Query0TimedSelfDestruct.TimeStarted) > Component_Query0TimedSelfDestruct.Duration))
                {
                    PostUpdateCommands.DestroyEntity(Component_QueryEntity);
                }
            }

            );
        }
    }
}