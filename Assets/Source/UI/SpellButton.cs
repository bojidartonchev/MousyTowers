using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellButton : MonoBehaviour {

    public SpellType m_type;

    private bool m_dragging;
    private DragDropController m_dragDropCtrl;

	// Use this for initialization
	void Start () {
        m_dragging = false;

        m_dragDropCtrl = FindObjectOfType<DragDropController>();
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
    }

    public void OnPointerExit()
    {
        if(m_dragging)
        {
            if (m_dragDropCtrl)
            {
                m_dragDropCtrl.StartSpellDrag(m_type);
            }
        }
    }
}
