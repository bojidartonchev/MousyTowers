using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Tower : NetworkBehaviour {

    public int m_limit;
    public bool m_isStartTower;

    [SyncVar]
    private int m_units;

    private Player m_occupator;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool IsOccupied()
    {
        return m_occupator != null;
    }

    public void Free()
    {
        Debug.Assert(m_units <= 0, "There are still units in this tower");

        m_occupator = null;
    }

    // CMDs
    private void CmdSendUnits()
    {

    }
}
