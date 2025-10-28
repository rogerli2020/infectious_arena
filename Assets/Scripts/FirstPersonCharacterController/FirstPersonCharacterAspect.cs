#pragma warning disable CS0618

using PlayerMovement;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.CharacterController;
using UnityEngine;
using Collider = Unity.Physics.Collider;

public struct FirstPersonCharacterUpdateContext
{
    // Here, you may add additional global data for your character updates, such as ComponentLookups, Singletons, NativeCollections, etc...
    // The data you add here will be accessible in your character updates and all of your character "callbacks".

    public void OnSystemCreate(ref SystemState state)
    {
        // Get lookups
    }

    public void OnSystemUpdate(ref SystemState state)
    {
        // Update lookups
    }
}

public readonly partial struct FirstPersonCharacterAspect : IAspect, IKinematicCharacterProcessor<FirstPersonCharacterUpdateContext>
{
    public readonly KinematicCharacterAspect CharacterAspect;
    public readonly RefRW<FirstPersonCharacterComponent> CharacterComponent;
    public readonly RefRW<FirstPersonCharacterControl> CharacterControl;
    public readonly RefRW<FirstPersonCharacterHeight> CapsuleHeight;
    
    public void PhysicsUpdate(ref FirstPersonCharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref FirstPersonCharacterComponent characterComponent = ref CharacterComponent.ValueRW;
        ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
        ref float3 characterPosition = ref CharacterAspect.LocalTransform.ValueRW.Position;
        ref FirstPersonCharacterHeight characterCrouch = ref CapsuleHeight.ValueRW;
        ref PhysicsCollider characterCollider = ref CharacterAspect.PhysicsCollider.ValueRW;
        
        // First phase of default character update
        CharacterAspect.Update_Initialize(in this, ref context, ref baseContext, ref characterBody, baseContext.Time.DeltaTime);
        CharacterAspect.Update_ParentMovement(in this, ref context, ref baseContext, ref characterBody, ref characterPosition, characterBody.WasGroundedBeforeCharacterUpdate);
        CharacterAspect.Update_Grounding(in this, ref context, ref baseContext, ref characterBody, ref characterPosition);
        
        // update height based on whether player is standing/crouching/recovering
        #region CrouchMechanics
        
        // if crouch key is down...
        if (CharacterControl.ValueRO.Crouch)
        {
            // if not already crouching, simply crouch
            if (characterCrouch.CrouchState != CrouchState.Crouching)
            {
                characterCrouch.DesiredHeight = 0.9f;
                characterCrouch.CrouchState = CrouchState.Crouching;
            }

        }
        
        // if crouch key is not pressed...
        else
        {
            // if currently crouching, start recovering
            if (characterCrouch.CrouchState == CrouchState.Crouching)
                characterCrouch.CrouchState = CrouchState.WishStanding;
            
            // if already recovering, continue to recover
            if (characterCrouch.CrouchState == CrouchState.WishStanding)
            {
                bool cantStandUp;
                
                
                #region CheckIfCanStandUp
                unsafe
                {
                    var filter = new CollisionFilter
                    {
                        BelongsTo   = 1u << (int)ColliderLayers.CharacterCrouchStateDetectionColliders,
                        CollidesWith= ~(1u << (int)ColliderLayers.CharacterMovementColliders),
                    };
                    var capsuleGeometry = new CapsuleGeometry
                    {
                        Radius = 0.45f,
                        Vertex0 = new float3(0f, 0.55f, 0f),
                        Vertex1 = new float3(0f, 1.45f, 0f),
                    };
                    var collider = Unity.Physics.CapsuleCollider.Create(capsuleGeometry, filter);
                    float3 checkPosition = characterPosition;
                    checkPosition.y += (float)((1.8 - characterCrouch.CurrentHeight) / 2f);
                    var transform = new RigidTransform(quaternion.identity, checkPosition);
                    var input = new ColliderDistanceInput
                    {
                        Collider = (Collider*)collider.GetUnsafePtr(),
                        Transform = transform,
                        MaxDistance = 0f
                    };
                    cantStandUp = baseContext.PhysicsWorld.CollisionWorld.CalculateDistance(input, out DistanceHit hit);
                    collider.Dispose();
                }
                #endregion
                
                // if you have space to stand up...
                if (!cantStandUp)
                {
                    // change to standing height instantly and interpolate camera view position instead?
                    characterCrouch.DesiredHeight = 1.8f;
                    characterCrouch.CrouchState = CrouchState.Standing;
                    
                    // // old implementation with gradual height recovery
                    // float heightToRecover = 1.8f - characterCrouch.CurrentHeight;
                    // if (heightToRecover > 0.01f)
                    // {
                    //     float heightToRecoverThisTick = 
                    //         (heightToRecover <= 0.01f)
                    //             ? heightToRecover
                    //             : (heightToRecover / 2f) * (baseContext.Time.DeltaTime / .05f);
                    //     characterCrouch.DesiredHeight += heightToRecoverThisTick;
                    // }
                    // else
                    // {
                    //     characterCrouch.DesiredHeight = 1.8f;
                    //     characterCrouch.CrouchState = CrouchState.Standing;
                    // }
                }
            }
            
        }
        
        #endregion
        
        // apply change in height
        #region ApplyUpdatedHeight

        if (characterCollider.IsUnique)
        {
            float heightDelta = characterCrouch.DesiredHeight - characterCrouch.CurrentHeight;
            if (math.abs(heightDelta) > 0.0001f)
            {
                // Collision filter
                var filter = new CollisionFilter
                {
                    BelongsTo = 1u << (int)ColliderLayers.CharacterMovementColliders,
                    CollidesWith = ~(1u << (int)ColliderLayers.CharacterCrouchStateDetectionColliders),
                };

                // Capsule parameters
                float vertexOffsetLength = (characterCrouch.DesiredHeight - 0.9f) * 0.5f;
                float3 vertexOffset = new float3(0f, vertexOffsetLength, 0f);
                float3 center = new float3(0f, 1f, 0f);

                // Modify collider geometry
                unsafe
                {
                    Unity.Physics.CapsuleCollider* capsuleCollider =
                        (Unity.Physics.CapsuleCollider*)characterCollider.ColliderPtr;
                    CapsuleGeometry capsuleGeometry = capsuleCollider->Geometry;
                    capsuleGeometry.Vertex0 = center - vertexOffset;
                    capsuleGeometry.Vertex1 = center + vertexOffset;
                    capsuleCollider->Geometry = capsuleGeometry;
                    capsuleCollider->SetCollisionFilter(filter);
                }

                // Update character position for standing up
                if (heightDelta > 0f)
                {
                    CharacterAspect.LocalTransform.ValueRW.Position.y += heightDelta * 0.5f;
                }

                // Apply new height
                characterCrouch.CurrentHeight = characterCrouch.DesiredHeight;
            }
        }

        #endregion
        
        // TODO Detect Ladder Surface Hits
        // Debug.Log(CharacterAspect.StatefulHitsBuffer.Length);
        
        // Update desired character velocity after grounding was detected, but before doing additional processing that depends on velocity
        MyHandleVelocityControl(ref context, ref baseContext, ref characterCrouch);
        
        // Second phase of default character update
        CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in this, ref context, ref baseContext, ref characterBody, in characterComponent.StepAndSlopeHandling);
        CharacterAspect.Update_GroundPushing(in this, ref context, ref baseContext, characterComponent.Gravity);
        CharacterAspect.Update_MovementAndDecollisions(in this, ref context, ref baseContext, ref characterBody, ref characterPosition);
        CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterBody);
        CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterBody);
        CharacterAspect.Update_ProcessStatefulCharacterHits();
    }

    private void MyHandleVelocityControl(ref FirstPersonCharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, ref FirstPersonCharacterHeight characterCrouch)
    {
        float deltaTime = baseContext.Time.DeltaTime;
        ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
        ref FirstPersonCharacterComponent characterComponent = ref CharacterComponent.ValueRW;
        ref FirstPersonCharacterControl characterControl = ref CharacterControl.ValueRW;
    
        // Rotate move input and velocity to take into account parent rotation
        if (characterBody.ParentEntity != Entity.Null)
        {
            characterControl.MoveVector = math.rotate(characterBody.RotationFromParent, characterControl.MoveVector);
            characterBody.RelativeVelocity = math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
        }

        // scale move speed by height so you're slower when crouching/recovering
        float moveSpeed = characterComponent.moveSpeed;
        moveSpeed = (characterCrouch.CrouchState != CrouchState.Standing)
            ? moveSpeed * (characterCrouch.CurrentHeight / 1.8f) : moveSpeed;
        float3 targetVelocity = characterControl.MoveVector * moveSpeed;
        
        bool isGrounded = characterBody.IsGrounded;
        
        // check if sliding
        bool isSliding = false;
        float3 up = new float3(0, 1, 0);
        float cosTheta = math.dot(characterBody.GroundHit.Normal, up);
        float angleRadians = math.acos(math.clamp(cosTheta, -1f, 1f));
        float angleDegrees = math.degrees(angleRadians);

        if (angleDegrees > 45f && characterBody.IsGrounded)
        {
            isSliding = true;
            isGrounded = false;
        }
        
        // gravity
        if (!isGrounded)
            CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity,
                characterComponent.Gravity, deltaTime);
        
        // handle jump
        if (characterControl.Jump)
        {
            if (isGrounded)
                CharacterControlUtilities.StandardJump(ref characterBody, 
                    characterBody.GroundingUp * characterComponent.jumpVelocity, 
                    true, characterBody.GroundingUp);
        }
        
        EntitiesPlayerMovementUtil.UpdateVelocityTemp
        (
            ref characterBody.RelativeVelocity, targetVelocity,
            ref characterControl, isGrounded, deltaTime,
            characterComponent.groundAccel,
            characterComponent.airAccel,
            characterComponent.friction,
            characterComponent.gravity,
            characterComponent.jumpVelocity,
            characterComponent.stopSpeed,
            characterComponent.moveSpeed,
            characterComponent.maxSpeed,
            isSliding,
            characterBody.GroundHit.Normal
        );
    }
    
    public void VariableUpdate(ref FirstPersonCharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
        ref FirstPersonCharacterComponent characterComponent = ref CharacterComponent.ValueRW;
        ref FirstPersonCharacterControl characterControl = ref CharacterControl.ValueRW;
        ref quaternion characterRotation = ref CharacterAspect.LocalTransform.ValueRW.Rotation;

        // Add rotation from parent body to the character rotation
        // (this is for allowing a rotating moving platform to rotate your character as well, and handle interpolation properly)
        KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(ref characterRotation, characterBody.RotationFromParent, baseContext.Time.DeltaTime, characterBody.LastPhysicsUpdateDeltaTime);

        // Compute character & view rotations from rotation input
        FirstPersonCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
            ref characterRotation,
            ref characterComponent.ViewPitchDegrees,
            characterControl.LookDegreesDelta,
            0f,
            characterComponent.MinViewAngle,
            characterComponent.MaxViewAngle,
            out float canceledPitchDegrees,
            out characterComponent.ViewLocalRotation);
    }
    
    // // DEFAULT OFFICIAL VELOCITY IMPLEMENTATION
    // private void HandleVelocityControl(ref FirstPersonCharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    // {
    //     float deltaTime = baseContext.Time.DeltaTime;
    //     ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
    //     ref FirstPersonCharacterComponent characterComponent = ref CharacterComponent.ValueRW;
    //     ref FirstPersonCharacterControl characterControl = ref CharacterControl.ValueRW;
    //
    //     // Rotate move input and velocity to take into account parent rotation
    //     if (characterBody.ParentEntity != Entity.Null)
    //     {
    //         characterControl.MoveVector = math.rotate(characterBody.RotationFromParent, characterControl.MoveVector);
    //         characterBody.RelativeVelocity = math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
    //     }
    //
    //     if (characterBody.IsGrounded)
    //     {
    //         // Move on ground
    //         float3 targetVelocity = characterControl.MoveVector * characterComponent.GroundMaxSpeed;
    //         CharacterControlUtilities.StandardGroundMove_Interpolated(ref characterBody.RelativeVelocity, targetVelocity, characterComponent.GroundedMovementSharpness, deltaTime, characterBody.GroundingUp, characterBody.GroundHit.Normal);
    //         
    //         // Jump
    //         if (characterControl.Jump)
    //         {
    //             CharacterControlUtilities.StandardJump(ref characterBody, characterBody.GroundingUp * characterComponent.JumpSpeed, true, characterBody.GroundingUp);
    //         }
    //     }
    //     else
    //     {
    //         // Move in air
    //         float3 airAcceleration = characterControl.MoveVector * characterComponent.AirAcceleration;
    //         if (math.lengthsq(airAcceleration) > 0f)
    //         {
    //             float3 tmpVelocity = characterBody.RelativeVelocity;
    //             CharacterControlUtilities.StandardAirMove(ref characterBody.RelativeVelocity, airAcceleration, characterComponent.AirMaxSpeed, characterBody.GroundingUp, deltaTime, false);
    //         
    //             // Cancel air acceleration from input if we would hit a non-grounded surface (prevents air-climbing slopes at high air accelerations)
    //             if (characterComponent.PreventAirAccelerationAgainstUngroundedHits && CharacterAspect.MovementWouldHitNonGroundedObstruction(in this, ref context, ref baseContext, characterBody.RelativeVelocity * deltaTime, out ColliderCastHit hit))
    //             {
    //                 characterBody.RelativeVelocity = tmpVelocity;
    //             }
    //         }
    //
    //         // Gravity
    //         CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity, characterComponent.Gravity, deltaTime);
    //
    //         // Drag
    //         CharacterControlUtilities.ApplyDragToVelocity(ref characterBody.RelativeVelocity, deltaTime, characterComponent.AirDrag);
    //     }
    // }

    #region Character Processor Callbacks
    public void UpdateGroundingUp(
        ref FirstPersonCharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;

        CharacterAspect.Default_UpdateGroundingUp(ref characterBody);
    }

    public bool CanCollideWithHit(
        ref FirstPersonCharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext,
        in BasicHit hit)
    {
        return PhysicsUtilities.IsCollidable(hit.Material);
    }

    public bool IsGroundedOnHit(
        ref FirstPersonCharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext,
        in BasicHit hit,
        int groundingEvaluationType)
    {
        FirstPersonCharacterComponent characterComponent = CharacterComponent.ValueRO;

        return CharacterAspect.Default_IsGroundedOnHit(
            in this,
            ref context,
            ref baseContext,
            in hit,
            in characterComponent.StepAndSlopeHandling,
            groundingEvaluationType);
    }

    public void OnMovementHit(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref KinematicCharacterHit hit,
            ref float3 remainingMovementDirection,
            ref float remainingMovementLength,
            float3 originalVelocityDirection,
            float hitDistance)
    {
        ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
        ref float3 characterPosition = ref CharacterAspect.LocalTransform.ValueRW.Position;
        FirstPersonCharacterComponent characterComponent = CharacterComponent.ValueRO;

        CharacterAspect.Default_OnMovementHit(
            in this,
            ref context,
            ref baseContext,
            ref characterBody,
            ref characterPosition,
            ref hit,
            ref remainingMovementDirection,
            ref remainingMovementLength,
            originalVelocityDirection,
            hitDistance,
            characterComponent.StepAndSlopeHandling.StepHandling,
            characterComponent.StepAndSlopeHandling.MaxStepHeight,
            characterComponent.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck);
    }

    public void OverrideDynamicHitMasses(
        ref FirstPersonCharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext,
        ref PhysicsMass characterMass,
        ref PhysicsMass otherMass,
        BasicHit hit)
    {
        // Custom mass overrides
    }

    public void ProjectVelocityOnHits(
        ref FirstPersonCharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext,
        ref float3 velocity,
        ref bool characterIsGrounded,
        ref BasicHit characterGroundHit,
        in DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits,
        float3 originalVelocityDirection)
    {
        FirstPersonCharacterComponent characterComponent = CharacterComponent.ValueRO;

        CharacterAspect.Default_ProjectVelocityOnHits(
            ref velocity,
            ref characterIsGrounded,
            ref characterGroundHit,
            in velocityProjectionHits,
            originalVelocityDirection,
            characterComponent.StepAndSlopeHandling.ConstrainVelocityToGroundPlane);
    }
    #endregion
}
