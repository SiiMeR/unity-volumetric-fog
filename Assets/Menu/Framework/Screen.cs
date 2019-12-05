using Menu.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NewMainMenu.Base
{
    public abstract class Screen<T> : Screen where T : Screen<T>
    {
        public static T Instance { get; private set; }

        protected static void Open()
        {
            if (Instance == null) 
                MenuManager.Instance.CreateInstance<T>();
            else 
                Instance.gameObject.SetActive(true);
            
            MenuManager.Instance.OpenScreen(Instance);
        }

        protected static void Close()
        {
            if (Instance == null)
            {
                Debug.LogError("No instance of screen to close");
                return;
            }

            MenuManager.Instance.CloseScreen(Instance);
        }

        protected virtual void Awake()
        {
            Instance = (T) this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        public override void OnBackPressed()
        {
            Close();
        }
    }

    public abstract class Screen : MonoBehaviour
    {
        public bool destroyWhenClosed = true;

        public bool disableScreensUnderneath = true;
        public abstract void OnBackPressed();
    }
}