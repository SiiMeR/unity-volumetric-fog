using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class UICarousel : MonoBehaviour
    {
        [SerializeField] private List<string> _options;
        [SerializeField] private TextMeshProUGUI _currentText;

        [SerializeField] private GameObject _leftArrow;
        [SerializeField] private GameObject _rightArrow;
        
        private int _currentIndex;
        
        public void OnLeftArrowPressed()
        {
            var nextIndex = (int) Mathf.Repeat(--_currentIndex, _options.Count);
            SetCurrentlySelectedOption(nextIndex);
        }

        public void OnRightArrowPressed()
        {
            var prevIndex = (int) Mathf.Repeat(++_currentIndex, _options.Count);
            SetCurrentlySelectedOption(prevIndex);
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
