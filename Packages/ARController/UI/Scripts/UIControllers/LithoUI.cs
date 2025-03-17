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
    /// Provides a static function through which a fatal error message overlay can be displayed
    /// </summary>
    [RequireComponent(typeof(Utility.DontDestroyOnLoad))]
    public class LithoUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Dropdown menu to use")]
        private GameObject _dropdownMenu = null;

        [SerializeField]
        [Tooltip("Overlay to show on error")]
        private GameObject _errorOverlay = null;

        [SerializeField]
        [Tooltip("Text element in which to display error messages")]
        private Text _errorMessageText = null;

        private static LithoUI _instance = null;

        public static bool HasFatalError;

        public const float BASELINE_SCREEN_WIDTH = 1125f;


        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                // This is not the first Litho UI in the scene, so destroy it
                Destroy(gameObject);
                return;
            }

            if (_dropdownMenu != null)
            {
                _dropdownMenu.SetActive(true);
            }

            if (_errorOverlay == null)
            {
                Debug.LogWarning("Error overlay object is not set; " +
                                 this + " error notification will not work as intended");
            }
            if (_errorMessageText == null)
            {
                Debug.LogWarning("Error message text item is not set; " +
                                 this + " error notification will not work as intended");
            }

            Litho.OnFatalError += ShowFatalError;
        }

        public static void ShowFatalError(string message)
        {
            HasFatalError = true;
            if (_instance != null)
            {
                _instance._errorMessageText.text = message;
                _instance._errorOverlay.SetActive(true);
            }
        }
    }

}
