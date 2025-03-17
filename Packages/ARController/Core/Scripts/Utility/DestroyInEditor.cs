/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Utility
{
    /// <summary>
    /// Immediately deletes this GameObject when this script runs in the Unity Editor (used for
    /// build-only components)
    /// </summary>
    [AddComponentMenu("LITHO/Utilities/Destroy In Editor", -9179)]
    public class DestroyInEditor : MonoBehaviour
    {
        // When the game starts
        private void Awake()
        {
#if UNITY_EDITOR
            // If in the Unity Editor, destroy the game object this script is attached to
            Destroy(gameObject);
#else
            // Otherwise, ignore this script (and destroy it)
            Destroy(this);
#endif
        }
    }

}
