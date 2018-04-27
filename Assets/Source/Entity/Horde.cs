using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Horde : MonoBehaviour {

	private Unit[] m_units;

	void Start () {
		m_units = new Unit[transform.childCount];

		for (int i = 0; i < transform.childCount; i++) {
			m_units [i] = transform.GetChild (i).GetComponent<Unit> ();
		}
	}

	public Unit[] GetUnits(){
		return m_units;
	}
}
