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
	private Camera _camera;

	// Use this for initialization
	void Start ()
	{
		_camera = Camera.main;
	}
	
	// Update is called once per frame
	void Update (){
		var yRot = Input.GetAxisRaw("Mouse X") * _XSensitivity;
		var xRot = Input.GetAxisRaw("Mouse Y") * _YSensitivity;
		
		var ms = _MoveSpeed;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			ms *= 2;
		}

		var position = transform.position;
		position += Time.deltaTime * ms * Input.GetAxis("Vertical") * _camera.transform.forward;
		position += Time.deltaTime * ms * Input.GetAxis("Horizontal") * _camera.transform.right;
		transform.position = position;

		transform.localRotation *= Quaternion.Euler (0f, yRot, 0f);
		
		_camera.transform.localRotation *= Quaternion.Euler(-xRot, 0f, 0f);
		
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
