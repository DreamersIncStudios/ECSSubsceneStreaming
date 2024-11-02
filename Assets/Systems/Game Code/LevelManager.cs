using DreamersInc;
using DreamersInc.BestiarySystem;
using DreamersIncStudios.MoonShot;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Utilities;

public class LevelManager : MonoBehaviour
{
    GameMaster GM;
    // Start is called before the first frame update
    void Start()
    {
        GM= GameMaster.Instance;
        BestiaryDB.SpawnPlayer(2,new Vector3(0,0,35));
    }

   public virtual void LoadLevel() {
        BestiaryDB.SpawnPlayer(2,new Vector3(0,1,25));

    }
}
