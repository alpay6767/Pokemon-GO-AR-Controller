/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;

namespace LITHO.UI
{

    /// <summary>
    /// Manages enabling and disabling tabs that require Litho device connection, and those which
    /// are relevant for user onboarding
    /// </summary>
    [AddComponentMenu("LITHO/UI/UI Manager", -9400)]
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Objects (e.g. UI tabs) to show only whilst connected to a device")]
        private List<GameObject> _connectionRequiredObjects = new List<GameObject>();

        [SerializeField]
        [Tooltip("Objects (e.g. UI tabs) to show only when running onboarding")]
        private List<GameObject> _onboardObjects = new List<GameObject>();

        [SerializeField]
        [Tooltip("Objects to show only when not running onboarding")]
        private List<GameObject> _notOnboardObjects = new List<GameObject>();

        private bool _isOnboarding;

        private TabManager _tabManager;


        private void Awake()
        {
            _tabManager = GetComponentInParent<TabManager>();

            // Respond to Litho confirming changes to handedness and grip type
            Litho.OnConnected += HandleDeviceConnected;
            Litho.OnDisconnected += HandleDeviceDisconnected;
        }

        private void Start()
        {
            // Simulate a disconnection event to move the UI to a 'disconnected' state
            // (Call this in Start so that each tab will be able to run its Awake event first)
            HandleDeviceConnectionStateChange(false);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            Litho.OnConnected -= HandleDeviceConnected;
            Litho.OnDisconnected -= HandleDeviceDisconnected;
        }

        private void HandleDeviceConnected(string deviceName)
        {
            HandleDeviceConnectionStateChange(true);
        }

        private void HandleDeviceDisconnected(string deviceName)
        {
            HandleDeviceConnectionStateChange(false);
        }

        private void HandleDeviceConnectionStateChange(bool connected)
        {
            foreach (GameObject obj in _connectionRequiredObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(connected);
                }
            }

            if (!connected)
            {
                // Go back to the device selection screen
                _tabManager.SelectValue(0);
            }
            bool shouldBeOnboarding = connected && !Litho.GetHasOnboarded();
            SetOnboardingUI(shouldBeOnboarding, shouldBeOnboarding ? 1 : -1);
        }

        public void StartOnboarding()
        {
            SetOnboardingUI(true, 1);
            Litho.SaveHasOnboarded(false);
        }

        public void FinishOnboarding()
        {
            SetOnboardingUI(false, 1);
            Litho.SaveHasOnboarded(true);
        }

        private void SetOnboardingUI(bool isOnboarding, int goToTab = -1)
        {
            _isOnboarding = isOnboarding;
            foreach (GameObject obj in _onboardObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(isOnboarding);
                }
            }
            foreach (GameObject obj in _notOnboardObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(!isOnboarding);
                }
            }
            if (goToTab >= 0)
            {
                _tabManager.SelectValue(goToTab);
            }
        }
    }

}
