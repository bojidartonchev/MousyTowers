using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Unit : NetworkBehaviour {

	[HideInInspector]
	public PathFinder pathFinder;

    [SyncVar]
    public NetworkInstanceId parentNetId;

    public override void OnStartClient()
    {
        // When we are spawned on the client,
        // find the parent object using its ID,
        // and set it to be our transform's parent.
        GameObject parentObject = ClientScene.FindLocalObject(parentNetId);
        if(parentObject)
        {
            transform.SetParent(parentObject.transform);
        }
        else
        {
            Debug.Log(this.gameObject.name);
        }
    }

    protected void Start () {
		pathFinder = GetComponent<PathFinder> ();
	}
}
