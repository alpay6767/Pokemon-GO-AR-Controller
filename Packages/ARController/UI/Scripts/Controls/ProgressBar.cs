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
    /// Sets the size of a child object proportionally to its parent based on a progress value
    /// </summary>
    [AddComponentMenu("LITHO/UI/Progress Bar", -9479)]
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Text element in which to display progress numerically")]
        private Text _valueText = null;

        [SerializeField]
        [Tooltip("Transform which spatially indicates progress")]
        private RectTransform _fillLevel = null;

        [SerializeField]
        [Tooltip("Direction in which to fill the progress bar")]
        private Slider.Direction _direction = Slider.Direction.LeftToRight;

        [SerializeField]
        [Tooltip("Current progress displayed by this progress bar")]
        [Range(0f, 1f)]
        private float _progress;
        public float Progress
        {
            get { return _progress; }
            set { SetProgress(value); }
        }


        private void Start()
        {
            SetProgress(_progress);
        }

        private void SetProgress(float newProgress)
        {
            _progress = Mathf.Max(0f, Mathf.Min(1f, newProgress));
            if (_valueText != null)
            {
                _valueText.text = ((int)(_progress * 100f)).ToString() + "%";
            }
            if (_fillLevel != null)
            {
                // Reset the fill level to account for direction changes
                _fillLevel.SetInsetAndSizeFromParentEdge(
                    RectTransform.Edge.Top, 0f,
                    ((RectTransform)_fillLevel.parent.transform).rect.height);
                _fillLevel.SetInsetAndSizeFromParentEdge(
                    RectTransform.Edge.Left, 0f,
                    ((RectTransform)_fillLevel.parent.transform).rect.width);
                // Resize the fill level to reflect the progress
                RectTransform.Edge edge
                    = _direction == Slider.Direction.LeftToRight ? RectTransform.Edge.Left
                    : _direction == Slider.Direction.RightToLeft ? RectTransform.Edge.Right
                    : _direction == Slider.Direction.TopToBottom ? RectTransform.Edge.Top
                    : RectTransform.Edge.Bottom;
                float distance = (_direction == Slider.Direction.LeftToRight
                                  || _direction == Slider.Direction.RightToLeft)
                    ? ((RectTransform)_fillLevel.parent.transform).rect.width
                    : ((RectTransform)_fillLevel.parent.transform).rect.height;
                _fillLevel.SetInsetAndSizeFromParentEdge(edge, 0f, distance * _progress);
            }
        }
    }

}
