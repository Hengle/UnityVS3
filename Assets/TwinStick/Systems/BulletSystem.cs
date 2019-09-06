using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public class BulletSystem : JobComponentSystem
{
    [BurstCompile]
    struct BulletSystemJob : IJobForEachWithEntity<SetBulletVelocity, PlayerInput>
    {
        public Vector3 mousePosition;

        public void Execute(Entity entity, int index, [ReadOnly] ref SetBulletVelocity bulletVelocityData, ref PlayerInput playerInput)
        {
            var targetTranslation = bulletVelocityData.TargetPosition;
            float2 angle = Vector3.Angle(bulletVelocityData.TargetPosition, mousePosition);
            playerInput.HorizontalInput = angle.x;
            playerInput.VerticalInput = angle.y;
            //entityManager.RemoveComponent(entity, typeof(SetBulletVelocity));
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new BulletSystemJob
        {
            mousePosition = Input.mousePosition        };
        return job.Schedule(this, inputDependencies);
    }

    protected override void OnStartRunning()
    {
        Debug.Log("OnStartRunning");
    }
}