using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Microsoft.CSharp;
using UnityEngine;

public class PlayerWeaponSystem : ComponentSystem
{
    private Unity.Entities.EntityQuery Component_Query;
    protected override void OnCreate()
    {
        Component_Query = GetEntityQuery(ComponentType.ReadOnly<PlayerWeaponData>(), ComponentType.ReadOnly<PlayerInput>());
    }

    protected override void OnUpdate()
    {
        {
            Entities.With(Component_Query).ForEach((Unity.Entities.Entity Component_QueryEntity, ref PlayerWeaponData Component_QueryPlayerWeaponData, ref PlayerInput Component_QueryPlayerInput) =>
            {
                if (Component_QueryPlayerInput.Fire)
                {
                    PostUpdateCommands.Instantiate(Component_QueryPlayerWeaponData.BulletType);
                }
            }

            );
        }
    }
}