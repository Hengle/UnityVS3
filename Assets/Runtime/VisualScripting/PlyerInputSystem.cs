using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Microsoft.CSharp;
using UnityEngine;

public class PlyerInputSystem : ComponentSystem
{
    private Unity.Entities.EntityQuery Component_Query;
    protected override void OnCreate()
    {
        Component_Query = GetEntityQuery(ComponentType.ReadWrite<PlayerInput>(), ComponentType.ReadOnly<PlayerTag>());
    }

    protected override void OnUpdate()
    {
        {
            Entities.With(Component_Query).ForEach((Unity.Entities.Entity Component_QueryEntity, ref PlayerInput Component_QueryPlayerInput) =>
            {
                Component_QueryPlayerInput.HorizontalInput = UnityEngine.Input.GetAxis("Horizontal");
                Component_QueryPlayerInput.Fire = UnityEngine.Input.GetButton("Fire1");
                Component_QueryPlayerInput.VerticalInput = UnityEngine.Input.GetAxis("Vertical");
            }

            );
        }
    }
}