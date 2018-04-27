﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour {

	[HideInInspector]
	public PathFinder pathFinder;

	void Start () {
		pathFinder = GetComponent<PathFinder> ();	
	}
}
