using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Tower : NetworkBehaviour {

    public int m_limit;
    public bool m_isStartTower;
    public Team m_occupator;

    public GameObject m_HordePrefab;
    public GameObject m_HordeLeaderPrefab;
    public GameObject m_HordeUnitPrefab;

    public GameObject m_SpawnPoss;

    private float tickPeriod = 0.0f;

    [SyncVar]
    private int m_units;

    [SyncVar(hook = "OnColorChange")]
    private Color m_color = Color.clear;

    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update()
    {
        if(isServer)
        {
            if (IsOccupied())
            {
                if (tickPeriod > 1)
                {
                    //Do Stuff
                    tickPeriod = 0;

                    if(m_units < m_limit)
                    {
                        m_units++;

                        // Istantiate prefabs from server
                        var hordeUnit = Instantiate(m_HordeUnitPrefab, m_SpawnPoss.transform);

                        // Assign parrent
                        hordeUnit.GetComponent<Unit>().parentNetId = this.netId;

                        //Spawn the GameObject you assign in the Inspector
                        NetworkServer.Spawn(hordeUnit);

                        hordeUnit.GetComponent<PathFinder>().SetDestination(this.transform);
                    }
                }
                tickPeriod += UnityEngine.Time.deltaTime;
            }
        }
        else
        {
            if (m_color == Color.clear)
            {
                var c = GameController.Instance.GetTeamColor(m_occupator);

                if (c != Color.clear)
                {
                    OnColorChange(c);
                }
            }
        }        
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
            m_color = p.m_color;

            RpcOnOccupy((int)p.m_team);
        }
    }

    public void Free()
    {
        Debug.Assert(m_units <= 0, "There are still units in this tower");

        m_occupator = Team.None;
    }

    public void MoveUnits(Tower target)
    {
        if(target)
        {
            CmdInitMoveUnits(target.netId);
        }
    }

    public int GetUnits()
    {
        return m_units;
    }

    // CMDs
    [Command]
    private void CmdInitMoveUnits(NetworkInstanceId targetId)
    {
        var target = GameController.Instance.GetTowerByNetworkId(targetId);

        if (target)
        {
            var hordeCount = Math.Min(target.m_limit, m_units / 2);
            m_units -= hordeCount;

            // Istantiate prefabs from server
            var hordeInstance = Instantiate(m_HordePrefab);
            hordeInstance.transform.position = m_SpawnPoss.transform.position;
            NetworkServer.Spawn(hordeInstance);

            var hordeLeaderInstance = Instantiate(m_HordeLeaderPrefab);
            hordeLeaderInstance.transform.position = m_SpawnPoss.transform.position;
            hordeLeaderInstance.transform.parent = hordeInstance.transform;
            hordeLeaderInstance.GetComponent<HordeLeader>().parentNetId = hordeInstance.GetComponent<Horde>().netId;
            NetworkServer.Spawn(hordeLeaderInstance);

            var children = GetComponentsInChildren<Unit>();
            for (int i = 0; i < hordeCount; i++)
            {
                if (i >= children.Length)
                {
                    break;
                }

                if (i == 0)
                {
                    //Destroy first unit to compensate the leader creation
                    NetworkServer.Destroy(children[i].gameObject);
                    continue;
                }

                // Already spawned, so just move to the new parent
                children[i].parentNetId = hordeInstance.GetComponent<Horde>().netId;
                children[i].transform.parent = hordeInstance.transform;
            }

            for (int i = 0; i < hordeInstance.transform.childCount; i++)
            {
                hordeInstance.transform.GetChild(i).GetComponent<PathFinder>().SetDestination(target.transform);
            }
        }
    }

    [ClientRpc]
    private void RpcOnOccupy(int team)
    {
        //var gc = GameController.Instance;
        //
        //var materialColored = new Material(Shader.Find("Diffuse"));
        //materialColored.color = gc.GetTeamColor((Team)team);
        //this.GetComponent<Renderer>().material = materialColored;
    }

    private void OnColorChange(Color newColor)
    {
        m_color = newColor;

        var materialColored = new Material(Shader.Find("Diffuse"));
        materialColored.color = m_color;
        this.GetComponent<Renderer>().material = materialColored;
    }
}
