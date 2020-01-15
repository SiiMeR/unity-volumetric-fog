using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu
{
    public class ColorPickerOption : Option
    {
        [SerializeField] private ColorPicker _colorPicker;
        [SerializeField] private GameObject _hsvField;

        
        private CanvasGroup _hsvCanvas;
        // Start is called before the first frame update
        void Start()
        {  
            _hsvField.SetActive(false);
            _hsvCanvas = _hsvField.GetComponent<CanvasGroup>();
        }

        public void OnValueChanged(Color newValue)
        {
            CurrentOptions.GetType().GetField(targetOption).SetValue(CurrentOptions, newValue);
        }

        public override void Awake()
        {
            base.Awake();

            _colorPicker = GetComponentInChildren<ColorPicker>();
            _colorPicker.onValueChanged.AddListener(OnValueChanged);

            _colorPicker.CurrentColor = (Color) CurrentOptions.GetType().GetField(targetOption).GetValue(CurrentOptions);
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
