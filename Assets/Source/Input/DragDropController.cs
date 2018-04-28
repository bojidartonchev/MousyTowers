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

            RaycastHit hitInfo;
            m_target = ReturnClickedObject(out hitInfo);
            if (m_target != null && m_target.tag == "Tower")
            {
                var targetTower = m_target.GetComponent<Tower>();
                var sourceTower = m_source.GetComponent<Tower>();

                if(targetTower && sourceTower && targetTower!=sourceTower)
                {
                    sourceTower.MoveUnits(targetTower);
                }
            }
        }

        //Is mouse Moving
        if (isMouseDragging)
        {
            //tracking mouse position.
            Vector3 currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, positionOfScreen.z);

            //converting screen position to world position with offset changes.
            Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenSpace) + offsetValue;

            //It will update target gameobject's current postion.
            //getTarget.transform.position = currentPosition;
        }


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