using System;
using System.Linq;
using DG.Tweening;
using Menu.Framework;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class OptionsMenuMainScreenController : AbstractScreen<OptionsMenuMainScreenController>
    {
        [SerializeField] private RectTransform _menuRect;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            _canvasGroup = _menuRect.GetComponent<CanvasGroup>();
            var volumetricFog = FindObjectOfType<VolumetricFog>();
            if (!volumetricFog)
            {
                return;
            }

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

        public void OnChangeScenePressed()
        {
            
        }

        public void OnResetPressed()
        {
            var volumetricFogOptions = ScriptableObject.CreateInstance<VolumetricFogOptions>();
            FindObjectOfType<VolumetricFog>().fogOptions = volumetricFogOptions;
                        
            foreach (var option in GetComponentsInChildren<Option>(true))
            {
                option.CurrentOptions = volumetricFogOptions;
                option.Awake();
            }
        }
    }
}