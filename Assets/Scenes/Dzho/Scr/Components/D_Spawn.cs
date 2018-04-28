/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class D_Spawn : MonoBehaviour
{  
    public GameObject spawnIn = GameObject.fin;
    public GameObject spawnObject;
    public int spawnLimit = 100;
    public float respawnTime = 1.0F;
    public int startSpawnNumber = 0;

    private float nextSpawnTime;

    public void Spawn(GameObject obj)
    {
        var unit = Instantiate(obj, spawnIn.transform);
        Debug.Log("Spawn");
    }

	// Use this for initialization
	void Start ()
    {
		for(int i=0; i<startSpawnNumber; i++)
        {
            Spawn(spawnObject);
        };
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Time.time > nextSpawnTime)
        {
            nextSpawnTime = Time.time + respawnTime;
            var spawnBox = transform.localScale;
            Spawn(spawnObject);
        }
    }
}*/
