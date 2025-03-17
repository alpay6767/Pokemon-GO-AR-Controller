/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Manages a prefab that can be displayed as part of a showcase
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Showcase Item", -9077)]
    public class ShowcaseItem : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Prefab representing a set of showcase content")]
        private GameObject _itemPrefab = null;

        [SerializeField]
        [Tooltip("Object already in the current scene (representing the showcase content) that " +
                 "should be replaced when the item is reset")]
        private GameObject _item = null;


        private void Start()
        {
            ResetItem();
        }

        public void ResetItem()
        {
            if (_item != null)
            {
                Destroy(_item);
            }
            _item = Instantiate(_itemPrefab, transform.position, transform.rotation, transform);
        }
    }

}
