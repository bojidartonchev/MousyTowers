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

    public GameObject m_selfEffectSpawnPossition;
    public GameObject m_targetEffectSpawnPossition;
    public GameObject m_unitSpawnPossition;

    private float tickPeriod = 0.0f;

    [SyncVar]
    private int m_units;

    private float m_occupyTime = 5f;
    private float m_lastOccupationStart;

    private bool m_isATeamPresent;
    private bool m_isBTeamPresent;
    private bool m_isBeingOccupied;

    private Dictionary<HordeLeader, Unit[]> m_availableUnits = new Dictionary<HordeLeader, Unit[]>();

    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            CheckPresence();

            if ((m_isBTeamPresent || m_isATeamPresent) && m_isBeingOccupied == false)
            {
                m_isBeingOccupied = true;
                m_lastOccupationStart = Time.time;
            }

            if (m_isBTeamPresent && m_isATeamPresent && m_isBeingOccupied)
            {
                m_isBeingOccupied = false;
            }

            if (m_isBeingOccupied && m_lastOccupationStart + m_occupyTime > Time.time)
            {
                if (m_isATeamPresent)
                {
                    Occupy((Team)0);
                }
                else if (m_isBTeamPresent)
                {
                    Occupy((Team)1);
                }
            }

            if (IsOccupied() && m_isBeingOccupied == false)
            {
                if (tickPeriod > 1)
                {
                    //Do Stuff
                    tickPeriod = 0;

                    if (m_units < m_limit)
                    {
                        m_units++;

                        // Istantiate prefabs from server
                        var hordeUnit = Instantiate(m_HordeUnitPrefab, m_unitSpawnPossition.transform.position, m_unitSpawnPossition.transform.rotation);

                        // Assign parrent
                        hordeUnit.GetComponent<Unit>().parentNetId = this.netId;

                        //Spawn the GameObject you assign in the Inspector
                        NetworkServer.Spawn(hordeUnit);
                    }
                }
                tickPeriod += UnityEngine.Time.deltaTime;
            }
        }
        else
        {
        }
    }


    public bool IsOccupied()
    {
        return m_occupator != Team.None;
    }

    [Server]
    public void Occupy(Team team)
    {
        if(IsOccupied() == false)
        {
            m_occupator = team;

            RpcOnOccupy((int)team);
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

    private void CheckPresence()
    {
        foreach (var kvp in m_availableUnits)
        {
            if (kvp.Key.GetHorde().GetTeam() == (Team)0)
            {
                m_isATeamPresent = true;
            }
            else if (kvp.Key.GetHorde().GetTeam() == (Team)1)
            {
                m_isBTeamPresent = true;
            }
        }
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
            hordeInstance.transform.position = transform.position;
            var horde = hordeInstance.GetComponent<Horde>();
            horde.team = m_occupator; 
            NetworkServer.Spawn(hordeInstance);

            var hordeLeaderInstance = Instantiate(m_HordeLeaderPrefab, m_unitSpawnPossition.transform.position, m_unitSpawnPossition.transform.rotation);
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
        var gc = GameController.Instance;

        var materialColored = new Material(Shader.Find("Diffuse"));
        materialColored.color = gc.GetTeamColor((Team)team);
        this.GetComponentInChildren<Renderer>().material = materialColored;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Leader"))
        {
            var leader = other.gameObject.GetComponent<HordeLeader>();
            leader.GetHorde().transform.parent = this.transform;
            if (leader)
            {
                if (!m_availableUnits.ContainsKey(leader))
                {
                    m_availableUnits.Add(leader, leader.GetHorde().GetUnits());
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Leader"))
        {
            var leader = other.gameObject.GetComponent<HordeLeader>();
            if (leader)
            {
                if (m_availableUnits.ContainsKey(leader))
                {
                    m_availableUnits.Remove(leader);
                }
            }
        }
    }
}
