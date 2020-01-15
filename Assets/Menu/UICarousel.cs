using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Menu
{
    public delegate void CarouselOptionChangedHandler(string newOption);
    public class UICarousel : MonoBehaviour
    {
        private List<string> _options;
        
        [SerializeField] private TextMeshProUGUI _currentText;

        [SerializeField] private GameObject _leftArrow;
        [SerializeField] private GameObject _rightArrow;
        
        private int _currentIndex;

        public event CarouselOptionChangedHandler onValueChanged;

        public void SetOptions(List<string> options)
        {
            _options = options;
            SetCurrentlySelectedOption(_currentIndex);
        }
        
        public void SetCurrentText(string text)
        {
            _currentText.SetText(text);
            _currentIndex = _options.IndexOf(text);
        }
        
        public void OnLeftArrowPressed()
        {
            var nextIndex = (int) Mathf.Repeat(--_currentIndex, _options.Count);
            SetCurrentlySelectedOption(nextIndex);
            onValueChanged?.Invoke(_options[nextIndex]);
        }

        public void OnRightArrowPressed()
        {
            var prevIndex = (int) Mathf.Repeat(++_currentIndex, _options.Count);
            SetCurrentlySelectedOption(prevIndex);
            onValueChanged?.Invoke(_options[prevIndex]);
        }

        public void SetCurrentlySelectedOption(int index)
        {
            _currentText.text = _options[index];
        }

        private void Awake()
        {
            SetCurrentlySelectedOption(_currentIndex);
        }
    }
}
