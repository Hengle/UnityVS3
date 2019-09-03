using System;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;
using VisualScripting.Entities.Runtime;
using System.Collections.Generic;
[Serializable, ComponentEditor]
public struct PlayerWeaponData : IComponentData
{
    public Unity.Entities.Entity BulletType;
    public Unity.Entities.Entity BulletTypePrefab;
}

[AddComponentMenu("Visual Scripting Components/PlayerWeaponData")]
class PlayerWeaponDataProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public Unity.Entities.Entity BulletType;
    public UnityEngine.GameObject BulletTypePrefab;

    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PlayerWeaponData { BulletType = BulletType, BulletTypePrefab = conversionSystem.GetPrimaryEntity(BulletTypePrefab) });
    }

    public void DeclareReferencedPrefabs(List<UnityEngine.GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(BulletTypePrefab);
    }
}