using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

// Система генерации частиц
[BurstCompile]
public partial struct ParticleEmitterSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (emitter, transform) in
                 SystemAPI.Query<RefRW<ParticleEmitterData>, RefRO<LocalToWorld>>())
        {
            if (emitter.ValueRO.SpawnRate <= 0f || emitter.ValueRO.ParticlePrefab == Entity.Null)
                continue;

            // Гарантируем, что коллайдер префаба игнорирует другие частицы (GroupIndex = -1)
            if (SystemAPI.HasComponent<PhysicsCollider>(emitter.ValueRO.ParticlePrefab))
            {
                var prefabCollider = SystemAPI.GetComponent<PhysicsCollider>(emitter.ValueRO.ParticlePrefab);
                var filter = prefabCollider.Value.Value.GetCollisionFilter();
                if (filter.GroupIndex != -1)
                {
                    filter.GroupIndex = -1;
                    prefabCollider.Value.Value.SetCollisionFilter(filter);
                }
            }

            emitter.ValueRW.Timer += dt;
            float interval = 1f / emitter.ValueRO.SpawnRate;

            var random = Unity.Mathematics.Random.CreateFromIndex(emitter.ValueRW.RandomSeed++);

            while (emitter.ValueRW.Timer >= interval)
            {
                emitter.ValueRW.Timer -= interval;

                Entity particle = ecb.Instantiate(emitter.ValueRO.ParticlePrefab);

                // Случайное направление, скорость и начальный размер
                float3 forward = transform.ValueRO.Forward;
                float3 randomDir = GetRandomDirectionInCone(ref random, forward, emitter.ValueRO.SpreadAngle);
                float speed = random.NextFloat(emitter.ValueRO.MinSpeed, emitter.ValueRO.MaxSpeed);
                float Scale = emitter.ValueRO.Scale;
                float ParticalLifetime = random.NextFloat(emitter.ValueRO.MinParticleLifetime, emitter.ValueRO.MaxParticleLifetime);
                // Позиция, поворот и начальный масштаб (LocalTransform.Scale масштабирует и меш, и коллайдер)
                ecb.SetComponent(particle, LocalTransform.FromPositionRotationScale(
                    transform.ValueRO.Position,
                    quaternion.LookRotationSafe(randomDir, math.up()),
                    Scale
                ));

                // Начальная физическая скорость
                ecb.SetComponent(particle, new PhysicsVelocity
                {
                    Linear = randomDir * speed,
                    Angular = random.NextFloat3(-5f, 5f)
                });

                // Данные времени жизни и целевого размера
                ecb.AddComponent(particle, new ParticleLifetime
                {
                    RemainingTime = ParticalLifetime,
                    TotalTime = ParticalLifetime,
                });
            }
        }
    }

    private static float3 GetRandomDirectionInCone(ref Unity.Mathematics.Random random, float3 forward, float maxAngle)
    {
        float cosAngle = math.cos(maxAngle);
        float z = random.NextFloat(cosAngle, 1f);
        float phi = random.NextFloat(0f, 2f * math.PI);
        float sinTheta = math.sqrt(1f - z * z);

        float3 localDir = new float3(sinTheta * math.cos(phi), sinTheta * math.sin(phi), z);
        quaternion rot = quaternion.LookRotationSafe(forward, math.up());
        return math.mul(rot, localDir);
    }
}

// Система удаления отживших частиц
[BurstCompile]
public partial struct ParticleLifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (lifetime, transform, entity) in
                 SystemAPI.Query<RefRW<ParticleLifetime>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            lifetime.ValueRW.RemainingTime -= dt;

            if (lifetime.ValueRO.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }
        }
    }
}
