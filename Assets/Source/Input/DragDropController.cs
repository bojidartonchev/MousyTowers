﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragDropController : MonoBehaviour
{
    //Initialize Variables
    GameObject m_source;
    GameObject m_target;
    bool isMouseDragging;
    Vector3 offsetValue;
    Vector3 positionOfScreen;

    bool isDragingSpell;
    SpellType spellType;

    public MeshRenderer m_groundRenderer;

    // Use this for initialization
    void Start()
    {

    }

    void Update()
    {
        //Mouse Button Press Down
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo;
            m_source = ReturnClickedObject(out hitInfo);
            if (m_source != null && m_source.tag == "Tower")
            {
                var sourceTower = m_source.GetComponent<Tower>();
                if(sourceTower && sourceTower.m_occupator == GameController.Instance.GetCurrentTeam())
                {
                    isMouseDragging = true;
                    //Converting world position to screen position.
                    positionOfScreen = Camera.main.WorldToScreenPoint(m_source.transform.position);
                    offsetValue = m_source.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, positionOfScreen.z));
                }                
            }
        }

        //Mouse Button Up
        if (Input.GetMouseButtonUp(0) && isMouseDragging)
        {
            isMouseDragging = false;

            // remove ground circle
            m_groundRenderer.material.SetFloat("_Radius", 0f);
            m_groundRenderer.material.SetFloat("_Border", 0f);

            RaycastHit hitInfo;
            m_target = ReturnClickedObject(out hitInfo);

            if (isDragingSpell)
            {
                Vector3 targetPoss = new Vector3();

                if(spellType == SpellType.ClearTower || spellType == SpellType.ProtectTower)
                {
                    if (m_target != null && m_target.tag == "Tower")
                    {
                        var targetTower = m_target.GetComponent<Tower>();

                        if (!targetTower || !(targetTower.m_occupator == GameController.Instance.GetCurrentTeam()))
                        {
                            return;
                        }

                        targetPoss = targetTower.gameObject.transform.position;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    targetPoss = hitInfo.point;
                }

                isDragingSpell = false;

                // lauchn spell
                var player = GameController.Instance.GetCurrentPlayer();
                if (player)
                {
                    player.CmdExecuteSpell(spellType, targetPoss);
                }
            }
            else
            {
                if (m_target != null && m_target.tag == "Tower")
                {
                    var targetTower = m_target.GetComponent<Tower>();
                    var sourceTower = m_source.GetComponent<Tower>();

                    if (targetTower && sourceTower && targetTower != sourceTower)
                    {
                        sourceTower.MoveUnits(targetTower);
                    }
                }
            }            
        }

        //Is mouse Moving
        if (isMouseDragging)
        {
            RaycastHit hitInfo;
            var tempTarget = ReturnClickedObject(out hitInfo);

            var color = Color.red;            

            if (isDragingSpell)
            {
                if(spellType == SpellType.ClearTower || spellType == SpellType.ProtectTower)
                {
                    // make circle green if acceptable target selected
                    if (tempTarget != null && tempTarget.tag == "Tower")
                    {
                        var sourceTower = tempTarget.GetComponent<Tower>();
                        if (sourceTower && sourceTower.m_occupator == GameController.Instance.GetCurrentTeam())
                        {
                            // make green
                            color = Color.green;
                        }
                    }
                }
                else
                {
                    color = Color.green;
                }                
            }
            else
            {
                // draging units

                // make circle green if acceptable target selected
                if (tempTarget != null && tempTarget.tag == "Tower")
                {
                    var tower = tempTarget.GetComponent<Tower>();
                    if (tower)
                    {
                        // make green
                        color = Color.green;
                    }
                }
            }

            m_groundRenderer.material.SetColor("_AreaColor", color);
            m_groundRenderer.material.SetVector("_Center", hitInfo.point);
            m_groundRenderer.material.SetFloat("_Radius", 3f);
            m_groundRenderer.material.SetFloat("_Border", 2f);
        }
    }

    public void StartSpellDrag(SpellType t)
    {
        isMouseDragging = true;
        isDragingSpell = true;
        spellType = t;
    }

    //Method to Return Clicked Object
    GameObject ReturnClickedObject(out RaycastHit hit)
    {
        GameObject target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
        {
            target = hit.collider.gameObject;
        }
        return target;
    }

}