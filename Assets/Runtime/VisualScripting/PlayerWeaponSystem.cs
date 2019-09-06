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
        Component_Query = GetEntityQuery(ComponentType.ReadOnly<PlayerWeaponData>(), ComponentType.ReadOnly<PlayerInput>(), ComponentType.ReadOnly<Unity.Transforms.Translation>());
    }

    protected override void OnUpdate()
    {
        {
            Entities.With(Component_Query).ForEach((Unity.Entities.Entity Component_QueryEntity, ref PlayerWeaponData Component_QueryPlayerWeaponData, ref PlayerInput Component_QueryPlayerInput, ref Unity.Transforms.Translation Component_QueryTranslation) =>
            {
                Debug.Log("being run");
                if (Component_QueryPlayerInput.Fire)
                {
                    Debug.Log("attemping to spawn");
                    Unity.Entities.Entity entity = PostUpdateCommands.Instantiate(Component_QueryPlayerWeaponData.BulletType);
                    PostUpdateCommands.AddComponent<PlayerInput>(entity, new PlayerInput{HorizontalInput = 0F, VerticalInput = 0F, Fire = false});
                    PostUpdateCommands.SetComponent<Unity.Transforms.Translation>(entity, new Unity.Transforms.Translation{Value = new Unity.Mathematics.float3{x = Component_QueryTranslation.Value.x, y = Component_QueryTranslation.Value.y, z = Component_QueryTranslation.Value.z}});
                    PostUpdateCommands.AddComponent<SetBulletVelocity>(entity, new SetBulletVelocity{TargetEntity = Component_QueryEntity, TargetPosition = Component_QueryTranslation.Value});
                }
            }

            );
        }
    }
}