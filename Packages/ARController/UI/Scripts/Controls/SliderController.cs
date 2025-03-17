/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////
using UnityEngine;
using UnityEngine.UI;

namespace LITHO.UI
{

    /// <summary>
    /// Remaps slider values and updates text objects with Slider values
    /// </summary>
    [AddComponentMenu("LITHO/UI/Slider Controller", -9479)]
    [RequireComponent(typeof(Slider))]
    public class SliderController : MonoBehaviour
    {
        private enum MappingMode
        {
            Linear = 0,
            Exponential
        }

        [SerializeField]
        private float _minValue;

        [SerializeField]
        private float _maxValue = 1f;

        [SerializeField]
        private MappingMode _mappingMode = MappingMode.Linear;

        [SerializeField]
        [Tooltip("Text which repeats the current value of the slider")]
        private Text _valueText = null;

        [SerializeField]
        [Tooltip("Text which displays the min value of the slider")]
        private Text _minValueText = null;

        [SerializeField]
        [Tooltip("Text which displays the max value of the slider")]
        private Text _maxValueText = null;

        private float _minInput, _maxInput;

        private Slider _slider;

        public Slider.SliderEvent OnValueChanged = new Slider.SliderEvent();

        public float Value
        {
            get
            {
                return MapValue(_slider.value);
            }
            set
            {
                float inverseMappedValue = InverseMapValue(value);
                if (Mathf.Abs(_slider.value - inverseMappedValue) > Litho.EPSILON)
                {
                    _slider.value = inverseMappedValue;
                    SetTextToValue(_valueText, value);
                }
            }
        }


        private void OnValidate()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;

            switch (_mappingMode)
            {
                case MappingMode.Linear:
                    _minInput = _minValue;
                    _maxInput = _maxValue;
                    break;
                case MappingMode.Exponential:
                    _minValue = Mathf.Max(Litho.EPSILON, _minValue);
                    _maxValue = Mathf.Max(Litho.EPSILON, _maxValue);
                    _minInput = Mathf.Log(_minValue);
                    _maxInput = Mathf.Log(_maxValue);
                    break;
            }

            SetTextToValue(_valueText, MapValue(_slider.value));
            SetTextToValue(_minValueText, _minValue);
            SetTextToValue(_maxValueText, _maxValue);
        }

        private void Awake()
        {
            OnValidate();
            _slider.onValueChanged.AddListener(HandleValueChanged);
        }

        private void HandleValueChanged(float sliderValue)
        {
            float mappedValue = MapValue(sliderValue);
            OnValueChanged?.Invoke(mappedValue);
            SetTextToValue(_valueText, mappedValue);
        }

        private float MapValue(float input)
        {
            switch (_mappingMode)
            {
                case MappingMode.Linear:
                    return Mathf.Lerp(_minValue, _maxValue, input);
                case MappingMode.Exponential:
                    return Mathf.Exp(Mathf.Lerp(_minInput, _maxInput, input));
            }
            return input;
        }

        private float InverseMapValue(float value)
        {
            switch (_mappingMode)
            {
                case MappingMode.Linear:
                    return Mathf.InverseLerp(_minInput, _maxInput, value);
                case MappingMode.Exponential:
                    return Mathf.InverseLerp(_minInput, _maxInput, Mathf.Log(value));
            }
            return value;
        }

        private void SetTextToValue(Text textObject, float value)
        {
            if (textObject != null)
            {
                textObject.text = value.ToString("F2");
            }
        }
    }

}
