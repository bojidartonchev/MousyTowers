using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class PathFinder : MonoBehaviour {

	public Transform Destination;

    private NavMeshAgent m_agent;

	void Start () {
		m_agent = GetComponent<NavMeshAgent> ();
	}

	void Update () {
		if(Destination && (m_agent && m_agent.destination != Destination.position))
        {
			m_agent.SetDestination(Destination.position);
        }
	}
}
