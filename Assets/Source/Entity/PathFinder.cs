using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class PathFinder : MonoBehaviour {

    private NavMeshAgent m_agent;

	void Start () {
		m_agent = GetComponent<NavMeshAgent> ();
	}



	public void SetDestination(Transform destination){
		if(destination && (m_agent && m_agent.destination != destination.position))
		{
			m_agent.SetDestination(destination.position);
		}
	}

	public void PrepareForAttack(Vector3 attackDestination)
	{
		if (m_agent) {
			m_agent.destination = attackDestination;
			m_agent.stoppingDistance = 0.0f;
		}
	}
}
