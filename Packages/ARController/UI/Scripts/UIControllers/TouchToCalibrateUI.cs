/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.UI
{

    /// <summary>
    /// Handles Litho calibration as triggered by touching and holding the screen
    /// </summary>
    [AddComponentMenu("LITHO/UI/Touch To Calibrate UI")]
    public class TouchToCalibrateUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Overlay to fade in over the screen when touched")]
        private CanvasGroup _overlay = null;

        [SerializeField]
        [Tooltip("Progress bar used to indicate calibration progress")]
        private ProgressBar _progressBar = null;

        [SerializeField]
        [Tooltip("RectTransform used to indicate pitch offset")]
        private RectTransform _pitchNeedle = null;

        [SerializeField]
        [Tooltip("How long it should take for the overlay to fade in")]
        private float _fadeInTime = 0.5f;

        [SerializeField]
        [Tooltip("How long calibration position should be held before calibration occurs")]
        private float _calibrateTime = 0.5f;

        [SerializeField]
        [Tooltip("Period over which to fade overlay back out when calibration is cancelled")]
        private float _cancelTime = 0.25f;

        [SerializeField]
        [Tooltip("How much the pitch may be misaligned by before calibration is disallowed")]
        private float _pitchThreshold = 5f;

        [SerializeField]
        [Tooltip("How many pixels to move the pitch needle per degree of pitch")]
        private float _pitchSensitivity = 2f;

        [SerializeField]
        [Tooltip("Overlay to fade out when touched")]
        private CanvasGroup _hideOverlay = null;

        [SerializeField]
        [Tooltip("Size of areas on the edge of the touch area that should be ignored (note: top " +
                 "and bottom are swapped)")]
        protected RectOffset _safetyMargin = new RectOffset();

        private bool _hasCalibrated;


        private void OnValidate()
        {
            _fadeInTime = Mathf.Max(0.0001f, _fadeInTime);
            _calibrateTime = Mathf.Max(0.0001f, _calibrateTime);
        }

        private void Awake()
        {
            if (_overlay == null)
            {
                Debug.LogWarning("Overlay is not set on " + this + "; cannot perform calibration");
            }
            if (_progressBar == null)
            {
                Debug.LogWarning(
                    "Progress bar is not set on " + this + "; cannot perform calibration");
            }
        }

        private void Update()
        {
            if (_overlay != null && _progressBar != null)
            {
                float pitchMisalignment = Camera.main.transform.eulerAngles.x;
                if (pitchMisalignment >= 90f)
                {
                    pitchMisalignment -= 360f;
                }
                if (_pitchNeedle != null)
                {
                    _pitchNeedle.anchoredPosition = new Vector2(
                        _pitchNeedle.anchoredPosition.x, pitchMisalignment * _pitchSensitivity);
                }
                Rect overlayRect = _overlay.GetComponent<RectTransform>().rect;
                bool touchIsValid = false;
                for (int t = 0; t < Input.touchCount; t++)
                {
                    // TODO: Figure out why this inverts the padding
                    if (_safetyMargin.Remove(overlayRect).Contains(
                        _overlay.transform.InverseTransformPoint(Input.GetTouch(t).position)))
                    {
                        touchIsValid = true;
                        break;
                    }
                }
                if (!_hasCalibrated && touchIsValid)
                {
                    if (_overlay.alpha < 1f)
                    {
                        _overlay.alpha += Time.unscaledDeltaTime / _fadeInTime;
                    }
                    else
                    {
                        _overlay.alpha = 1f;
                        if (_pitchNeedle == null || Mathf.Abs(pitchMisalignment) < _pitchThreshold)
                        {
                            if (_progressBar.Progress < 1f)
                            {
                                _progressBar.Progress += Time.unscaledDeltaTime / _calibrateTime;
                            }
                            else
                            {
                                _progressBar.Progress = 1f;
                                Litho.Calibrate();
                                _hasCalibrated = true;
                            }
                        }
                        else
                        {
                            if (_progressBar.Progress > 0f)
                            {
                                _progressBar.Progress -= Time.unscaledDeltaTime * 0.5f
                                    / _calibrateTime;
                            }
                            else
                            {
                                _progressBar.Progress = 0f;
                            }
                        }
                    }
                }
                else
                {
                    if (!touchIsValid)
                    {
                        _hasCalibrated = false;
                    }
                    if (_overlay.alpha > 0f)
                    {
                        _overlay.alpha -= Time.unscaledDeltaTime / _cancelTime;
                    }
                    else
                    {
                        _overlay.alpha = 0f;
                        _progressBar.Progress = 0f;
                    }
                }
                if (_hideOverlay != null)
                {
                    _hideOverlay.alpha = (1f - _overlay.alpha) * (1f - _overlay.alpha);
                }
            }
        }
    }

}
