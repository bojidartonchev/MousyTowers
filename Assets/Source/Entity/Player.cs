using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour {

    [SyncVar]
    public int m_team;

    [SyncVar]
    public Color m_color;

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
}
