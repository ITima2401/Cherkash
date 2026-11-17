using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ParticleEmitterAuthoring : MonoBehaviour
{
    public GameObject ParticlePrefab;
    public float SpawnRate = 50f;
    public float MinSpeed = 5f;
    public float MaxSpeed = 10f;
    public float Scale = 0.2f;

    [Header("Разброс и время жизни")]
    [Range(0f, 180f)] public float SpreadDegrees = 30f;
    public float MinParticleLifetime = 1f;
    public float MaxParticleLifetime = 3f;

    class Baker : Baker<ParticleEmitterAuthoring>
    {
        public override void Bake(ParticleEmitterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ParticleEmitterData
            {
                ParticlePrefab = GetEntity(authoring.ParticlePrefab, TransformUsageFlags.Dynamic),
                SpawnRate = authoring.SpawnRate,
                Timer = 0f,
                MinSpeed = authoring.MinSpeed,
                MaxSpeed = authoring.MaxSpeed,
                Scale = authoring.Scale,
                SpreadAngle = math.radians(authoring.SpreadDegrees),
                MinParticleLifetime = authoring.MinParticleLifetime,
                MaxParticleLifetime = authoring.MaxParticleLifetime,
                RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue)
            });
        }
    }
}
