using System;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;
using VisualScripting.Entities.Runtime;
using System.Collections.Generic;
[Serializable, ComponentEditor]
public struct TimedSelfDestruct : IComponentData
{
    public float Duration;
    [HideInInspector]
    public float TimeStarted;
}

[AddComponentMenu("Visual Scripting Components/TimedSelfDestruct")]
class TimedSelfDestructProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public float Duration;
    [HideInInspector]
    public float TimeStarted;

    public void Convert(Unity.Entities.Entity entity, Unity.Entities.EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TimedSelfDestruct { Duration = Duration, TimeStarted = TimeStarted });
    }

    public void DeclareReferencedPrefabs(List<UnityEngine.GameObject> referencedPrefabs)
    {
    }
}