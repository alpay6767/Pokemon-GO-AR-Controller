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
    /// Handles synchronization of Litho calibration, handedness and grip type controls
    /// </summary>
    [AddComponentMenu("LITHO/UI/Calibration UI", -9396)]
    public class CalibrationUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("List of all toggles in the scene that control Litho handedness")]
        private List<ToggleController> _handednessToggles = new List<ToggleController>();

        [SerializeField]
        [Tooltip("List of all buttons in the scene that control Litho grip type")]
        private List<ToggleController> _gripTypeToggles = new List<ToggleController>();

        [SerializeField]
        [Tooltip("List of all buttons in the scene that trigger Litho calibration")]
        private List<Button> _calibrateButtons = null;

        [SerializeField]
        [Tooltip("UI views to show when in right-handed point grip")]
        private List<GameObject> _pointGripRightObjects = new List<GameObject>();
        [SerializeField]
        [Tooltip("UI views to show when in left-handed point grip")]
        private List<GameObject> _pointGripLeftObjects = new List<GameObject>();
        [SerializeField]
        [Tooltip("UI views to show when in right-handed clutch grip")]
        private List<GameObject> _clutchGripRightObjects = new List<GameObject>();
        [SerializeField]
        [Tooltip("UI views to show when in left-handed clutch grip")]
        private List<GameObject> _clutchGripLeftObjects = new List<GameObject>();


        private void Awake()
        {
            HandleDeviceConnectionStateChange(false);

            // Respond to Litho connection events
            Litho.OnConnected += HandleDeviceConnected;
            Litho.OnDisconnected += HandleDeviceDisconnected;
            // Respond to Litho confirming changes to handedness and grip type
            Litho.OnHandednessChanged += HandleHandednessChanged;
            Litho.OnGripTypeChanged += HandleGripTypeChanged;

            if (_handednessToggles != null && _handednessToggles.Count > 0)
            {
                foreach (ToggleController toggle in _handednessToggles)
                {
                    if (toggle != null)
                    {
                        toggle.OnValueChanged.AddListener(SetHandedness);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null handedness toggle on " + this);
                    }
                }
            }
            if (_gripTypeToggles != null && _gripTypeToggles.Count > 0)
            {
                foreach (ToggleController toggle in _gripTypeToggles)
                {
                    if (toggle != null)
                    {
                        toggle.OnValueChanged.AddListener(SetGripType);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null grip type toggle on " + this);
                    }
                }
            }
            if (_calibrateButtons != null && _calibrateButtons.Count > 0)
            {
                foreach (Button button in _calibrateButtons)
                {
                    if (button != null)
                    {
                        button.onClick.AddListener(Calibrate);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null calibration button on " + this);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            Litho.OnConnected -= HandleDeviceConnected;
            Litho.OnDisconnected -= HandleDeviceDisconnected;
            Litho.OnHandednessChanged -= HandleHandednessChanged;
            Litho.OnGripTypeChanged -= HandleGripTypeChanged;

            if (_handednessToggles != null && _handednessToggles.Count > 0)
            {
                foreach (ToggleController toggle in _handednessToggles)
                {
                    if (toggle != null)
                    {
                        toggle.OnValueChanged.RemoveListener(SetHandedness);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null handedness toggle on " + this);
                    }
                }
            }
            if (_gripTypeToggles != null && _gripTypeToggles.Count > 0)
            {
                foreach (ToggleController toggle in _gripTypeToggles)
                {
                    if (toggle != null)
                    {
                        toggle.OnValueChanged.RemoveListener(SetGripType);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null grip type toggle on " + this);
                    }
                }
            }
            if (_calibrateButtons != null && _calibrateButtons.Count > 0)
            {
                foreach (Button button in _calibrateButtons)
                {
                    if (button != null)
                    {
                        button.onClick.RemoveListener(Calibrate);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null calibration button on " + this);
                    }
                }
            }
        }

        private void HandleDeviceConnected(string deviceName)
        {
            HandleDeviceConnectionStateChange(true);
            UpdateGripImages();
        }

        private void HandleDeviceDisconnected(string deviceName)
        {
            HandleDeviceConnectionStateChange(false);
        }

        private void HandleHandednessChanged(Handedness newHandedness, Handedness oldHandedness)
        {
            UpdateGripImages();
        }

        private void HandleGripTypeChanged(GripType newGripType, GripType oldGripType)
        {
            UpdateGripImages();
        }

        private void HandleDeviceConnectionStateChange(bool connected)
        {
            if (_handednessToggles != null && _handednessToggles.Count > 0)
            {
                foreach (ToggleController toggle in _handednessToggles)
                {
                    if (toggle != null)
                    {
                        toggle.Interactable = connected;
                    }
                    else
                    {
                        Debug.LogWarning("There is a null handedness toggle on " + this);
                    }
                }
            }
            if (_gripTypeToggles != null && _gripTypeToggles.Count > 0)
            {
                foreach (ToggleController toggle in _gripTypeToggles)
                {
                    if (toggle != null)
                    {
                        toggle.Interactable = connected;
                    }
                    else
                    {
                        Debug.LogWarning("There is a null grip type toggle on " + this);
                    }
                }
            }
            if (_calibrateButtons != null && _calibrateButtons.Count > 0)
            {
                foreach (Button button in _calibrateButtons)
                {
                    if (button != null)
                    {
                        button.interactable = connected;
                    }
                    else
                    {
                        Debug.LogWarning("There is a null calibration button on " + this);
                    }
                }
            }
        }


        public void SetHandedness(int handednessIndex)
        {
            // Request that Litho update the handedness
            Litho.Handedness = (Handedness)(1 - handednessIndex);
        }
        public void SetGripType(int gripTypeIndex)
        {
            // Request that Litho update the grip type
            Litho.GripType = (GripType)gripTypeIndex;
        }

        public void Calibrate()
        {
            Litho.Calibrate();
        }
        private void UpdateGripImages()
        {
            if (_handednessToggles != null && _handednessToggles.Count > 0)
            {
                foreach (ToggleController toggle in _handednessToggles)
                {
                    if (toggle != null)
                    {
                        toggle.SelectValue(1 - (int)Litho.Handedness, -1, true);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null handedness toggle on " + this);
                    }
                }
            }
            if (_gripTypeToggles != null && _gripTypeToggles.Count > 0)
            {
                foreach (ToggleController toggle in _gripTypeToggles)
                {
                    if (toggle != null)
                    {
                        toggle.SelectValue((int)Litho.GripType, -1, true);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null grip type toggle on " + this);
                    }
                }
            }

            if (_pointGripRightObjects != null && _pointGripRightObjects.Count > 0)
            {
                foreach (GameObject image in _pointGripRightObjects)
                {
                    if (image != null)
                    {
                        image.SetActive(Litho.GripType == GripType.Point
                                        && Litho.Handedness == Handedness.Right);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null point grip right image on " + this);
                    }
                }
            }
            if (_pointGripLeftObjects != null && _pointGripLeftObjects.Count > 0)
            {
                foreach (GameObject image in _pointGripLeftObjects)
                {
                    if (image != null)
                    {
                        image.SetActive(Litho.GripType == GripType.Point
                                        && Litho.Handedness == Handedness.Left);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null point grip left image on " + this);
                    }
                }
            }
            if (_clutchGripRightObjects != null && _clutchGripRightObjects.Count > 0)
            {
                foreach (GameObject image in _clutchGripRightObjects)
                {
                    if (image != null)
                    {
                        image.SetActive(Litho.GripType == GripType.Clutch
                                        && Litho.Handedness == Handedness.Right);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null clutch grip right image on " + this);
                    }
                }
            }
            if (_clutchGripLeftObjects != null && _clutchGripLeftObjects.Count > 0)
            {
                foreach (GameObject image in _clutchGripLeftObjects)
                {
                    if (image != null)
                    {
                        image.SetActive(Litho.GripType == GripType.Clutch
                                        && Litho.Handedness == Handedness.Left);
                    }
                    else
                    {
                        Debug.LogWarning("There is a null clutch grip left image on " + this);
                    }
                }
            }
        }
    }

}
