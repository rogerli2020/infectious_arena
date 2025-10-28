using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;

namespace PlayerMovement
{
    public static class EntitiesPlayerMovementUtil
    {
        public static void UpdateVelocityTemp(
            ref float3 velocity, float3 targetVelocity, 
            ref FirstPersonCharacterControl playerInput, bool isGrounded, float deltaTime,
            float groundAccel, float airAccel, float friction, float gravity, float jumpSpeed, float stopSpeed, 
            float moveSpeed, float maxSpeed, bool isSliding, float3 groundNormal)
        {
            if (isSliding)
                isGrounded = false;

            // save and zero out vertical velocity
            float yVel = velocity.y;
            velocity.y = 0f;
            
            // handle horizontal velocities
            if (isGrounded) HandleFriction(ref velocity, deltaTime, friction, stopSpeed);
            
            HandleHorizontalMovement(ref velocity, targetVelocity, isGrounded, moveSpeed, maxSpeed, 
                groundAccel, airAccel, deltaTime, friction);
            
            if (!isSliding) ClampHorizontalSpeed(ref velocity, maxSpeed);
            
            // recover vertical velocity
            velocity.y = yVel;
            
            // sliding logic
            if (isSliding)
            {
                velocity -= math.project(velocity, groundNormal);
            }
            
            SanitizeVector3(ref velocity);
        }

        private static void ClampHorizontalSpeed(ref float3 velocity, float maxSpeed)
        {
            float horizontalSpeed = math.length(velocity);

            if (horizontalSpeed > maxSpeed)
            {
                velocity *= maxSpeed / horizontalSpeed;
            }
        }

        private static void HandleFriction(ref float3 velocity, float deltaTime, float friction, float stopSpeed)
        {
            float horizontalSpeed = math.length(velocity);
            
            if (horizontalSpeed > 0f)
            {
                float dropInSpeed = math.max(stopSpeed, horizontalSpeed) * friction * deltaTime;
                velocity = math.normalize(velocity) * math.max(0f, horizontalSpeed - dropInSpeed);
            }
        }
        

        private static void HandleHorizontalMovement(ref float3 velocity, float3 targetVelocity, bool isGrounded,
            float moveSpeed, float maxSpeed, float groundAccel, float airAccel, float deltaTime, float friction)
        {
            float3 wishVelocity;
            float3 wishDirection;
            float wishSpeed;
            float acceleration;
            float addSpeed, currentSpeed, accelSpeed;

            wishVelocity = targetVelocity;
            if (math.length(wishVelocity) == 0) return;
            
            wishDirection = math.normalize(wishVelocity);
            wishSpeed = math.length(wishVelocity);
            if (isGrounded)
                acceleration = groundAccel;
            else
                acceleration = airAccel;

            // calculate speed to add
            currentSpeed = math.dot(velocity, wishDirection);
            addSpeed = wishSpeed - currentSpeed;

            // return if no speed to add
            if (addSpeed <= 0f) return;

            accelSpeed = acceleration * deltaTime * wishSpeed * friction;
            accelSpeed = math.min(accelSpeed, addSpeed);

            velocity += accelSpeed * wishDirection;
        }

        // private static void HandleJump(ref float3 velocity, float jumpSpeed)
        // {
        //     velocity.y = 100f;
        // }
        
        // private static void HandleGravity(ref float3 velocity, float deltaTime, float gravity)
        // {
        //     velocity.y -= gravity * deltaTime;
        // }

        public static void SanitizeVector3(ref float3 velocity)
        {
            if (float.IsNaN(velocity.x)) velocity.x = 0f;
            if (float.IsNaN(velocity.y)) velocity.y = 0f;
            if (float.IsNaN(velocity.z)) velocity.z = 0f;
        }
    }
}