using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Screen = Menu.Framework.Screen;

namespace Menu.Framework
{
	public class MenuManager : Singleton<MenuManager>
	{
		[SerializeField] private RectTransform _escapeToSettings;
		
		[Header("Menu screens")]
		public PauseMenuController pauseMenuPrefab;
		public OptionsMenuMainScreenController optionsMenuMainPrefab;

		private readonly Stack<Screen> _screens = new Stack<Screen>();

		private void Awake()
		{
			// OptionsMenuMainScreenController.Show();
		}

		public void CreateInstance<T>() where T : Screen
		{
			var prefab = GetPrefab<T>();

			Instantiate(prefab, transform);
		}

		public void OpenScreen(Screen instance)
		{
			// De-activate top screen
			if (_screens.Count > 0)
			{
				if (instance.disableScreensUnderneath)
				{
					foreach (var screen in _screens)
					{
						screen.gameObject.SetActive(false);

						if (screen.disableScreensUnderneath)
							break;
					}
				}

				var topCanvas = instance.GetComponent<Canvas>();
				var previousCanvas = _screens.Peek().GetComponent<Canvas>();
				topCanvas.sortingOrder = previousCanvas.sortingOrder + 1;
			}
			
			_screens.Push(instance);
		}
		
		
		private T GetPrefab<T>() where T : Screen
		{
			// Get prefab dynamically, based on public fields set from Unity
			// You can use private fields with SerializeField attribute too
			var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (var field in fields)
			{
				var prefab = field.GetValue(this) as T;
				if (prefab != null)
				{
					return prefab;
				}
			}

			throw new MissingReferenceException("Prefab not found for type " + typeof(T));
		}
	
		public void CloseScreen(Screen screen)
		{
			if (_screens.Count == 0)
			{
				Debug.LogErrorFormat(screen, "{0} cannot be closed because the screen stack is empty", screen.GetType());
				return;
			}

			if (_screens.Peek() != screen)
			{
				Debug.LogErrorFormat(screen, "{0} cannot be closed because it is not on top of stack", screen.GetType());
				return;
			}

			CloseTopScreen();
		}

		public void CloseTopScreen()
		{
			var instance = _screens.Pop();

			if (instance.destroyWhenClosed)
				Destroy(instance.gameObject);
			else
				instance.gameObject.SetActive(false);

			// Re-activate top screen
			// If a re-activated screen is an overlay we need to activate the screen under it
			foreach (var screen in _screens)
			{
				screen.gameObject.SetActive(true);

				if (screen.disableScreensUnderneath)
					break;
			}

			if (_screens.Count == 0)
			{
				Time.timeScale = 1f;
				_escapeToSettings.DOAnchorPosY(0, 0.1f);
			}
		}

		private void Update()
		{
			// On Android the back button is sent as Esc
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				if (_screens.Count > 0)
				{
					_screens.Peek().OnBackPressed();
				}
				else
				{
					Time.timeScale = 0f;
					_escapeToSettings.DOAnchorPosY(-50, 0.2f);
					OptionsMenuMainScreenController.Show();
				}
			}
		}
	}
}
