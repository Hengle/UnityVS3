using System;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;
using VisualScripting.Entities.Runtime;
using System.Collections.Generic;
[Serializable, ComponentEditor]
public struct PlayerInput : IComponentData
{
    [HideInInspector]
    public float HorizontalInput;
    [HideInInspector]
    public float VerticalInput;
    [HideInInspector]
    public bool Fire;
}

[AddComponentMenu("Visual Scripting Components/PlayerInput")]
class PlayerInputProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [HideInInspector]
    public float HorizontalInput;
    [HideInInspector]
    public float VerticalInput;
    [HideInInspector]
    public bool Fire;

    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PlayerInput { HorizontalInput = HorizontalInput, VerticalInput = VerticalInput, Fire = Fire });
    }

    public void DeclareReferencedPrefabs(List<UnityEngine.GameObject> referencedPrefabs)
    {
    }
}