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
    /// Handles selection of an option using a toggle slider
    /// </summary>
    [AddComponentMenu("LITHO/UI/Toggle Controller", -9480)]
    [RequireComponent(typeof(Slider))]
    public class ToggleController : OptionSlider<Text>
    {
        [SerializeField]
        [Tooltip("Text which repeats the current option on top of the slider handle")]
        private Text _handleText = null;

        protected override float BindedValue
        {
            get => slider != null ? slider.value : 0f;
            set
            {
                if (slider != null)
                {
                    slider.value = value;
                }
            }
        }
        protected override float BindedMinValue
        {
            get => slider != null ? slider.minValue : 0f;
            set
            {
                if (slider != null)
                {
                    slider.minValue = value;
                }
            }
        }
        protected override float BindedMaxValue
        {
            get => slider != null ? slider.maxValue : 0f;
            set
            {
                if (slider != null)
                {
                    slider.maxValue = value;
                }
            }
        }

        public override bool Interactable
        {
            get => slider != null && slider.interactable;
            set
            {
                if (slider != null)
                {
                    slider.interactable = value;
                }
            }
        }

        public string ValueName => _options[Value] != null ? _options[Value].text : "";

        private RectTransform rt;
        private Slider slider;
        private RectTransform handleSlideArea;


        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            slider = GetComponent<Slider>();

            _sliderUsesIntegers = true;
            _clickToScroll = true;
            _swipeSensitivity = 1f;
        }

        protected override void Update()
        {
            base.Update();

            // Force the slider to operate from left to right (other options not yet supported)
            slider.direction = Slider.Direction.LeftToRight;

            SetHandleText();
        }

        protected override bool ValidateElements()
        {
            rt = GetComponent<RectTransform>();
            slider = GetComponent<Slider>();

            if (base.ValidateElements() && rt != null && slider != null)
            {
                if (_handleText == null)
                {
                    foreach (Text text in GetComponentsInChildren<Text>())
                    {
                        if (text.gameObject.name == "HandleText")
                        {
                            _handleText = text;
                            break;
                        }
                    }
                }
                if (_handleText == null)
                {
                    Debug.LogError("Handle text not found on Toggle UI element;" +
                                   "cannot implement toggle control");
                    return false;
                }
                return true;
            }
            return false;
        }

        public override void SetLayoutHorizontal()
        {
            if (!ValidateElements())
            {
                return;
            }
            base.SetLayoutHorizontal();

            _drivenTransformTracker.Clear();

            if (slider != null)
            {
                float width = rt.sizeDelta.x;
                if (slider.fillRect != null)
                {
                    slider.fillRect.sizeDelta = new Vector2(width, slider.fillRect.sizeDelta.y);
                }
                if (slider.handleRect != null)
                {
                    _drivenTransformTracker.Add(this, slider.handleRect,
                                                DrivenTransformProperties.SizeDeltaX
                                                | DrivenTransformProperties.Scale
                                                | DrivenTransformProperties.Pivot
                                                | DrivenTransformProperties.Rotation);
                    slider.handleRect.localScale = new Vector3(1f, 1f, 1f);
                    slider.handleRect.pivot = new Vector2(0.5f, 0.5f);
                    slider.handleRect.localRotation = Quaternion.identity;

                    float handleWidth
                        = (width + _optionSpacer.spacing) / OptionCount - _optionSpacer.spacing;

                    slider.handleRect.sizeDelta = new Vector2(handleWidth,
                                                              slider.handleRect.sizeDelta.y);

                    // Update the RectTransform of the handle slide area
                    handleSlideArea = slider.handleRect.parent?.GetComponent<RectTransform>();
                    if (handleSlideArea != null)
                    {
                        _drivenTransformTracker.Add(this, handleSlideArea,
                                                    DrivenTransformProperties.SizeDeltaX
                                                    | DrivenTransformProperties.Scale
                                                    | DrivenTransformProperties.Pivot
                                                    | DrivenTransformProperties.Rotation);
                        handleSlideArea.localScale = new Vector3(1f, 1f, 1f);
                        handleSlideArea.pivot = new Vector2(0.5f, 0.5f);
                        handleSlideArea.localRotation = Quaternion.identity;

                        handleSlideArea.sizeDelta = new Vector2(
                            width - handleWidth - _optionSpacer.padding.left
                            - _optionSpacer.padding.right,
                            handleSlideArea.sizeDelta.y);
                    }
                }
            }
        }

        private void SetHandleText()
        {
            _handleText.text = ValueName;
        }
    }

}
