/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Sends a list of haptic events to be played by the connected Litho
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Haptic Trigger", -9077)]
    public class HapticTrigger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Vector by which to offset proportionally to the radius of the held object")]
        private List<HapticEffect.Type>_effectTypes = new List<HapticEffect.Type>();
        public List<HapticEffect.Type> EffectTypes => _effectTypes;


        public void PlayEffect()
        {
            if (EffectTypes.Count > 0)
            {
                Litho.PlayHapticEffects(EffectTypes);
            }
            else
            {
                Debug.LogWarning("Cannot play haptic effect; no effects specified");
            }
        }
    }

}
