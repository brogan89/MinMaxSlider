using System.Collections.Generic;
using Min_Max_Slider;
using UnityEngine;

public class HandleTextOverride : MonoBehaviour
{
    [SerializeField] private MinMaxSlider _minMaxSlider;

    private readonly Dictionary<int, string> _database = new()
    {
        [1] = "One",
        [2] = "Two",
        [3] = "Three",
        [4] = "Four",
        [5] = "Five",
    };
    
    private void Start()
    {
        _minMaxSlider.handleTextOverride = HandleTextOverrideCallback;
        _minMaxSlider.UpdateText();
    }

    private string HandleTextOverrideCallback(MinMaxSlider.HandleType handle, float value)
    {
        var valInt = Mathf.RoundToInt(value);

        if (_database.TryGetValue(valInt, out var strVal))
        {
            if (handle == MinMaxSlider.HandleType.Min)
                return $"Left - {strVal}";
            if (handle == MinMaxSlider.HandleType.Max)
                return $"Right - {strVal}";
        }
            
        return "Nil";
    }
}
