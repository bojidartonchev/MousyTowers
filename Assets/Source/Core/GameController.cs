using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameController : NetworkBehaviour {

    private Team m_currentTeam;

    private List<Player> m_players;
    private List<Tower> m_towers;

    public GameObject m_tREMOVE;

    // Use this for initialization
    void Start () {
        m_players = new List<Player>();
        m_towers = new List<Tower>();

        var players = FindObjectsOfType<Player>();
        if(players.Length > 0)
        {
            foreach (var p in players)
            {
                AddPlayer(p);
            }
        }

        var towers = FindObjectsOfType<Tower>();
        if (towers.Length > 0)
        {
            foreach (var t in towers)
            {
                AddTower(t);
            }
        }

        if(isServer)
        {
            if (m_players.Count >= 2)
            {
                InitGame();
            }
        }        
    }
	
	// Update is called once per frame
	void Update () {
        if (m_tREMOVE)
        {
            m_tREMOVE.GetComponent<Text>().text = m_currentTeam.ToString();
        }
    }

    [Server]
    public void InitGame()
    {
        if(isServer)
        {
            foreach(var pl in m_players)
            {
                var tower = m_towers.Find(t => t.m_isStartTower && t.m_startForTeam == pl.GetTeam());

                if (tower)
                {
                    tower.Occupy(pl);
                }
            }            
        }
    }

    public void AddPlayer(Player pl)
    {
        if(m_players != null)
        {
            bool alreadyAdded = m_players.Find(p => p.GetInstanceID() == pl.GetInstanceID()) != null;
            if (alreadyAdded == false)
            {
                if (pl.isLocalPlayer)
                {
                    m_currentTeam = pl.GetTeam();
                }

                m_players.Add(pl);

                if (isServer)
                {
                    if (m_players.Count >= 2)
                    {
                        InitGame();
                    }
                }
            }
        }        
    }

    public void AddTower(Tower t)
    {
        if(m_towers != null)
        {
            bool alreadyAdded = m_towers.Find(tw => tw.GetInstanceID() == t.GetInstanceID()) != null;
            if (alreadyAdded == false)
            {
                m_towers.Add(t);
            }
        }
        
    }

    public Player GetCurrentPlayer()
    {
        return GetTeamPlayer(m_currentTeam);
    }

    public Color GetTeamColor(Team t)
    {
        var player = GetTeamPlayer(t);

        if(player)
        {
            return player.m_color;
        }

        return Color.white;
    }

    private Player GetTeamPlayer(Team p)
    {
        var pl = m_players.Find(s => s.m_team == (int)p);

        return pl;
    }
}
