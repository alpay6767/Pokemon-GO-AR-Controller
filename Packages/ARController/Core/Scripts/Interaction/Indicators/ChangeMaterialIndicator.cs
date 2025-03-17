/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Updates the material of this object when any Manipulables on it or its children are hovered
    /// or grabbed
    /// </summary>
    [AddComponentMenu("LITHO/Indicators/Change Material Indicator", -9698)]
    public class ChangeMaterialIndicator : ManipulationIndicator
    {
        [SerializeField]
        [Tooltip("Material to apply when not hovered or activated")]
        private Material _defaultMaterial = null;

        [SerializeField]
        [Tooltip("Material to apply when hovered")]
        private Material _hoveredMaterial = null;

        [SerializeField]
        [Tooltip("Material to apply when activated")]
        private Material _activatedMaterial = null;

        [SerializeField]
        [Tooltip("Whether to apply material changes to child (and more deeply-nested) objects")]
        private bool _applyToChildren = true;


        protected virtual void Start()
        {
            if (_defaultMaterial == null)
            {
                _defaultMaterial = GetComponentInChildren<Renderer>()?.material;
            }
        }

        protected override void ProcessManipulatorEnter(Manipulation manipulation)
        {
            if (_hoveredMaterial != null)
            {
                SetMaterial(_hoveredMaterial);
            }
        }

        protected override void ProcessManipulatorExit(Manipulation manipulation)
        {
            if (_defaultMaterial != null)
            {
                SetMaterial(_defaultMaterial);
            }
        }

        protected override void ProcessManipulatorGrab(Manipulation manipulation)
        {
            if (_activatedMaterial != null)
            {
                SetMaterial(_activatedMaterial);
            }
        }

        protected override void ProcessManipulatorRelease(Manipulation manipulation)
        {
            if (_hoveredMaterial != null)
            {
                SetMaterial(_hoveredMaterial);
            }
        }

        private void SetMaterial(Material newMaterial)
        {
            if (!_applyToChildren)
            {
                if (GetComponent<Renderer>())
                {
                    GetComponent<Renderer>().material = newMaterial;
                }
            }
            else
            {
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                {
                    r.material = newMaterial;
                }
            }
        }
    }

}
