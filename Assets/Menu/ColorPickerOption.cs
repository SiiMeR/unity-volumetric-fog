using DG.Tweening;
using UnityEngine;

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
            if (_hsvField.activeInHierarchy)
            {
                DOTween.Sequence()
                    .Append(_hsvCanvas.DOFade(0.0f, 0.2f))
                    .AppendCallback(() => _hsvField.SetActive(!_hsvField.activeInHierarchy));
            }
            else
            {
                DOTween.Sequence()
                    .AppendCallback(() => _hsvField.SetActive(!_hsvField.activeInHierarchy))
                    .Append(_hsvCanvas.DOFade(1.0f, 0.4f));
            }

        }
        
        // Update is called once per frame
        void Update()
        {
        }
    }
}
