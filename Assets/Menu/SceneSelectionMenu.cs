using DG.Tweening;
using Menu.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menu
{
    public class SceneSelectionMenu : AbstractScreen<SceneSelectionMenu>
    {
        [SerializeField] private RectTransform _menuRect;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            _canvasGroup = _menuRect.GetComponent<CanvasGroup>();

            _menuRect.anchoredPosition = new Vector2(600, _menuRect.anchoredPosition.y);
            var inAnimationDuration = 0.4f;
            _menuRect.DOAnchorPosX(0, inAnimationDuration);
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1, inAnimationDuration).SetEase(Ease.Linear);
        }

        public override void OnBackPressed()
        {
            var outDuration = 0.3f;
            DOTween.Sequence()
                .Join(_menuRect.DOAnchorPosX(600, outDuration))
                .Join(_canvasGroup.DOFade(0, outDuration).SetEase(Ease.Linear))
                .AppendCallback(() => base.OnBackPressed());
        }

        public void OnSceneSelected(string scene)
        {
            // Close();
            SceneManager.LoadScene(scene);
            Time.timeScale = 1f;
        }
    }
}
