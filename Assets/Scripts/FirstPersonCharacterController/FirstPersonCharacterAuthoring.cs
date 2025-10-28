using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Unity.CharacterController;
using Unity.Physics;
using System.Collections.Generic;
using UnityEngine;
using CapsuleCollider = UnityEngine.CapsuleCollider;


[DisallowMultipleComponent]
public class FirstPersonCharacterAuthoring : MonoBehaviour
{
    public GameObject ViewEntity;
    public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();

    public float GroundMaxSpeed = 10f;
    public float GroundedMovementSharpness = 15f;
    public float AirAcceleration = 50f;
    public float AirMaxSpeed = 10f;
    public float AirDrag = 0f;
    public float JumpSpeed = 10f;
    public float3 Gravity = math.up() * -30f;
    public bool PreventAirAccelerationAgainstUngroundedHits = true;
    
    public float gravity = 30f;            
    public float stopSpeed = 2f;          
    public float maxSpeed = 10f;      
    public float groundAccel = 4f;
    public float airAccel = 0.8f;
    public float slideAccel = 0.75f;
    public float friction = 4f;
    public float maxVelocity = 75f;
    public float moveSpeed = 7.5f;
    public float jumpVelocity = 8f;
    
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault();
    public float MinViewAngle = -90f;
    public float MaxViewAngle = 90f;
    
    public class Baker : Baker<FirstPersonCharacterAuthoring>
    {
        public override void Bake(FirstPersonCharacterAuthoring authoring)
        {
            KinematicCharacterUtilities.BakeCharacter(this, authoring.gameObject, authoring.CharacterProperties);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

            AddComponent(entity, new FirstPersonCharacterComponent
            {
                GroundMaxSpeed = authoring.GroundMaxSpeed,
                GroundedMovementSharpness = authoring.GroundedMovementSharpness,
                AirAcceleration = authoring.AirAcceleration,
                AirMaxSpeed = authoring.AirMaxSpeed,
                AirDrag = authoring.AirDrag,
                JumpSpeed = authoring.JumpSpeed,
                Gravity = authoring.Gravity,
                PreventAirAccelerationAgainstUngroundedHits = authoring.PreventAirAccelerationAgainstUngroundedHits,
                
                // my movement stats
                gravity = authoring.gravity,            
                stopSpeed = authoring.stopSpeed,
                maxSpeed = authoring.maxSpeed,
                groundAccel = authoring.groundAccel,
                airAccel = authoring.airAccel,
                slideAccel = authoring.slideAccel,
                friction = authoring.friction,
                maxVelocity = authoring.maxVelocity,
                moveSpeed = authoring.moveSpeed,
                jumpVelocity = authoring.jumpVelocity,
                
                StepAndSlopeHandling = authoring.StepAndSlopeHandling,
                MinViewAngle = authoring.MinViewAngle,
                MaxViewAngle = authoring.MaxViewAngle,
                
                ViewEntity = GetEntity(authoring.ViewEntity, TransformUsageFlags.Dynamic),
                ViewPitchDegrees = 0f,
                ViewLocalRotation = quaternion.identity,
            });
            
            // input
            AddComponent(entity, new FirstPersonCharacterControl());
            
            // crouching
            AddComponent(entity, new FirstPersonCharacterHeight()
            { 
                CrouchState = CrouchState.Standing,
                CurrentHeight = 1.8f,
                DesiredHeight = 1.8f,
            });
        }
    }
}
