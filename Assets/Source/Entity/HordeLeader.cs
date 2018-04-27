using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HordeLeader : Unit {

	private Horde m_horde;

	private void Start(){
		base.Start ();
		m_horde = transform.parent.GetComponent<Horde> ();
	}

	private void OnTriggerEnter(Collider col){
		var hordeLeader = col.GetComponent<HordeLeader> ();

		if (hordeLeader) {
			NotifyHorde (hordeLeader);
		}
	}

	private void NotifyHorde(HordeLeader threat){
		m_horde.StartAttack (threat);
	}

	public Horde GetHorde()
	{
		return m_horde;
	}
}
