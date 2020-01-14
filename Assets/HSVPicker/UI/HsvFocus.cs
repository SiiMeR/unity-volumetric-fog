using System;
using DG.Tweening;
using Menu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HSVPicker.UI
{
    public class HsvFocus : MonoBehaviour, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private bool _isMouseOver;

        private void OnEnable()
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!_isMouseOver)
            {
                DOTween.Sequence()
                    .Append(GetComponent<CanvasGroup>().DOFade(0.0f, 0.2f))
                    .AppendCallback(() => gameObject.SetActive(false));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isMouseOver = true;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isMouseOver = false;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
