using Unity.Entities;
using Unity.Mathematics;

// Данные самого излучателя
public struct ParticleEmitterData : IComponentData
{
    public Entity ParticlePrefab;
    public float SpawnRate;        // Частиц в секунду
    public float Timer;            // Внутренний таймер
    public float MinSpeed;
    public float MaxSpeed;
    public float Scale;
    public float SpreadAngle;      // Угол разброса в радианах
    public float MinParticleLifetime;
    public float MaxParticleLifetime;
    public uint RandomSeed;
}

// Время жизни и параметры масштабирования конкретной частицы
public struct ParticleLifetime : IComponentData
{
    public float RemainingTime;
    public float TotalTime;
}
