﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpellType
{
    ProtectTower,
    ClearTower,
    FreezeTarget
}

public class Spell : MonoBehaviour {

    public SpellType m_type;
    public float m_cooldown;
    public GameObject m_spellEffect;

    private float m_lastActiveTime;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool CanExecute()
    {
        return true;
    }

    public void Execute(Vector3 targetCoords)
    {
        var mainTower = GameController.Instance.GetMyStartTower();

        if(mainTower)
        {
            var projectile = Instantiate(m_spellEffect);

            if(m_type == SpellType.ClearTower || m_type == SpellType.ProtectTower)
            {
                projectile.transform.position = new Vector3(mainTower.transform.position.x, 0f, mainTower.transform.position.z);
            }
            else
            {
                projectile.transform.position = mainTower.transform.position;
            }
            
            if(m_type == SpellType.FreezeTarget)
            {
                projectile.gameObject.transform.LookAt(targetCoords);
            }           

            // getProjectile script and send to tower.
            // if on server, detect colision and notify clients
        }
    }
}
