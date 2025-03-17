/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LITHO.UI
{

    /// <summary>
    /// Generates a device option UI for each device that presents itself to the app, allowing
    /// those devices to be connected to
    /// </summary>
    [AddComponentMenu("LITHO/UI/Device Select UI", -9399)]
    public class DeviceSelectUI : MonoBehaviour
    {
        private enum ConnectionState
        {
            NoneConnected = 0,  // Initial state/ disconnected/ failed
            Connecting,         // Connection requested, awaiting response
            Connected,          // Connection established
            Disconnecting,      // Disconnection requested, awaiting response
            SwitchingDevice,    // Disconnection requested, move into Connecting state afterwards
        }

        [SerializeField]
        [Tooltip("Prefab to show for each device that is found")]
        private GameObject _deviceOptionPrefab = null;

        [SerializeField]
        [Tooltip("Divider to show between connected device and other options")]
        private GameObject _separator = null;

        [SerializeField]
        [Tooltip("Transform in which to host device select buttons for found devices")]
        private RectTransform _content = null;

        private List<DeviceOptionUI> _deviceOptionUIs = new List<DeviceOptionUI>();

        private DeviceOptionUI _selectedOptionUI, _nextOptionUI;

        private ConnectionState state = ConnectionState.NoneConnected;

        private bool ButtonsActive
        {
            get
            {
                return state == ConnectionState.NoneConnected
                    || state == ConnectionState.Connected;
            }
        }

        private float _lastStateChangeTime;

        private bool _hasAutoconnected;

        private float _timeUntilAutoconnect;

        private const float AUTO_CONNECT_DELAY_PERIOD = 1.5f;

        private const float UPDATE_TAIL_PERIOD = 1.5f;


        private void Awake()
        {
            Litho.OnDeviceFound += HandleDeviceFound;
            Litho.OnConnected += HandleDeviceConnected;
            Litho.OnConnectionFailed += HandleConnectionFailed;
            Litho.OnDisconnected += HandleDeviceDisconnected;
        }

        private void Start()
        {
            _timeUntilAutoconnect = AUTO_CONNECT_DELAY_PERIOD;
        }

        private void Update()
        {
            _content.parent.parent.gameObject.SetActive(_content.childCount >= 1);
            _separator.SetActive(_selectedOptionUI != null
                                 && _selectedOptionUI.transform.parent == transform
                                 && _content.childCount >= 1);

            if (!_hasAutoconnected && !Litho.IsConnected
                && ButtonsActive && state == ConnectionState.NoneConnected
                && _deviceOptionUIs.Count > 0)
            {
                foreach (DeviceOptionUI deviceOptionUI in _deviceOptionUIs)
                {
                    // If this option is active and this happens to be the favourite device
                    if (deviceOptionUI.gameObject.activeInHierarchy
                        && deviceOptionUI.DeviceName == Litho.GetFavouriteDeviceName())
                    {
                        if (_timeUntilAutoconnect <= 0f)
                        {
                            _timeUntilAutoconnect = 0f;
                            // Fake a press of the connection button to automatically connect
                            deviceOptionUI.OnButtonClick.Invoke();
                            _hasAutoconnected = true;

                            Debug.LogFormat(
                                "Automatically connecting to favourite Litho device ({0})",
                                deviceOptionUI.DeviceName);
                        }
                        else
                        {
                            _timeUntilAutoconnect -= Time.unscaledDeltaTime;
                        }
                    }
                }
            }

            if (Time.unscaledTime < _lastStateChangeTime + UPDATE_TAIL_PERIOD)
            {
                // Force the UI layout to update soon
                LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            Litho.OnDeviceFound -= HandleDeviceFound;
            Litho.OnConnected -= HandleDeviceConnected;
            Litho.OnConnectionFailed -= HandleConnectionFailed;
            Litho.OnDisconnected -= HandleDeviceDisconnected;
        }

        private void ChangeState(ConnectionState newState)
        {
            switch (newState)
            {
                case ConnectionState.NoneConnected:
                    if (state == ConnectionState.Disconnecting
                        || state == ConnectionState.Connecting
                        || state == ConnectionState.SwitchingDevice)
                    {
                        SetButtonsInteractable(true);
                        Litho.StartDeviceSearch();
                    }
                    else
                    {
                        Debug.LogWarningFormat(
                            "Invalid state transition ({0} to {1})", state, newState);
                    }
                    break;
                case ConnectionState.Connecting:
                    if (state == ConnectionState.NoneConnected
                       || state == ConnectionState.Connected
                       || state == ConnectionState.SwitchingDevice)
                    {
                        SetButtonsInteractable(false);
                    }
                    else
                    {
                        Debug.LogWarningFormat(
                            "Invalid state transition ({0} to {1})", state, newState);
                    }
                    break;
                case ConnectionState.Connected:
                    if (state == ConnectionState.Connecting)
                    {
                        Litho.StopDeviceSearch();
                        SetButtonsInteractable(true);
                    }
                    else
                    {
                        Debug.LogWarningFormat(
                            "Invalid state transition ({0} to {1})", state, newState);
                    }
                    break;
                case ConnectionState.Disconnecting:
                    if (state == ConnectionState.Connected)
                    {
                        SetButtonsInteractable(false);
                    }
                    else
                    {
                        Debug.LogWarningFormat(
                            "Invalid state transition ({0} to {1})", state, newState);
                    }
                    break;
                case ConnectionState.SwitchingDevice:
                    if (state == ConnectionState.Connected)
                    {
                        SetButtonsInteractable(false);
                    }
                    else
                    {
                        Debug.LogWarningFormat(
                            "Invalid state transition ({0} to {1})", state, newState);
                    }
                    break;
            }
            // Update the current state
            Debug.LogFormat("State changed from {0} to {1}", state, newState);
            state = newState;

            _lastStateChangeTime = Time.unscaledTime;
        }

        private void HandleDeviceFound(string deviceName)
        {
            // Only proceed if this name has not been added to the list already
            for (int i = 0; i < _deviceOptionUIs.Count; i++)
            {
                if (deviceName == _deviceOptionUIs[i].DeviceName)
                {
                    return;
                }
            }
            // If this is the first device that was found
            if (_deviceOptionUIs.Count == 0 && _content.childCount > 0)
            {
                // Remove the placeholder option
                Destroy(_content.transform.GetChild(0).gameObject);
            }

            // Create the prefab in the list of devices
            DeviceOptionUI deviceOptionUI =
                Instantiate(_deviceOptionPrefab, _content).GetComponent<DeviceOptionUI>();
            // Add this device to the list of devices
            _deviceOptionUIs.Add(deviceOptionUI);
            // Set up the device option view
            deviceOptionUI.DeviceName = deviceName;
            deviceOptionUI.SetButtonInteractable(ButtonsActive);
            // Subscribe to respond to the button on this option
            deviceOptionUI.OnButtonClick.AddListener(() =>
            {
                HandleButtonClick(deviceOptionUI);
            });

            _lastStateChangeTime = Time.unscaledTime;

            Debug.Log("Litho device found: " + deviceName);
        }

        private void HandleDeviceConnected(string deviceName)
        {
            _hasAutoconnected = true;
            _selectedOptionUI.SetStatusConnected();
            ChangeState(ConnectionState.Connected);
            FindObjectOfType<TabManager>()?.SelectValueDelayed(3f, 1, 0);
        }

        private void HandleConnectionFailed(string deviceName)
        {
            _selectedOptionUI.SetStatusConnectionFailed();
            DeselectOptionUI();
            ChangeState(ConnectionState.NoneConnected);
        }

        private void HandleDeviceDisconnected(string deviceName)
        {
            // If a device was connected, and a new one was selected
            if (state == ConnectionState.SwitchingDevice)
            {
                _selectedOptionUI.SetStatusDisconnected();
                DeselectOptionUI();
                SelectOptionUI(_nextOptionUI);
                _selectedOptionUI.SetStatusConnecting();
                ChangeState(ConnectionState.Connecting);
                Litho.ConnectToDevice(_selectedOptionUI.DeviceName);
            }
            // If this was an involuntary disconnection
            else if (state == ConnectionState.Connected)
            {
                // Attempt to reconnect
                _selectedOptionUI.SetStatusReconnecting();
                ChangeState(ConnectionState.Connecting);
                Litho.ConnectToDevice(_selectedOptionUI.DeviceName);

            }
            // If a connected device was told to disconnect
            else if (state == ConnectionState.Disconnecting)
            {
                _selectedOptionUI.SetStatusDisconnected();
                DeselectOptionUI();
                ChangeState(ConnectionState.NoneConnected);
            }
            // Otherwise something strange has happened
            else
            {
                // Reset the state (and log erroneous transitions)
                ChangeState(ConnectionState.NoneConnected);
            }
        }

        private void HandleButtonClick(DeviceOptionUI deviceOptionUI)
        {
            if (state == ConnectionState.Connected)
            {
                // If the disconnect button was clicked
                if (deviceOptionUI.DeviceName == _selectedOptionUI.DeviceName)
                {
                    ChangeState(ConnectionState.Disconnecting);
                    Litho.DisconnectFromDevice();
                }
                else
                {
                    _nextOptionUI = deviceOptionUI;
                    _selectedOptionUI.SetStatusDisconnecting();
                    ChangeState(ConnectionState.SwitchingDevice);
                    Litho.DisconnectFromDevice();
                }
            }
            // Otherwise, this must have been a connect button
            else
            {
                SelectOptionUI(deviceOptionUI);
                _selectedOptionUI.SetStatusConnecting();
                ChangeState(ConnectionState.Connecting);
                Litho.ConnectToDevice(deviceOptionUI.DeviceName);
            }
        }

        private void SelectOptionUI(DeviceOptionUI deviceOptionUI)
        {
            if (deviceOptionUI != null)
            {
                _selectedOptionUI = deviceOptionUI;
                _selectedOptionUI.Reparent(transform);
            }
            else
            {
                Debug.LogWarning("Cannot select null device option UI");
            }
        }

        private void DeselectOptionUI()
        {
            if (_selectedOptionUI != null)
            {
                _selectedOptionUI.Reparent(_content);
                _selectedOptionUI = null;
            }
            else
            {
                Debug.LogWarning("There is no selected option UI to deselect");
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            foreach (DeviceOptionUI option in _deviceOptionUIs)
            {
                option.SetButtonInteractable(interactable);
            }
        }
    }

}
