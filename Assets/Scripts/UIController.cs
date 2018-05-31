using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{

	[SerializeField] private GameObject UIPanel;

	private Player _player;
	private VolumetricFog _volumetricFog;
	
	// Use this for initialization
	void Start ()
	{
		_player = FindObjectOfType<Player>();
		_volumetricFog = FindObjectOfType<VolumetricFog>();
	}
	// Update is called once per frame
	void Update () {
//		if (Input.GetButtonDown("Cancel"))
//		{
//			UIPanel.SetActive(!UIPanel.activeInHierarchy);
//
//			if (_player)
//			{
//				_player.enabled = !UIPanel.activeInHierarchy;
//			}
//			
//		}
	}

	public void OnSliderValueChanged(Slider target)
	{
		
		print(target.name);
	}

	public void OnCheckboxValueChanged(Toggle toggle)
	{
		print(toggle.name);
	}

	public void OnDropdownValueChanged(Dropdown dropdown)
	{
		
	}

}
