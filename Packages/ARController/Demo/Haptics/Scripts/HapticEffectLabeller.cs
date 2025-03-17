/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Pastes the list of haptic effect names of a HapticTrigger into a Label
    /// </summary>
    public class HapticEffectLabeller : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("HapticTrigger to read HapticEffect types from")]
        private HapticTrigger _hapticTrigger = null;

        [SerializeField]
        [Tooltip("Label to list HapticEffect types in")]
        private Label _label = null;

        private void Start()
        {
            if (_hapticTrigger != null && _label != null)
            {
                if (_hapticTrigger.EffectTypes != null && _hapticTrigger.EffectTypes.Count > 0)
                {
                    string labelText = "";
                    for (int e = 0; e < _hapticTrigger.EffectTypes.Count; e++)
                    {
                        labelText += _hapticTrigger.EffectTypes[e];
                        if (e != _hapticTrigger.EffectTypes.Count - 1)
                        {
                            labelText += "\n";
                        }
                    }
                    _label.SetText(labelText);
                }
                else
                {
                    _label.SetText("No haptic effect");
                }
            }
        }
    }

}
