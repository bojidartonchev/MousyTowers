using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Tower : NetworkBehaviour {

    public int m_limit;
    public bool m_isStartTower;
    public Team m_startForTeam;

    [SyncVar]
    private int m_units;

    private Team m_occupator;

	// Use this for initialization
	void Start () {
        var gameCtrl = FindObjectOfType<GameController>();

        if (gameCtrl)
        {
            gameCtrl.AddTower(this);
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool IsOccupied()
    {
        return m_occupator != Team.None;
    }

    [Server]
    public void Occupy(Player p)
    {
        if(IsOccupied() == false)
        {
            m_occupator = p.GetTeam();

            RpcOnOccupy((int)p.m_team);
        }
    }

    public void Free()
    {
        Debug.Assert(m_units <= 0, "There are still units in this tower");

        m_occupator = Team.None;
    }

    // CMDs
    [Command]
    private void CmdInitMoveUnits()
    {

    }

    // RPC
    [ClientRpc]
    private void RpcSpawnUnit()
    {

    }

    [ClientRpc]
    private void RpcOnInitMoveUnits()
    {

    }

    [ClientRpc]
    private void RpcOnOccupy(int team)
    {
        var gc = FindObjectOfType<GameController>();

        var materialColored = new Material(Shader.Find("Diffuse"));
        materialColored.color = gc.GetTeamColor((Team)team);
        this.GetComponent<Renderer>().material = materialColored;
    }
}
