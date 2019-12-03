using Menu.Framework;
using UnityEngine.SceneManagement;

namespace Menu
{
    public class PauseMenuController : AbstractScreen<PauseMenuController>
    {
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
