using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Microsoft.CSharp;
using UnityEngine;

public class PlayerMovementController : ComponentSystem
{
    private Unity.Entities.EntityQuery Component_Query;
    public struct GraphData : Unity.Entities.IComponentData
    {
        public float Movement_Multiplier;
    }

    protected override void OnCreate()
    {
        Component_Query = GetEntityQuery(ComponentType.ReadWrite<Unity.Transforms.Translation>(), ComponentType.ReadOnly<PlayerInput>());
        EntityManager.CreateEntity(typeof (GraphData));
        SetSingleton(new GraphData{Movement_Multiplier = 0.1F});
    }

    protected override void OnUpdate()
    {
        GraphData graphData = GetSingleton<GraphData>();
        {
            Entities.With(Component_Query).ForEach((Unity.Entities.Entity Component_QueryEntity, ref Unity.Transforms.Translation Component_QueryTranslation, ref PlayerInput Component_QueryPlayerInput) =>
            {
                Debug.Log(("running movement controller" + Component_QueryEntity));
                Component_QueryTranslation.Value.x += (Component_QueryPlayerInput.HorizontalInput * graphData.Movement_Multiplier);
                Component_QueryTranslation.Value.y += 0F;
                Component_QueryTranslation.Value.z += (Component_QueryPlayerInput.VerticalInput * graphData.Movement_Multiplier);
            }

            );
        }
    }
}