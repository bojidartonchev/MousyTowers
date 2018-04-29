using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D_GameObj : MonoBehaviour {

    public D_Player owner;
    public int m_team;

	// Use this for initialization
	void Start () {
        if (owner) m_team = owner.m_team;
        else m_team = 0;
	}

}
