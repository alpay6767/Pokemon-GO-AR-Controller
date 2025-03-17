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
    /// Handles showing and hiding of a dropdown hamburger menu
    /// </summary>
    [AddComponentMenu("LITHO/UI/Dropdown Menu", -9500)]
    [RequireComponent(typeof(CanvasGroup))]
    public class DropdownMenu : ValueAnimator
    {
        [SerializeField]
        [Tooltip("Whether to hide this menu when the application starts in the Editor")]
        private bool _hideByDefaultInEditor = true;

        private bool _isOpen;

        [SerializeField]
        [Tooltip("Button which triggers opening the menu")]
        private Button _openButton = null;

        [SerializeField]
        [Tooltip("Button which triggers closing the menu")]
        private Button _closeButton = null;

        [SerializeField]
        [Tooltip("RectTransform which contains the content of the menu")]
        private RectTransform _content = null;

        [SerializeField]
        [Tooltip("CanvasGroup used to obscure the view of the scene in the background")]
        private CanvasGroup _background = null;

        [SerializeField]
        [Tooltip("CanvasGroup which contains content to show when the menu is closed")]
        private CanvasGroup _altContent = null;

        private float _openness = 1f;
        private float _startTop;
        private bool _isFirstOpening;


        private void Awake()
        {
            _softenAnimationFactor = 0.15f;

            // Ensure the menu is initially dropped down if set to be open
            _openness = 0f;

            if (!Application.isEditor || !_hideByDefaultInEditor)
            {
                Open();
                _isFirstOpening = true;
                if (_background != null)
                {
                    _background.alpha = 0f;
                }
            }
            else
            {
                OpenImmediately();
                Close();
            }

            _startTop = _content.offsetMax.y;

            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            // Subscribe to events
            Litho.OnConnected += HandleDeviceConnected;
            Litho.OnDisconnected += HandleDeviceDisconnected;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            Litho.OnConnected -= HandleDeviceConnected;
            Litho.OnDisconnected -= HandleDeviceDisconnected;
        }

        public void Open()
        {
            _isOpen = true;
            _openButton.interactable = false;
            _closeButton.interactable = Application.isEditor || Litho.IsConnected;
        }
        public void OpenImmediately()
        {
            Open();
            _openness = 1f;
        }

        public void Close()
        {
            _isOpen = false;
            _openButton.interactable = true;
            _closeButton.interactable = false;
        }
        public void CloseImmediately()
        {
            Close();
            _openness = 0f;
        }

        public void ToggleOpen()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        protected override void AnimateValue()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            float previousOpenness = _openness;
            _openness = GetNewValue(_isOpen ? 1f : 0f, _openness);

            if (Application.isPlaying)
            {
                if (_background != null)
                {
                    if (!_isFirstOpening)
                    {
                        _background.alpha = _openness;
                    }
                    else
                    {
                        _background.alpha = 0f;
                    }
                }
                _content.localPosition = new Vector2(
                    _content.localPosition.x, _startTop + _content.rect.height * (1 - _openness));

                if (_altContent != null)
                {
                    _altContent.alpha = 1f - _openness;
                }
            }
            else
            {
                if (_background != null)
                {
                    _background.alpha = 1f;
                }
                if (_altContent != null)
                {
                    _altContent.alpha = 1f;
                }
            }

            // Spin the button in and out of sight
            _openButton.transform.rotation = Quaternion.Euler(
                0, 0, _openness * 180f);
            _closeButton.transform.rotation = Quaternion.Euler(
                0, 0, _openness * 180f);
            // Scale the buttons in and out of sight
            _openButton.transform.localScale = Vector3.one * Mathf.Min(1, (1 - _openness) * 2);
            _closeButton.transform.localScale = Vector3.one * Mathf.Min(1, _openness * 2);

            // If this menu has just finished closing
            if (_openness < 0.01f && previousOpenness >= 0.01f)
            {
                // Deactivate content and background
                _content.gameObject.SetActive(false);
                _background.gameObject.SetActive(false);
            }
            // If this menu has just started opening
            else if (_openness >= 0.01f && previousOpenness < 0.01f)
            {
                // Activate content and background
                _content.gameObject.SetActive(true);
                _background.gameObject.SetActive(true);
            }
        }

        private void HandleDeviceConnected(string deviceName)
        {
            _closeButton.interactable = Application.isEditor || (Litho.IsConnected && _isOpen);
        }

        private void HandleDeviceDisconnected(string deviceName)
        {
            Open();
        }
    }

}
