using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPickerOption : MonoBehaviour
{
    [SerializeField] private ColorPicker _colorPicker;
    
    // Start is called before the first frame update
    void Start()
    {
        _colorPicker.onValueChanged.AddListener(color =>
        {
            print(color);
        });       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
