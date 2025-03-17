/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Utility
{
    /// <summary>
    /// Marks this GameObject as something that should not be destroyed when a scene is loaded
    /// </summary>
    [AddComponentMenu("LITHO/Utilities/Don't Destroy On Load", -9180)]
    public class DontDestroyOnLoad : MonoBehaviour
    {
        // When the game starts
        private void Awake()
        {
            // Tell Unity to specifically not destroy this object when a scene loads
            DontDestroyOnLoad(transform.root.gameObject);

            if (!Equals(transform.root.gameObject, gameObject))
            {
                Debug.LogWarning("DontDestroyOnLoad has been called for " +
                                 transform.root.gameObject.name + " as a proxy for calling it " +
                                 "on " + name + " (which is not a root GameObject, hence does " +
                                 "not support DontDestroyOnLoad)");
            }
        }
    }

}
