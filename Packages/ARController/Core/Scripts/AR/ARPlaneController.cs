/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace LITHO
{

    /// <summary>
    /// Handles plane selection and visibility
    /// </summary>
    [AddComponentMenu("LITHO/AR/AR Plane Controller", -9899)]
    [RequireComponent(typeof(ARPlane))]
    [RequireComponent(typeof(Manipulable))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ARPlaneController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Material to append to the MeshRenderer to make this plane more visible")]
        private Material _visibleMaterial = null;

        private bool _isVisible;

        private bool _eventsAreRegistered;

        private ARPlane _arPlane;
        private Manipulable _manipulable;
        private MeshRenderer _meshRenderer;


        private void Awake()
        {
            _arPlane = GetComponent<ARPlane>();
            _manipulable = GetComponent<Manipulable>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            // Only allow this plane to be selected if it is horizontal
            if (!ARManager.GetPlaneIsHorizontal(_arPlane))
            {
                ManipulationIndicator indicator = GetComponent<ManipulationIndicator>();
                if (indicator != null)
                {
                    indicator.enabled = false;
                }
            }
            else if (!_eventsAreRegistered)
            {
                _manipulable.OnManipulatorGrab += HandleGrab;
                _eventsAreRegistered = true;
            }
        }

        private void OnDestroy()
        {
            if (_manipulable != null)
            {
                _manipulable.OnManipulatorGrab -= HandleGrab;
                _eventsAreRegistered = false;
            }
        }

        public void SetVisibility(bool shouldBeVisible)
        {
            if (_visibleMaterial != null)
            {
                if (shouldBeVisible && !_isVisible)
                {
                    List<Material> materials = new List<Material>(_meshRenderer.materials);
                    materials.Add(_visibleMaterial);
                    _meshRenderer.materials = materials.ToArray();
                }
                else if (!shouldBeVisible && _isVisible)
                {
                    List<Material> materials = new List<Material>(_meshRenderer.materials);
                    _meshRenderer.materials = materials.GetRange(0, 1).ToArray();
                }
            }
            else
            {
                Debug.LogWarning("Visible material not set; cannot modify AR plane visibility");
            }
            _isVisible = shouldBeVisible;
        }

        public void SetInteractability(bool shouldBeManipulable)
        {
            _manipulable.Interactable = shouldBeManipulable;
        }

        private void HandleGrab(Manipulation manipulation)
        {
            ARManager arManager = FindObjectOfType<ARManager>();
            if (arManager != null)
            {
                arManager.SelectGroundPlane(_arPlane, manipulation.Manipulator.GrabPosition);
            }
        }

        private void HandleGroundPlaneSelectionStarted()
        {
            SetInteractability(ARManager.GetPlaneIsHorizontal(_arPlane));
            SetVisibility(true);
        }

        private void HandleGroundPlaneSelectionCompleted()
        {
            if (_manipulable != null)
            {
                SetInteractability(false);
            }
        }
    }

}
