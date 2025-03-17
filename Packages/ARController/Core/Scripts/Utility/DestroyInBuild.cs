/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Utility
{
    /// <summary>
    /// Immediately deletes this GameObject when this script runs anywhere other than the
    /// Unity Editor (used for Editor-only components)
    /// </summary>
    [AddComponentMenu("LITHO/Utilities/Destroy In Build", -9178)]
    public class DestroyInBuild : MonoBehaviour
    {
        // When the game starts
        private void Awake()
        {
#if !UNITY_EDITOR
            // If in a build, destroy the game object this script is attached to
            Destroy(gameObject);
#else
            // Otherwise, ignore this script (and destroy it)
            Destroy(this);
#endif
        }
    }

}
