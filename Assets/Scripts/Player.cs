using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{

	[SerializeField] private float _MoveSpeed = 10;
	[SerializeField] private bool _LockCursor = true;
	[SerializeField] private float _XSensitivity = 1;
	[SerializeField] private float _YSensitivity = 1;

	private bool _isCursorLocked;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		
		float yRot = Input.GetAxisRaw("Mouse X") * _XSensitivity;
		float xRot = Input.GetAxisRaw("Mouse Y") * _YSensitivity;
		
		var ms = _MoveSpeed;
		
		if (Input.GetKey(KeyCode.LeftShift))
		{
			ms *= 2;
		}
		
		transform.position += Camera.main.transform.forward * ms * Input.GetAxis("Vertical") * Time.deltaTime;
		transform.position += Camera.main.transform.right * ms * Input.GetAxis("Horizontal") * Time.deltaTime;

		transform.localRotation *= Quaternion.Euler (0f, yRot, 0f);
		
		var cameras = gameObject.GetComponentsInChildren<Camera>();
		
		foreach (var cam in cameras)
		{
			cam.transform.localRotation *= Quaternion.Euler(-xRot, 0f, 0f);
		}
		
	
		UpdateCursorLock();
	}
	
	//
	// CODE BELOW IS FROM UNITY STANDARD ASSETS MouseLook.cs
	//

	
	public void UpdateCursorLock()
	{
		//if the user set "lockCursor" we check & properly lock the cursos
		if (_LockCursor)
			InternalLockUpdate();
	}

	private void InternalLockUpdate()
	{
		if(Input.GetKeyUp(KeyCode.Escape))
		{
			_isCursorLocked = false;
		}
		else if(Input.GetMouseButtonUp(0))
		{
			_isCursorLocked = true;
		}

		if (_isCursorLocked)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
