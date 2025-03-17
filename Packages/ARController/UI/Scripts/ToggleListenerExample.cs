/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.UI;
using LITHO.UI;

namespace LITHO.Demo
{

    /// <summary>
    /// Demonstrates how Litho UI toggle events can be subscribed to and used
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ToggleListenerExample : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Toggle to listen to")]
        private ToggleController _toggle = null;

        private Text _responseText = null;


        private void Awake()
        {
            _responseText = GetComponent<Text>();

            if (_toggle != null)
            {
                _toggle.OnValueChanged.AddListener(Toggle_OnValueChanged);
            }
            else
            {
                Debug.LogWarning("Toggle property not found; cannot respond to toggle updates");
            }
        }

        private void Toggle_OnValueChanged(int value)
        {
            _responseText.text = "Toggle set to '" + _toggle.ValueName
                + "' (option number " + value + ")";
        }
    }

}
