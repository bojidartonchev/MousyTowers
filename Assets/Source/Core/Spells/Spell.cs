using System.Collections;
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

    public void Execute(Vector3 startCoords, Vector3 targetCoords)
    {
        var projectile = Instantiate(m_spellEffect);
        if (m_type == SpellType.ClearTower || m_type == SpellType.ProtectTower)
        {
            projectile.transform.position = new Vector3(targetCoords.x, 0f, targetCoords.z);
        }
        else
        {
            projectile.transform.position = startCoords;
        }

        if (m_type == SpellType.FreezeTarget)
        {
            for (int i = 0; i < projectile.transform.childCount; i++)
            {
                projectile.gameObject.transform.GetChild(i).LookAt(targetCoords);
            }            
        }
    }
}
