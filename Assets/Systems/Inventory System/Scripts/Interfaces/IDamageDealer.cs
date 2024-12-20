using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Stats;
using System;
using Dreamers.InventorySystem.Interfaces;
using Stats.Entities;

namespace DreamersInc.DamageSystem.Interfaces
{
    public interface IDamageDealer
    {
        int BaseDamage { get; }
        float CriticalHitMod { get; }
        float MagicMod { get; }
        bool CriticalHit { get; }
        ElementName ElementName { get; }
        TypeOfDamage TypeOfDamage { get; }

        void SetElement(ElementName value);
        void SetDamageType();
        int DamageAmount();
        bool DoDamage { get; }
        void SetDamageBool(bool value);
        public event EventHandler<OnHitArgs> OnHitAction; 
        public Action ChanceCheck { get; set; }
        Action CriticalEventCheck { get; set; }
        void SetStatData( BaseCharacterComponent stats,TypeOfDamage damageType);
        public Stat Magic_Offense { get; }
        public Stat Range_Offense { get; }
        public Stat Melee_Offense {get;}
        public Attributes Skill { get; }
        public Attributes Speed { get; }
        public WeaponType Type { get; }
    }

}
