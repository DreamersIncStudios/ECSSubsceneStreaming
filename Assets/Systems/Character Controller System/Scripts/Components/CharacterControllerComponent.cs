﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;
namespace MotionSystem.Components
{
    public struct CharControllerE : IComponentData
    {

        public float3 CapsuleCenter;
        public float CapsuleRadius;
        public float CapsuleHeight;
        public float3 OGCapsuleCenter { get; private set; }
        public float OGCapsuleHeight { get; private set; }
        public float H;
        public float V;
        public bool Jump { get; set; }
        public bool Crouch;

        public bool CombatCapable;
        public bool ApplyRootMotion { get { return IsGrounded; } }
        [SerializeField]public bool SkipGroundCheck { get; set; }
        public Vector3 Move { get; set; }
        public bool Walk;
        public Vector3 GroundNormal;
        public bool IsGrounded;
        public float GroundCheckDistance;
        public bool block;
        public float m_MovingTurnSpeed;
        public float m_StationaryTurnSpeed;
        public float m_JumpPower;
        public float m_GravityMultiplier;
        public float m_RunCycleLegOffset; //specific to the character in sample assets, will need to be modified to work with others
        public float m_MoveSpeedMultiplier;
        public float m_AnimSpeedMultiplier;
        public float m_OrigGroundCheckDistance;
        public bool AI;
        
        public float AnimationSpeed { get; set; }
        public bool Targetting;
        public bool CastingInput;
        public bool CastingTimer;
        //Todo Add back
        // => AnimationSpeed < 1.0f;
        public float Speed;
        public float SnapSpeed => 10;
        public bool Slowed { get; set; }

        public void Setup(MovementData data, CapsuleCollider col, bool aiControl) { 
            m_MovingTurnSpeed = data.MovingTurnSpeed;
            m_StationaryTurnSpeed= data.StationaryTurnSpeed;
            m_JumpPower = data.JumpPower;
            m_GravityMultiplier = data.GravityMultiplier;
            m_RunCycleLegOffset= data.RunCycleLegOffset;
            m_AnimSpeedMultiplier= data.AnimSpeedMultiplier;
            m_MoveSpeedMultiplier= data.MoveSpeedMultiplier;
            GroundCheckDistance= data.GroundCheckDistance;
            OGCapsuleCenter= CapsuleCenter = col.center;
            OGCapsuleHeight = CapsuleHeight= col.height;
            CombatCapable= data.CombatCapable;
            AI = aiControl;
        }
    }

    public struct AI_Control : IComponentData
    {
        public bool IsGrounded;
    }


    [Serializable]
    public struct MovementData
    {
        [Header("Weapon Specs")]
        public float EquipResetTimer;
        [Header("Animation Movement Specs")]
        public float MovingTurnSpeed;
        public float StationaryTurnSpeed;
            public  float JumpPower;
        [Range(1f, 4f)]public  float GravityMultiplier;
        public float RunCycleLegOffset; //specific to the character in sample assets, will need to be modified to work with others
        public  float MoveSpeedMultiplier;
        public  float AnimSpeedMultiplier;
        public  float GroundCheckDistance;
        public bool CombatCapable;
    }
}

