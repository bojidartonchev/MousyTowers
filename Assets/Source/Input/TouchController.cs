using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchController : MonoBehaviour {

	public GameObject Team1;
	public GameObject Team2;

	
	// Update is called once per frame
	void Update () {
		if ( Input.GetMouseButtonDown (0) || Input.GetMouseButtonDown (1)){ 
			RaycastHit hit; 
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
			if ( Physics.Raycast (ray,out hit,100.0f)  && hit.transform.gameObject.tag == "Tower") {
				if (Input.GetMouseButton (0)) 
				{
					for (int i = 0; i < Team1.transform.childCount; i++) {
						Team1.transform.GetChild (i).GetComponent<PathFinder> ().SetDestination(hit.transform);
					}
				}
				else if (Input.GetMouseButtonDown (1))
				{
					for (int i = 0; i < Team1.transform.childCount; i++) {
						Team2.transform.GetChild (i).GetComponent<PathFinder> ().SetDestination(hit.transform);
					}
				}
					
			}
		}
	}
}
