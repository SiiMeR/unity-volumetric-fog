using System;
using System.Collections.Generic;
using System.Linq;
using Menu.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menu
{
    public class PauseMenuController : AbstractScreen<PauseMenuController>
    {
        private List<Button> _buttons;

        private void Start()
        {
            _buttons = GetComponentsInChildren<Button>().ToList();
            // _buttons.ForEach(button => button.OnPointerEnter());
        }

        public void OnCloseMenuPressed()
        {
            Hide();
        }

        public void OnOptionsPressed()
        {
            OptionsMenuMainScreenController.Show();
        }

        public void OnRestartPressed()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnChangeScenePressed()
        {
            ChangeSceneScreenController.Show();
        }
    }
}
