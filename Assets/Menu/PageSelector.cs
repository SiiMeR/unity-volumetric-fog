using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Menu
{
    public class PageSelector : MonoBehaviour
    {
        public List<GameObject> pages;

        private UICarousel _carousel;
        
        // Start is called before the first frame update
        void Awake()
        {
            pages.ForEach(page => page.SetActive(false));
            pages.FirstOrDefault()?.SetActive(true);
            
            _carousel = GetComponent<UICarousel>();
            
            var options = pages.Select((page, idx) => $"Page {idx + 1}").ToList();
            _carousel.SetOptions(options);

            _carousel.onValueChanged += newPage =>
            {
                pages.ForEach(page => page.SetActive(false));
                pages.Where((page, idx) => newPage == $"Page {idx + 1}")
                    .FirstOrDefault()?.SetActive(true);
            };
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
