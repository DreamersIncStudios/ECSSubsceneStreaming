using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine;
using System;
//using DreamerInc.CombatSystem;
using Random = UnityEngine.Random;
using DreamersInc.CharacterControllerSys.VFX;
using UnityEngine.Serialization;

namespace DreamersInc.ComboSystem
{

    [System.Serializable]
    public struct AnimationCombo

    {
        public AttackType CurrentAnim;
        [SerializeField] private uint triggerAnimIndex;
        public bool Alternate;
        public uint TriggerAnimIndex { get => triggerAnimIndex;
            set => triggerAnimIndex = value;
        }
        public string CurrentStateName => CurrentAnim.ToString() + TriggerAnimIndex;
        public float2 NormalizedInputTime;
        public float AnimationEndTime;
        public bool InputAllowed(float time) => time > NormalizedInputTime.x && time < NormalizedInputTime.y;
        public float MaxProb { get; set; }
        public AnimationTrigger Trigger;
        // TODO consider adding late inputs ??????

    }

    public interface ITrigger
    {
        public ComboNames Name { get; } // Change To String ???????????
        public string TriggerString { get; }
    }
    [System.Serializable]
    public struct AnimationTrigger : ITrigger
    {
        ComboNames name;
        public ComboNames Name { get { return name; } } // TODO Remove
        public uint TriggerAnimIndex { get => triggerAnimIndex;
            set => triggerAnimIndex = value;
        }
        public AttackType AttackType;
        public uint triggerAnimIndex;
        public string TriggerString => AttackType.ToString() + TriggerAnimIndex;
        public float TransitionDuration;
        public float TransitionOffset;
        [FormerlySerializedAs("EndofCurrentAnim")] public float EndOfCurrentAnim;
        public VFX AttackVFX;
        [Tooltip(" testing Value")]
        public float Chance;
        [Range(-1, 100)]
        [Tooltip("Value Must be between 0 and 100 \n " +
            "-1 is used for never repeat")]

        public int ChanceForNextAttack;
        public float ProbabilityTotalWeight { get; set; }

        //   float probabilityPercent => Chance / ProbabilityTotalWeight * 100;
        public float ProbabilityRangeFrom { get; set; }
        float ProbabilityRangeTo => ProbabilityRangeFrom + Chance;
        public void SetRangeFrom(float startPoint)
        {
            ProbabilityRangeFrom = startPoint;
        }
        public bool Picked(float picked)
        {
            return picked > ProbabilityRangeFrom && picked < ProbabilityRangeTo;
        }

        //TODO add stat modifer increase or decrease likely hood of sequential attack based on stats
        public bool AttackAgain(float selected)
        {
            return selected < ChanceForNextAttack && ChanceForNextAttack != -1;
        }
        [FormerlySerializedAs("delay")] public float Delay;
        public bool Trigger => Delay <= 0.0f;
        public void AdjustTime(float time)
        {
            Delay -= time;
        }

    }
    [Serializable]
    public struct VFX
    {
        public int ID;
        public float Forward, Up;
        public Vector3 Rot;
        [Tooltip("Time in Milliseconds")]
        public float LifeTime;
        [Range(0, 100)]
        public int ChanceToPlay;
        public bool Play => ID != 0;
        public void SpawnVFX(Transform characterTransform)
        {
            int prob = Mathf.RoundToInt(Random.Range(0, 99));
            if (prob < ChanceToPlay)
            {
                Vector3 forwardPos = characterTransform.forward * Forward + characterTransform.up * Up;
                VFXDatabase.Instance.PlayVFX(ID, characterTransform.position + forwardPos, characterTransform.rotation.eulerAngles + Rot, 0, LifeTime);
            }
        }
    }


    [System.Serializable]
    public struct SetTrigger
    {
        [SerializeField] private ComboNames name;
        [SerializeField] private ComboAnimNames triggerAnimName;

        public ComboNames Name => name; // Change To String ???????????
        public ComboAnimNames TriggeredAnimName => triggerAnimName; // Change to String ???????????

    }
    [System.Serializable]
    public struct ComboPattern
    {
        [FormerlySerializedAs("name")] public ComboNames Name;
        public List<SetTrigger> Attacks;
    }
    public enum ComboAnimNames
    {
        None, Grounded, Targeted_Locomation, Locomation_Grounded_Weapon,
        Equip_Light, Equip_Heavy, Equip_LightCharged, Equip_HeavyCharged, Equip_Projectile,
        Light_Attack1, Light_Attack2, Light_Attack3, Light_Attack4, Light_Attack5, Light_Attack6,
        Heavy_Attack1, Heavy_Attack2, Heavy_Attack3, Heavy_Attack4, Heavy_Attack5, Heavy_Attack6
            , Ground_attack02, Light_Attack1_Alt, Projectile, ChargedProjectile
    }
    public enum AttackType
    {
        none, LightAttack, HeavyAttack, ChargedLightAttack, ChargedHeavyAttack, Projectile, ChargedProjectile, Grounded, Targeted_Locomotion, Locomotion_Grounded_Weapon,
        SpecialAttack, Defend, Dodge

    }
    public enum ComboNames
    {
        None, Combo_1, Combo_2, Combo_3, Combo_4, Combo_5, Combo_6, Combo_7, Combo_8, Combo_9, Combo_10,
        Projectile1, Dodge,
    }
}