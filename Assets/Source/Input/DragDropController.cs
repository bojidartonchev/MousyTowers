using System.Collections;
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

    public GameObject m_projector;
    public Material m_projectorMat;

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
            m_source = ReturnClickedObject(out hitInfo, false);
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
            //m_groundRenderer.material.SetFloat("_Radius", 0f);
            //m_groundRenderer.material.SetFloat("_Border", 0f);
            if(m_projector)
            {
                m_projector.SetActive(false);
            }

            RaycastHit hitInfo;
            m_target = ReturnClickedObject(out hitInfo, false);

            if (isDragingSpell)
            {
                isDragingSpell = false;

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

                        targetPoss = targetTower.m_selfEffectSpawnPossition.transform.position;
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
            RaycastHit projectorHitInfo;
            ReturnClickedObject(out projectorHitInfo, true);

            RaycastHit hitInfo;
            var tempTarget = ReturnClickedObject(out hitInfo, false);

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

            if (m_projector)
            {
                m_projector.SetActive(true);
                m_projector.transform.position = new Vector3(projectorHitInfo.point.x, m_projector.transform.position.y, projectorHitInfo.point.z);
                m_projectorMat.SetColor("_Color", color);
            }
        }
    }

    public void StartSpellDrag(SpellType t)
    {
        isMouseDragging = true;
        isDragingSpell = true;
        spellType = t;
    }

    //Method to Return Clicked Object
    GameObject ReturnClickedObject(out RaycastHit hit, bool onlyTerrain)
    {
        GameObject target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(onlyTerrain)
        {
            if (Physics.Raycast(ray.origin, ray.direction * 10, out hit, 1000, 1 << LayerMask.NameToLayer("Terrain")))
            {
                target = hit.collider.gameObject;
            }
        }
        else
        {
            if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
            {
                target = hit.collider.gameObject;
            }
        }
        
        return target;
    }

}