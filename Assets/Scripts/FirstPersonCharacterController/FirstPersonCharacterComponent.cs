using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;
using Unity.NetCode;

[Serializable]
[GhostComponent]
public struct FirstPersonCharacterComponent : IComponentData
{
    public float GroundMaxSpeed;
    public float GroundedMovementSharpness;
    public float AirAcceleration;
    public float AirMaxSpeed;
    public float AirDrag;
    public float JumpSpeed;
    public float3 Gravity;
    public bool PreventAirAccelerationAgainstUngroundedHits;
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;

    public float MinViewAngle;
    public float MaxViewAngle;

    public Entity ViewEntity;
    [GhostField]
    public float ViewPitchDegrees;
    public quaternion ViewLocalRotation;
    
    public float gravity;            
    public float stopSpeed;          
    public float maxSpeed;      
    public float groundAccel;
    public float airAccel;
    public float slideAccel;
    public float friction;
    public float maxVelocity;
    public float moveSpeed;
    public float jumpVelocity;
}

[Serializable]
public struct FirstPersonCharacterControl : IComponentData
{
    public float3 MoveVector;
    public float2 LookDegreesDelta;
    public bool Jump;
    public bool Crouch;
}

[Serializable]
public struct FirstPersonCharacterView : IComponentData
{
    public Entity CharacterEntity;
}

public enum CrouchState
{
    Standing,
    Crouching,
    WishStanding
};

public enum ColliderLayers
{
    Nothing,
    CharacterMovementColliders,
    CharacterCrouchStateDetectionColliders
}

[Serializable]
[GhostComponent]
public struct FirstPersonCharacterHeight : IComponentData
{
    [GhostField] public CrouchState CrouchState;
    [GhostField] public float CurrentHeight;
    [GhostField] public float DesiredHeight;
}