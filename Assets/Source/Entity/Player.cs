using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour {

    [SyncVar]
    public int m_team;

    [SyncVar]
    public Color m_color;

    public List<Spell> m_spells;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetTeam(Team t)
    {
        m_team = (int)t;
    }

    public Team GetTeam()
    {
        return (Team)m_team;
    }

    [Command]
    public void CmdExecuteSpell(SpellType type, Vector3 target)
    {
        var spell = m_spells.Find(s => s.m_type == type);

        if(spell && spell.CanExecute())
        {
            RpcOnExecuteSpell(type, target);
        }
    }

    [ClientRpc]
    public void RpcOnExecuteSpell(SpellType type, Vector3 target)
    {
        var spell = m_spells.Find(s => s.m_type == type);

        if (spell)
        {
            spell.Execute(target);
        }
    }
}
