using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Horde : NetworkBehaviour {

    public Team team;

	private Unit[] m_units;
	private HordeLeader m_leader;
	private int m_liveUnits;

	private bool isBattling;
	private float m_lastDeleteTime;
	private float m_deletionTime = 0.8f;

	void Start () {
		m_units = new Unit[transform.childCount];

		for (int i = 0; i < transform.childCount; i++) {
			m_units [i] = transform.GetChild (i).GetComponent<Unit> ();
			if (m_units [i].gameObject.tag == "Leader") 
			{
				m_leader = m_units [i].GetComponent<HordeLeader> ();
			}
		}

		m_liveUnits = m_units.Length;
	}

	private void Update()
	{
		if (isBattling && m_lastDeleteTime + m_deletionTime < Time.time) 
		{
			m_lastDeleteTime = Time.time;

			for (int i = 0; i < m_units.Length; i++) {
				if(m_units[i] && (m_units[i].gameObject.tag != "Leader" || m_liveUnits == 1))
				{
					Destroy(m_units[i].gameObject);
					m_liveUnits--;
					break;
				}
			}
		}
	}

	public void StartAttack(HordeLeader attacker)
	{
		Horde attackingHorde = attacker.GetHorde ();
		if (m_leader && attacker && attackingHorde && attackingHorde.team != team) {
			var attackingUnit = attackingHorde.GetRandomUnit();
			var attackPoint = (GetRandomUnit().transform.position - attackingUnit.transform.position) * 0.5f + attackingUnit.transform.position;
			for (int i = 0; i < m_units.Length; i++) {
				m_units [i].pathFinder.PrepareForAttack (attackPoint);
			}

			isBattling = true;
			m_lastDeleteTime = Time.time;
		}
	}

	public Unit GetRandomUnit()
	{
		int index = Random.Range (0, m_units.Length);

		return m_units [index];
	}

    public HordeLeader GetLeader()
    {
        return m_leader;
    }

    public Team GetTeam()
    {
        return team;
    }

	public int GetLiveUnits()
	{
		return m_liveUnits;
	}

	public Unit[] GetUnits(){
		return m_units;
	}


}
