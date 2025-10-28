using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;
using Unity.CharacterController;
using Unity.Burst.Intrinsics;
using Unity.NetCode;
using Unity.Physics.Extensions;
using Unity.Rendering;


[UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
[BurstCompile]
public partial struct FirstPersonCharacterPhysicsUpdateSystem : ISystem
{
    private EntityQuery _characterQuery;
    private FirstPersonCharacterUpdateContext _context;
    private KinematicCharacterUpdateContext _baseContext;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
            .WithAll<
                FirstPersonCharacterComponent,
                FirstPersonCharacterControl>()
            .Build(ref state);

        _context = new FirstPersonCharacterUpdateContext();
        _context.OnSystemCreate(ref state);
        _baseContext = new KinematicCharacterUpdateContext();
        _baseContext.OnSystemCreate(ref state);

        state.RequireForUpdate(_characterQuery);
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _context.OnSystemUpdate(ref state);
        _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

        FirstPersonCharacterPhysicsUpdateJob job = new FirstPersonCharacterPhysicsUpdateJob
        {
            Context = _context,
            BaseContext = _baseContext,
        };
        job.ScheduleParallel();
    }

    // [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct FirstPersonCharacterPhysicsUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public FirstPersonCharacterUpdateContext Context;
        public KinematicCharacterUpdateContext BaseContext;

        void Execute(FirstPersonCharacterAspect characterAspect)
        {
            characterAspect.PhysicsUpdate(ref Context, ref BaseContext);
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            BaseContext.EnsureCreationOfTmpCollections();
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
        }
    }
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FirstPersonPlayerVariableStepControlSystem))]
[BurstCompile]
public partial struct FirstPersonCharacterVariableUpdateSystem : ISystem
{
    private EntityQuery _characterQuery;
    private FirstPersonCharacterUpdateContext _context;
    private KinematicCharacterUpdateContext _baseContext;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
            .WithAll<
                FirstPersonCharacterComponent,
                FirstPersonCharacterControl>()
            .Build(ref state);

        _context = new FirstPersonCharacterUpdateContext();
        _context.OnSystemCreate(ref state);
        _baseContext = new KinematicCharacterUpdateContext();
        _baseContext.OnSystemCreate(ref state);

        state.RequireForUpdate(_characterQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _context.OnSystemUpdate(ref state);
        _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

        FirstPersonCharacterVariableUpdateJob variableUpdateJob = new FirstPersonCharacterVariableUpdateJob
        {
            Context = _context,
            BaseContext = _baseContext,
        };
        variableUpdateJob.ScheduleParallel();

        FirstPersonCharacterViewJob viewJob = new FirstPersonCharacterViewJob
        {
            FirstPersonCharacterLookup = SystemAPI.GetComponentLookup<FirstPersonCharacterComponent>(true),
        };
        viewJob.ScheduleParallel();
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct FirstPersonCharacterVariableUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public FirstPersonCharacterUpdateContext Context;
        public KinematicCharacterUpdateContext BaseContext;

        void Execute(FirstPersonCharacterAspect characterAspect)
        {
            characterAspect.VariableUpdate(ref Context, ref BaseContext);
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            BaseContext.EnsureCreationOfTmpCollections();
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        { }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct FirstPersonCharacterViewJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<FirstPersonCharacterComponent> FirstPersonCharacterLookup;

        void Execute(ref LocalTransform localTransform, in FirstPersonCharacterView characterView)
        {
            if (FirstPersonCharacterLookup.TryGetComponent(
                    characterView.CharacterEntity, 
                    out FirstPersonCharacterComponent character))
            {
                localTransform.Rotation = character.ViewLocalRotation;
            }
        }
    }
}


// [BurstCompile]
// [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// public partial struct HideLocalPlayerMeshSystem : ISystem
// {
//     public void OnUpdate(ref SystemState state)
//     {
//         if (!SystemAPI.TryGetSingletonEntity<NetworkId>(out var localNetworkEntity))
//             return;
//
//         var localNetworkId = SystemAPI.GetComponent<NetworkId>(localNetworkEntity).Value;
//         var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
//         
//         foreach (var (owner, entity) in 
//                  SystemAPI.Query<RefRO<GhostOwner>>().WithEntityAccess())
//         {
//             if (owner.ValueRO.NetworkId == localNetworkId)
//             {
//                 // Hide the mesh for the local player
//                 // if (SystemAPI.HasComponent<RenderMeshArray>(entity))
//                 if (true)
//                 {
//                     ecb.RemoveComponent<RenderMeshArray>(entity);
//                 }
//             }
//         }
//         
//         ecb.Playback(state.EntityManager);
//     }
// }


#region EnsureUniqueColliderForEachPlayer

public struct UniqueColliderTag : IComponentData { }

/// <summary>
/// This system simply makes sure all player physics colliders are unique.
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(FirstPersonCharacterPhysicsUpdateSystem))]
public partial struct EnsureUniqueSpawnedCharacterCollidersSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var entitiesToMakeUnique = new NativeList<Entity>(Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (colliderRef, entity) in SystemAPI.Query<RefRW<PhysicsCollider>>()
                     .WithAll<FirstPersonCharacterComponent>()
                     .WithNone<UniqueColliderTag>()
                     .WithEntityAccess())
        {
            // We can't call MakeUnique() yet
            entitiesToMakeUnique.Add(entity);
            ecb.AddComponent<UniqueColliderTag>(entity);
        }

        // Do all structural modifications now, after iteration
        foreach (var entity in entitiesToMakeUnique)
        {
            var colliderRef = state.EntityManager.GetComponentData<PhysicsCollider>(entity);
            if (!colliderRef.IsUnique)
            {
                colliderRef.MakeUnique(entity, state.EntityManager);
                state.EntityManager.SetComponentData(entity, colliderRef);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        entitiesToMakeUnique.Dispose();
    }
}

#endregion