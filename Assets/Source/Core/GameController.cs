using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameController : NetworkBehaviour {

    private Team m_currentTeam;

    // Use getters instead
    private List<Player> m_players;
    private List<Tower> m_towers;

    private static GameController _instance;

    private static object _lock = new object();

    public static GameController Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                /*
				Debug.LogWarning ("[Singleton] Instance '" + typeof(T) +
				"' already destroyed on application quit." +
				" Won't create again - returning null.");
				*/
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (GameController)FindObjectOfType(typeof(GameController));

                    if (FindObjectsOfType(typeof(GameController)).Length > 1)
                    {
                        /*
						Debug.LogError ("[Singleton] Something went really wrong " +
						" - there should never be more than 1 singleton!" +
						" Reopening the scene might fix it.");
						*/
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<GameController>();

                        //singleton.name = "(singleton) " + typeof(T).ToString ();

                        DontDestroyOnLoad(singleton);
#if UNITY_EDITOR
                        singleton.name = string.Format("_{0}Singleton", typeof(GameController).Name);
#endif
                        /*
						Debug.Log ("[Singleton] An instance of " + typeof(T) +
						" is needed in the scene, so '" + singleton +
						"' was created with DontDestroyOnLoad.");
						*/
                    }
                    else
                    {
                        /*
						Debug.Log ("[Singleton] Using instance already created: " +
						_instance.gameObject.name);
						*/
                    }
                }

                return _instance;
            }
        }
    }

    private static bool applicationIsQuitting = false;

    /// <summary>
    /// When Unity quits, it destroys objects in a random order.
    /// In principle, a Singleton is only destroyed when application quits.
    /// If any script calls Instance after it have been destroyed, 
    ///   it will create a buggy ghost object that will stay on the Editor scene
    ///   even after stopping playing the Application. Really bad!
    /// So, this was made to be sure we're not creating that buggy ghost object.
    /// </summary>
    public void OnDestroy()
    {
        applicationIsQuitting = true;
    }

    public Team GetCurrentTeam()
    {
        if(m_currentTeam == Team.None && m_players.Count == 0)
        {
            //force update players
            var pl = Players;
        }

        return m_currentTeam;
    }

    public List<Player> Players
    {   get
        {
            if(m_players.Count <= 1)
            {
                m_players = new List<Player>();

                var players = FindObjectsOfType<Player>();
                if (players.Length > 0)
                {
                    foreach(var pl in players)
                    {
                        if(pl.isLocalPlayer)
                        {
                            m_currentTeam = pl.GetTeam();
                        }

                        m_players.Add(pl);
                    }
                }
            }            

            return m_players;
        }

        private set
        {
            m_players = value;
        }
    }

    public List<Tower> Towers
    {
        get
        {
            if (m_towers.Count <= 0)
            {
                var towers = FindObjectsOfType<Tower>();
                if (towers.Length > 0)
                {
                    m_towers.AddRange(towers);
                }
            }

            return m_towers;
        }

        private set
        {
            m_towers = value;
        }
    }


    public GameObject m_tREMOVE;

    // Use this for initialization
    void Awake () {
        m_players = new List<Player>();
        m_towers = new List<Tower>();     
    }
	
	// Update is called once per frame
	void Update () {
        if (m_tREMOVE)
        {
            m_tREMOVE.GetComponent<Text>().text = m_currentTeam.ToString();
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

        return Color.clear;
    }

    public Tower GetTowerByNetworkId(NetworkInstanceId id)
    {
        var tw = Towers.Find(t => t.netId == id);

        return tw;
    }

    public Tower GetTeamStartTower(Team team)
    {
        var tw = Towers.Find(t => t.m_occupator == team && t.m_isStartTower);

        return tw;
    }

    public Tower GetMyStartTower()
    {
        return GetTeamStartTower(m_currentTeam);
    }

    private Player GetTeamPlayer(Team p)
    {
        var pl = Players.Find(s => s.m_team == (int)(p == Team.None ? m_currentTeam : p));

        return pl;
    }
}
