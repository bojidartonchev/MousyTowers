using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    private Player m_currentPlayer;

    public GameObject m_tREMOVE;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(m_tREMOVE && m_currentPlayer)
        {
            m_tREMOVE.GetComponent<Text>().text = m_currentPlayer.GetTeam().ToString();
        }
	}

    public void SetCurrentPlayer(Player player)
    {
        m_currentPlayer = player;
    }

    public Player GetCurrentPlayer()
    {
        return m_currentPlayer;
    }
}
