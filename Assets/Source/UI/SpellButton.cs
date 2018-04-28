using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellButton : MonoBehaviour {

    public SpellType m_type;

    private bool m_dragging;

	// Use this for initialization
	void Start () {
        m_dragging = false;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnPointerDown()
    {
        m_dragging = true;
    }

    public void OnPointerUp()
    {
        m_dragging = false;

        if(m_type == SpellType.ClearTower || m_type == SpellType.ProtectTower)
        {
            var player = GameController.Instance.GetCurrentPlayer();

            if(player)
            {
                player.CmdExecuteSpell(m_type, new Vector3()); // no target needed;
            }
        }
    }

    public void OnPointerExit()
    {
        if(m_dragging)
        {
            // notify draging component to start draging
        }
    }
}
