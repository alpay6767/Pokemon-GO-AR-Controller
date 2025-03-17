/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace LITHO.UI
{

    [System.Serializable]
    public class OptionChangedEvent : UnityEvent<int> { }

    /// <summary>
    /// Handles animation and locking of sliding UI elements
    /// </summary>
    public abstract class OptionSlider<T> : ValueAnimator, 
        IPointerDownHandler, IDragHandler, IPointerUpHandler, ILayoutSelfController
    {
        [SerializeField]
        [Tooltip("LayoutGroup which contains the options for this slider")]
        protected HorizontalOrVerticalLayoutGroup _optionSpacer = null;

        [SerializeField]
        [Tooltip("Maximum number of options that can be swiped through with a single gesture")]
        protected int _maxSwipeDelta = 1;

        public int Value
        {
            get
            {
                return _forcedValue < 0 ? IntervalValue : _forcedValue;
            }
            private set
            {
                _forcedValue = value;
            }
        }

        protected int IntervalValue
        {
            get
            {
                return (int)Mathf.Round(ScaledValue);
            }
        }

        protected float ScaledValue
        {
            get
            {
                return Mathf.Clamp(BindedValue * ScaleFactor, 0, OptionCount);
            }
        }

        private float ScaleFactor
        {
            get
            {
                return (_sliderUsesIntegers || OptionCount <= 1) ? 1f : (OptionCount - 1);
            }
        }

        protected abstract float BindedValue { get; set; }

        private float bindedMinValue;
        protected virtual float BindedMinValue
        {
            get
            {
                return bindedMinValue;
            }
            set
            {
                bindedMinValue = value;
            }
        }

        private float bindedMaxValue;
        protected virtual float BindedMaxValue
        {
            get
            {
                return bindedMaxValue;
            }
            set
            {
                bindedMaxValue = value;
            }
        }

        private bool _interactable;
        public virtual bool Interactable
        {
            get
            {
                return _interactable;
            }
            set
            {
                _interactable = value;
            }
        }

        public OptionChangedEvent OnValueChanged = new OptionChangedEvent();

        protected List<T> _options = new List<T>();

        protected bool _sliderUsesIntegers = false;
        protected bool _clickToScroll = false;
        protected float _swipeSensitivity;

        private bool _isHolding, _wasHolding;
        protected int OptionCount { get; private set; } = 1;

        private float _clickStartValue = -1;
        private int _forcedValue = -1;
        private float _pointerDownTime;

        private int _delayedGoToValue = -1, _delayedConditionalValue = -1;
        private float _delayedGoToTime;
        private bool _delayedGoToSuppressEvent;

        protected DrivenRectTransformTracker _drivenTransformTracker
            = new DrivenRectTransformTracker();

        private const float TAP_TIME_THRESHOLD = 0.15f;


        private void Start()
        {
            if (!ValidateElements())
            {
                return;
            }
        }

        protected override void Update()
        {
            if (transform.hasChanged)
            {
                if (!ValidateElements())
                {
                    return;
                }
                transform.hasChanged = false;
            }
            // If this is the frame after a pointer down event
            if (_isHolding && !_wasHolding)
            {
                // Update the forced value to whatever has been enforced by the UI control
                _forcedValue = IntervalValue;
            }

            base.Update();

            _wasHolding = _isHolding;

            // Cancel any delayed conditional value changes if the value condition has been broken
            if (_delayedConditionalValue >= 0 && Value != _delayedConditionalValue)
            {
                _delayedGoToValue = -1;
                _delayedConditionalValue = -1;
                _delayedGoToTime = 0f;
            }
            else if (_delayedGoToValue >= 0 && Time.time >= _delayedGoToTime)
            {
                SelectValue(
                    _delayedGoToValue, _delayedConditionalValue, _delayedGoToSuppressEvent);
            }
        }


        private void OnDisable()
        {
            _drivenTransformTracker.Clear();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable || OptionCount <= 1)
            {
                return;
            }

            _pointerDownTime = Time.unscaledTime;
            _isHolding = true;
            _clickStartValue = IntervalValue;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Interactable || OptionCount <= 1)
            {
                return;
            }

            _forcedValue = IntervalValue;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Interactable)
            {
                return;
            }

            if (OptionCount > 1)
            {
                _isHolding = false;
                float touchTime = Time.unscaledTime - _pointerDownTime;
                // If click-to-scroll is enabled
                // and this is a tap
                // and there is a valid click start value
                // and the click start value equals the resolved click position value
                if (_clickToScroll
                    && !eventData.dragging && touchTime < TAP_TIME_THRESHOLD
                    && _clickStartValue > -1 && Mathf.Abs(_clickStartValue - _forcedValue)
                        < Litho.EPSILON)
                {
                    // Move to the next value
                    _forcedValue = (int)(Mathf.Round(_clickStartValue) + 1) % OptionCount;
                }
                else if (eventData.dragging)
                {
                    float swipeDelta = (BindedValue - (_clickStartValue / ScaleFactor))
                        * _swipeSensitivity / touchTime;
                    swipeDelta = (int)Mathf.Round(
                        Mathf.Clamp(swipeDelta, -_maxSwipeDelta, _maxSwipeDelta));
                    _forcedValue = (int)Mathf.Clamp(_forcedValue + swipeDelta, 0, OptionCount - 1);
                }

                if (Mathf.Abs(Value - _clickStartValue) > Litho.EPSILON)
                {
                    OnValueChanged?.Invoke(Value);
                }
            }
            else
            {
                BindedValue = 0;
            }
            _clickStartValue = -1;
            ResetAnimation();
        }

        public virtual void SetLayoutHorizontal()
        {
            if (!ValidateElements())
            {
                return;
            }
            ((ILayoutGroup)_optionSpacer).SetLayoutHorizontal();
        }

        public virtual void SetLayoutVertical()
        {
            if (!ValidateElements())
            {
                return;
            }
            ((ILayoutGroup)_optionSpacer).SetLayoutVertical();
        }

        protected virtual bool ValidateElements()
        {
            if (_optionSpacer == null)
            {
                _optionSpacer = GetComponentInChildren<HorizontalOrVerticalLayoutGroup>();
            }
            if (_optionSpacer == null)
            {
                Debug.LogError("Option spacer not found on Toggle UI element;" +
                               "cannot implement toggle control");
                return false;
            }
            // Ensure changes to the list of options are accounted for
            _options = new List<T>();
            for (int c = 0; c < _optionSpacer.transform.childCount; c++)
            {
                if (_optionSpacer.transform.GetChild(c).gameObject.activeSelf)
                {
                    _options.Add(_optionSpacer.transform.GetChild(c).GetComponent<T>());
                }
            }
            OptionCount = Mathf.Max(1, _options.Count);
            BindedMinValue = 0;
            BindedMaxValue = _sliderUsesIntegers ? (OptionCount - 1) : 1f;

            return true;
        }

        protected override void AnimateValue()
        {
            // If not dragging the slider
            if (!ValidateElements() || _isHolding)
            {
                return;
            }

            if (OptionCount > 1)
            {
                bool complete = false;
                BindedValue = GetNewValue(Value / ScaleFactor, BindedValue, ref complete);
                if (complete)
                {
                    _forcedValue = -1;
                }
            }
            else
            {
                BindedValue = 0;
            }
        }

        // Moves to newValue (only if currently on conditionalValue, if set)
        public void SelectValue(int newValue, int conditionalValue = -1, bool suppressEvent = false)
        {
            float startValue = Value;
            if (conditionalValue < 0 || Mathf.Abs(startValue - conditionalValue) < Litho.EPSILON)
            {
                Value = newValue;
                if (!suppressEvent && Mathf.Abs(Value - startValue) > Litho.EPSILON)
                {
                    OnValueChanged?.Invoke(Value);
                }
            }
        }

        // Moves to newValue (only if currently on conditionalValue, if set)
        public void SelectValueDelayed(
            float delay, int newValue, int conditionalValue = -1, bool suppressEvent = false)
        {
            _delayedGoToTime = Time.time + delay;
            _delayedGoToValue = newValue;
            _delayedConditionalValue = conditionalValue;
            _delayedGoToSuppressEvent = suppressEvent;
        }
    }

}
