using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu
{
    public class ColorPickerOption : MonoBehaviour
    {
        [SerializeField] private ColorPicker _colorPicker;
        [SerializeField] private GameObject _hsvField;

        private CanvasGroup _hsvCanvas;
        // Start is called before the first frame update
        void Start()
        {
            _colorPicker.onValueChanged.AddListener(EventManager.FogColorChanged);             
            _hsvField.SetActive(false);
            _hsvCanvas = _hsvField.GetComponent<CanvasGroup>();
        }

        public void OnColorClicked()
        {
            if(!_hsvField.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_hsvCanvas.gameObject);
                Show();
            }
        }

        private Sequence Show()
        {
            return DOTween.Sequence()
                .AppendCallback(() => _hsvField.SetActive(true))
                .Append(_hsvCanvas.DOFade(1.0f, 0.4f));
        }
    }
}
