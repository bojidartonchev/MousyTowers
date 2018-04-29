using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HudController : MonoBehaviour {

    public string HudScene;

	// Use this for initialization
	void Start () {
		if(HudScene != "")
        {
            SceneManager.LoadSceneAsync(HudScene, LoadSceneMode.Additive);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
