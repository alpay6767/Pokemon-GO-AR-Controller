/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace LITHO.UI
{

    /// <summary>
    /// Manages layout and scrolling of a set of tabs
    /// </summary>
    [AddComponentMenu("LITHO/UI/Tab Manager", -9499)]
    [RequireComponent(typeof(ScrollRect))]
    public class TabManager : OptionSlider<RectTransform>
    {
        [SerializeField]
        [Tooltip("Prefab to use to show number of tabs and currently selected tab")]
        private GameObject _tabIndicatorPrefab = null;

        [SerializeField]
        [Tooltip("Transform which will contain tab indicators in the UI")]
        private Transform _tabIndicatorSpacer = null;

        [SerializeField]
        [Tooltip("Colour a tab indicator should be when the corresponding tab is selected")]
        private Color _indicateSelected = new Color(1f, 1f, 1f, 1f);

        [SerializeField]
        [Tooltip("Colour a tab indicator should be when the corresponding tab is hidden")]
        private Color _indicateHidden = new Color(0.7176f, 0.7176f, 0.7176f, 0.5f);

        private List<Image> _tabIndicators = new List<Image>();

        protected override float BindedValue
        {
            get => _scrollbar != null ? _scrollbar.value : 0;
            set
            {
                if (_scrollbar != null)
                {
                    _scrollbar.value = value;
                }
            }
        }

        protected override float BindedMinValue => 0f;

        protected override float BindedMaxValue => 1f;

        public override bool Interactable
        {
            get => _scrollbar != null && _scrollbar.interactable;
            set
            {
                if (_scrollbar != null)
                {
                    _scrollbar.interactable = value;
                }
            }
        }

        private ScrollRect _scrollRect;
        private Scrollbar _scrollbar;


        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            if (_scrollRect != null)
            {
                _scrollbar = _scrollRect.horizontalScrollbar;
            }

            _sliderUsesIntegers = false;
            _clickToScroll = false;
            _swipeSensitivity = 6;

            // Account for notches in the screen by resizing this tab view if necessary; the Litho
            // UI assumes portrait use only, as this is the most comfortable way to hold a
            // smartphone in one hand.

            float safeMarginTop = Screen.height - Screen.safeArea.yMax;
            float safeMarginBottom = Screen.safeArea.yMin;

            RectTransform rect = GetComponent<RectTransform>();
            rect.offsetMax = new Vector2(
                rect.offsetMax.x, Mathf.Min(rect.offsetMax.y, -safeMarginTop));
            rect.offsetMin = new Vector2(
                rect.offsetMin.x, Mathf.Max(rect.offsetMin.y, safeMarginBottom));
        }

        public override void SetLayoutHorizontal()
        {
            if (!ValidateElements())
            {
                return;
            }

            base.SetLayoutHorizontal();
            // Set the content width to leave space for all of the tabs
            _scrollRect.content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, LithoUI.BASELINE_SCREEN_WIDTH * OptionCount);

            for (int c = 0; c < _scrollRect.content.childCount; c++)
            {
                _scrollRect.content.GetChild(c).GetComponent<RectTransform>()
                           .SetSizeWithCurrentAnchors(
                               RectTransform.Axis.Horizontal,
                               LithoUI.BASELINE_SCREEN_WIDTH - _optionSpacer.spacing);
            }

            _drivenTransformTracker.Clear();
            _drivenTransformTracker.Add(this, _scrollRect.content,
                                        DrivenTransformProperties.SizeDeltaX);
        }

        protected override bool ValidateElements()
        {
            _scrollRect = GetComponent<ScrollRect>();

            if (base.ValidateElements() && _scrollRect != null
                && _tabIndicatorSpacer != null && _tabIndicators != null)
            {
                _scrollbar = _scrollRect.horizontalScrollbar;
                _scrollbar.numberOfSteps = 0;

                if (Application.isPlaying)
                {
                    // Ensure the number of tab indicators matches the number of tabs
                    // (one frame per change)
                    if (_tabIndicatorSpacer.childCount > OptionCount)
                    {
                        Transform child = _tabIndicatorSpacer.GetChild(0);
                        if (child != null)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                    else if (_tabIndicatorSpacer.childCount < OptionCount)
                    {
                        // Create a new tab indicator
                        Instantiate(_tabIndicatorPrefab, _tabIndicatorSpacer);
                    }
                }
                if (_tabIndicators.Count != _tabIndicatorSpacer.childCount)
                {
                    _tabIndicators = new List<Image>(
                        _tabIndicatorSpacer.GetComponentsInChildren<Image>());
                    LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
                }
                return true;
            }
            return false;
        }

        protected override void AnimateValue()
        {
            base.AnimateValue();

            if (Application.isPlaying)
            {
                for (int i = 0; i < _tabIndicators.Count; i++)
                {
                    _tabIndicators[i].color = Color.Lerp(
                        _indicateSelected, _indicateHidden,
                        Mathf.Max(0, Mathf.Min(1, Mathf.Abs(i - ScaledValue))));
                }
            }
        }

        public void GoToNextTab()
        {
            SelectValue(Value + 1);
        }
    }

}
