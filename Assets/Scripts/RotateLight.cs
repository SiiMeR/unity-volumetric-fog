using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateLight : MonoBehaviour
{

	[SerializeField] private float RotateSpeed = 50f;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		transform.RotateAround(transform.position, transform.up, Time.deltaTime * RotateSpeed);
	}
}
