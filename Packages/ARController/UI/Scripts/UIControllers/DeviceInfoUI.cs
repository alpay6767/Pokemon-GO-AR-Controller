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
    /// Presents device information for the currently connected Litho device
    /// </summary>
    [AddComponentMenu("LITHO/UI/Device Info UI", -9398)]
    public class DeviceInfoUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Text element that indicates the name of the connected Litho device")]
        private Text _deviceNameText = null;

        [SerializeField]
        [Tooltip("Image whose colour indicates whether this Litho device is the favourite")]
        private Image _favouriteIndicator = null;

        [SerializeField]
        [Tooltip("Rect transform that indicates battery level")]
        private RectTransform _batteryLevelIndicator = null;

        [SerializeField]
        [Tooltip("Colour to use to indicate failed connection")]
        private Color _colourBad = Color.red;

        private Color _colourOkay;

        [SerializeField]
        [Tooltip("Colour to use to indicate this device is favourited")]
        private Color _colourFavourite = Color.yellow;

        private const string NO_DEVICE_NAME = "NO LITHO";


        private void Awake()
        {
            Litho.OnConnected += HandleDeviceConnected;
            Litho.OnDisconnected += HandleDeviceDisconnected;
            Litho.OnFavouriteDeviceChanged += HandleFavouriteDeviceChanged;
            Litho.OnBatteryLevelReceived += SetBatteryIndicator;

            if (_deviceNameText == null)
            {
                Debug.LogWarning("Device name text object is not set");
            }

            // Listen for favouriting and unfavouriting devices
            if (_favouriteIndicator == null)
            {
                Debug.LogWarning("Favourite device indicator object is not set");
            }

            // Listen for battery level updates, if applicable
            if (_batteryLevelIndicator != null)
            {
                _colourOkay = _batteryLevelIndicator.GetComponent<Image>()?.color ?? Color.black;
            }
            else
            {
                Debug.LogWarning("Battery indicator object is not set");
            }
        }

        private void OnDestroy()
        {
            Litho.OnConnected -= HandleDeviceConnected;
            Litho.OnDisconnected -= HandleDeviceDisconnected;
            Litho.OnFavouriteDeviceChanged -= HandleFavouriteDeviceChanged;
            Litho.OnBatteryLevelReceived -= SetBatteryIndicator;
        }

        private void HandleDeviceConnected(string deviceName)
        {
            SetDeviceIndicatorText(deviceName);
            SetFavouriteIndicator();
        }

        private void HandleDeviceDisconnected(string deviceName)
        {
            SetDeviceIndicatorText(NO_DEVICE_NAME);
            SetFavouriteIndicator();
        }

        private void HandleFavouriteDeviceChanged(string newFavouriteDeviceName,
                                                  string oldFavouriteDeviceName)
        {
            SetFavouriteIndicator();
        }

        private void SetDeviceIndicatorText(string deviceName)
        {
            if (_deviceNameText != null)
            {
                _deviceNameText.text = deviceName;
            }
        }

        private void SetFavouriteIndicator(string newFavouriteDeviceName)
        {
            if (_favouriteIndicator != null)
            {
                _favouriteIndicator.color = _deviceNameText.text == newFavouriteDeviceName
                    ? _colourFavourite : Color.clear;
            }
        }
        private void SetFavouriteIndicator()
        {
            SetFavouriteIndicator(Litho.GetFavouriteDeviceName());
        }

        private void SetBatteryIndicator(int batterylevel)
        {
            if (_batteryLevelIndicator == null)
            {
                return;
            }
            // Flag low battery with a red indictor bar
            _batteryLevelIndicator.GetComponent<Image>().color = batterylevel <= 25
                ? _colourBad : _colourOkay;

            // Resize the indicator bar to reflect the battery level
            _batteryLevelIndicator.SetInsetAndSizeFromParentEdge(
                RectTransform.Edge.Left, 0f,
                ((RectTransform)_batteryLevelIndicator.parent.transform).rect.width
                * batterylevel / 100f);
        }

        public void FavouriteCurrentDevice()
        {
            if (_deviceNameText != null && _deviceNameText.text != NO_DEVICE_NAME)
            {
                if (Litho.GetFavouriteDeviceName() != _deviceNameText.text)
                {
                    Litho.SaveFavouriteDeviceName(_deviceNameText.text);
                    Debug.LogFormat("Favouriting current device ({0})", _deviceNameText.text);
                }
                else
                {
                    Litho.SaveFavouriteDeviceName("");
                    Debug.LogFormat("Unfavouriting current device ({0})", _deviceNameText.text);
                }
            }
            else
            {
                Debug.Log("Cannot favourite device; no device is connected");
            }
        }
    }

}
