using System;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;
using VisualScripting.Entities.Runtime;
using System.Collections.Generic;
[Serializable, ComponentEditor]
public struct SetBulletVelocity : IComponentData
{
    public Unity.Entities.Entity TargetEntity;
    public Unity.Mathematics.float3 TargetPosition;
}

[AddComponentMenu("Visual Scripting Components/SetBulletVelocity")]
class SetBulletVelocityProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public Unity.Entities.Entity TargetEntity;
    public Unity.Mathematics.float3 TargetPosition;

    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SetBulletVelocity { TargetEntity = TargetEntity, TargetPosition = TargetPosition });
    }

    public void DeclareReferencedPrefabs(List<UnityEngine.GameObject> referencedPrefabs)
    {
    }
}