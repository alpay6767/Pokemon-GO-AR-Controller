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
    /// Controller for a collection of UI elements representing a device connection button
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DeviceOptionUI : MonoBehaviour
    {
        private enum State
        {
            NotConnected = 0,
            Connecting,
            ConnectedAnimated,
            Connected,
            Disconnecting,
            DisconnectedAnimated,
            Disconnected,
            Reconnecting,
            ConnectionFailedAnimated,
            ConnectionFailed,
            SearchTemplate,
        }

        [SerializeField]
        [Tooltip("Text element that indicates the name of the represented Litho device")]
        private Text _deviceNameText = null;

        [SerializeField]
        [Tooltip("Button to allow connection and disconnection")]
        private Button _button = null, _darkButton = null;

        [SerializeField]
        [Tooltip("Whether this option is the 'searching' template shown until actual devices are " +
                 "discovered")]
        private bool _isSearchTemplate = false;

        private string StatusText
        {
            get
            {
                if (_buttonText != null)
                {
                    return _buttonText.text;
                }
                return "";
            }
            set
            {
                if (_buttonText != null)
                {
                    _buttonText.text = value;
                    _darkButtonText.text = value;
                }
            }
        }

        private float Alpha
        {
            get
            {
                return _buttonText?.color.a ?? 0.0f;
            }
            set
            {
                if (_buttonText != null)
                {
                    _buttonText.color = new Color(_buttonText.color.r,
                                                  _buttonText.color.g,
                                                  _buttonText.color.b,
                                                  value);
                    _darkButtonText.color = new Color(_darkButtonText.color.r,
                                                      _darkButtonText.color.g,
                                                      _darkButtonText.color.b,
                                                      value);
                }
            }
        }

        public string DeviceName
        {
            get
            {
                return _deviceNameText?.text ?? "";
            }
            set
            {
                if (_deviceNameText != null)
                {
                    _deviceNameText.text = value;
                    name = value.Replace(" ", "") + "OptionDiv";
                }
            }
        }

        private State _state;

        private bool ShouldBeDark
            => _state == State.ConnectedAnimated
            || _state == State.Connecting
            || _state == State.Reconnecting;

        // Animation variables
        private bool _hidden = true, _shouldHide;
        private Transform _targetParent;

        public Button.ButtonClickedEvent OnButtonClick;

        private Text _buttonText, _darkButtonText;

        private CanvasGroup _canvasGroup;

        private float _blinkPeriod = 1f, _blinkProgress;
        private int _blinkCycles = 0;


        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            // Start with a light button
            _darkButton.gameObject.SetActive(false);

            _buttonText = _button.GetComponentInChildren<Text>();
            _darkButtonText = _darkButton.GetComponentInChildren<Text>();

            _shouldHide = false;

            if (_isSearchTemplate)
            {
                _state = State.SearchTemplate;
                if (FindObjectOfType<Litho>() != null)
                {
                    if (Litho.CAN_CONNECT)
                    {
                        DeviceName = "PLEASE WAIT";
                        StatusText = "SEARCHING";
                        SetUpTextCycle(1.5f, -1);
                    }
                    else
                    {
                        DeviceName = "NO LITHOS";
                        StatusText = "UNSUPPORTED";
                        SetUpTextCycle(1f, 0);
                    }
                }
                else
                {
                    DeviceName = "LITHO SCRIPT";
                    StatusText = "NOT FOUND";
                    SetUpTextCycle(1f, 0);
                }
                SetButtonInteractable(false);
                _hidden = false;
            }
            else
            {
                _button.onClick.AddListener(OnButtonClick.Invoke);
                _darkButton.onClick.AddListener(OnButtonClick.Invoke);

                transform.localScale = new Vector3(1f, 0f, 1f);
                _canvasGroup.alpha = 0f;
                _hidden = true;
            }
        }

        private void Update()
        {
            if (_hidden != _shouldHide)
            {
                if (_shouldHide)
                {
                    _canvasGroup.alpha = _canvasGroup.alpha * 0.9f;
                    if (_canvasGroup.alpha < 0.05f)
                    {
                        transform.localScale = new Vector3(
                            1f, transform.localScale.y * 0.8f, 1f);
                        if (transform.localScale.y < 0.05f)
                        {
                            transform.localScale = new Vector3(1f, 0f, 1f);
                            _canvasGroup.alpha = 0f;
                            _hidden = true;
                        }
                    }
                }
                else
                {
                    transform.localScale = new Vector3(
                        1f, transform.localScale.y * 0.8f + 0.2f, 1f);
                    if (transform.localScale.y > 0.9f)
                    {
                        _canvasGroup.alpha = _canvasGroup.alpha * 0.9f + 0.1f;
                        if (_canvasGroup.alpha > 0.95f)
                        {
                            transform.localScale = Vector3.one;
                            _canvasGroup.alpha = 1f;
                            _hidden = false;
                        }
                    }
                }
                LayoutRebuilder.MarkLayoutForRebuild(
                    transform.parent.GetComponent<RectTransform>());
            }
            else if (_targetParent != null)
            {
                if (_targetParent == transform.parent)
                {
                    _targetParent = null;
                    _shouldHide = false;
                    return;
                }
                if (_hidden)
                {
                    transform.SetParent(_targetParent);
                    transform.SetAsFirstSibling();
                    UpdateButtonColour();
                    _shouldHide = false;
                }
                else
                {
                    _shouldHide = true;
                }
                LayoutRebuilder.MarkLayoutForRebuild(
                    transform.parent.GetComponent<RectTransform>());
            }

            // Update the text cycle
            bool cycleComplete = !_hidden && !_shouldHide && RunTextCycle();

            switch (_state)
            {
                case State.ConnectedAnimated:
                    if (cycleComplete)
                    {
                        SetStatusConnectedAnimationComplete();
                    }
                    break;
                case State.DisconnectedAnimated:
                    if (cycleComplete)
                    {
                        SetStatusDisconnectedAnimationComplete();
                    }
                    break;
                case State.ConnectionFailedAnimated:
                    if (cycleComplete)
                    {
                        SetStatusConnectionFailedAnimationComplete();
                    }
                    break;
            }
        }

        public void Reparent(Transform newParent)
        {
            _targetParent = newParent;
        }

        public void SetButtonInteractable(bool interactable)
        {
            _button.interactable = interactable;
            _darkButton.interactable = interactable;
        }

        private void UpdateButtonColour()
        {
            _button.gameObject.SetActive(!ShouldBeDark);
            _darkButton.gameObject.SetActive(ShouldBeDark);
        }

        public void SetStatusConnecting()
        {
            _state = State.Connecting;
            StatusText = "CONNECTING";
            SetUpTextCycle(1.5f, -1);
        }
        public void SetStatusDisconnecting()
        {
            _state = State.Disconnecting;
            StatusText = "DISCONNECTING";
            SetUpTextCycle(1.5f, -1);
        }
        public void SetStatusReconnecting()
        {
            _state = State.Reconnecting;
            StatusText = "RECONNECTING";
            SetUpTextCycle(1.5f, -1);
        }
        public void SetStatusConnected()
        {
            _state = State.ConnectedAnimated;
            StatusText = "CONNECTED";
            SetUpTextCycle(0.6f, 3);
        }
        public void SetStatusDisconnected()
        {
            _state = State.DisconnectedAnimated;
            StatusText = "DISCONNECTED";
            SetUpTextCycle(0.6f, 3);
        }
        public void SetStatusConnectionFailed()
        {
            _state = State.ConnectionFailedAnimated;
            StatusText = "FAILED";
            SetUpTextCycle(0.6f, 3);
        }
        private void SetStatusConnectedAnimationComplete()
        {
            _state = State.Connected;
            StatusText = "DISCONNECT";
            SetUpTextCycle(1f, 0);
        }
        private void SetStatusDisconnectedAnimationComplete()
        {
            _state = State.Disconnected;
            StatusText = "RECONNECT";
            SetUpTextCycle(1f, 0);
        }
        private void SetStatusConnectionFailedAnimationComplete()
        {
            _state = State.ConnectionFailed;
            StatusText = "RETRY";
            SetUpTextCycle(1f, 0);
        }

        private void SetUpTextCycle(float period, int cycles)
        {
            Alpha = 0f;
            _blinkPeriod = period;
            _blinkCycles = cycles;
            // Align the cycle with any previous cycle, whilst matching cycle count
            if (_blinkProgress > Mathf.PI)
            {
                _blinkProgress -= 2 * Mathf.PI;
            }
        }

        // Returns whether the cycle is complete
        private bool RunTextCycle()
        {
            float increment = 2 * Mathf.PI * Time.deltaTime / _blinkPeriod;

            if (_blinkCycles != 0)
            {
                Alpha = Mathf.Sin(_blinkProgress);
                _blinkProgress += increment;
                if (_blinkProgress > 2 * Mathf.PI)
                {
                    _blinkProgress -= 2 * Mathf.PI;
                    _blinkCycles--;
                }
                return false;
            }
            else
            {
                Alpha = Alpha * 0.95f + 0.05f;
                return true;
            }
        }
    }

}
