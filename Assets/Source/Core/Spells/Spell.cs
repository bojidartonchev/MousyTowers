using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;

public enum SpellType
{
    ProtectTower,
    ClearTower,
    FreezeTarget
}

public class Spell : MonoBehaviour {

    public SpellType m_type;
    public int m_cooldown;
    public int m_duration;
    public GameObject m_spellEffect;

    private int m_lastActiveTime;
    private GameObject m_projectile;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool CanExecute()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

        return cur_time > m_lastActiveTime + m_cooldown;
    }

    public void Execute(Vector3 startCoords, Vector3 targetCoords)
    {
        var m_projectile = Instantiate(m_spellEffect);
        if (m_type == SpellType.ClearTower || m_type == SpellType.ProtectTower)
        {
            m_projectile.transform.position = targetCoords;
        }
        else
        {
            m_projectile.transform.position = startCoords;
        }

        if (m_type == SpellType.FreezeTarget)
        {
            for (int i = 0; i < m_projectile.transform.childCount; i++)
            {
                m_projectile.gameObject.transform.GetChild(i).LookAt(targetCoords);
            }            
        }

        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        m_lastActiveTime = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

        var destroyer = m_projectile.AddComponent<ParticleSystemDestroyer>();
        destroyer.maxDuration = m_duration;
        destroyer.minDuration = m_duration - 1;
    }
}
