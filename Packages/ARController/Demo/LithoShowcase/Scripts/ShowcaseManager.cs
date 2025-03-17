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
    /// Manages a collection of showcase items to be displayed sequentially
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Showcase Manager", -9078)]
    public class ShowcaseManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Transform in which to host showcase items as they are cycled")]
        private Transform _itemBase = null;

        [SerializeField]
        [Tooltip("Whether to populate the list of showcase items using the hierarchical " +
                 "children of this object")]
        private bool _populateFromChildren = true;

        [SerializeField]
        [Tooltip("List of items in the showcase")]
        private List<ShowcaseItem> _items = new List<ShowcaseItem>();

        [SerializeField]
        [Tooltip("Index of the showcase item that is currently on display")]
        private int _currentItem;


        private void Awake()
        {
            if (_itemBase == null)
            {
                _itemBase = transform;
            }
            if (_populateFromChildren)
            {
                _items.Clear();
                foreach (ShowcaseItem item in GetComponentsInChildren<ShowcaseItem>())
                {
                    if (item != null)
                    {
                        item.transform.SetParent(_itemBase, true);
                        _items.Add(item);
                    }
                }
            }
            UpdateItem();
        }

        private void Start()
        {
            // HACK: Duplicate an object with a Collider to trigger a Rigidbody centre-of-mass
            // recalculation (Unity does not handle this properly), then destroy the object again
            GameObject temp = Instantiate(_itemBase.gameObject, _itemBase.position,
                                          _itemBase.rotation, _itemBase);
            Destroy(temp);
            UpdateItem();
        }

        private void UpdateItem()
        {
            foreach (ShowcaseItem item in _items)
            {
                item.gameObject.SetActive(false);
            }
            _items[_currentItem].transform.localPosition = Vector3.zero;
            _items[_currentItem].gameObject.SetActive(true);
            _items[_currentItem].ResetItem();
        }

        private void OnValidate()
        {
            if (_items.Count > 0)
            {
                if (_currentItem < 0)
                {
                    _currentItem = -((-_currentItem) % _items.Count) + _items.Count;
                }
                else
                {
                    _currentItem = _currentItem % _items.Count;
                }
            }
            else
            {
                _currentItem = 0;
            }
        }

        public void ShowItem(int itemIndex)
        {
            _currentItem = itemIndex;
            OnValidate();
            UpdateItem();
        }

        public void ShowNext()
        {
            ShowItem(_currentItem + 1);
        }

        public void ShowPrevious()
        {
            ShowItem(_currentItem - 1);
        }
    }

}
