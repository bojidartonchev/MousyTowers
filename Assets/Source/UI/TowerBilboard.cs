using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerBilboard : MonoBehaviour {
    public Camera m_Camera;

    // Use this for initialization
    void Start () {
        m_Camera = Camera.main;

        transform.LookAt(transform.position + m_Camera.transform.rotation * Vector3.forward,
            m_Camera.transform.rotation * Vector3.up);
    }   

    void Update()
    {
        
    }
}
