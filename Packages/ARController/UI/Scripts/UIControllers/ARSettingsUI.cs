/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LITHO.UI
{

    /// <summary>
    /// Displays AR scanning progress to the user
    /// </summary>
    [AddComponentMenu("LITHO/UI/AR Settings UI", -9397)]
    public class ARSettingsUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Toggle determining visibility of AR planes")]
        private ToggleController _planeVisibilityToggle = null;

        [SerializeField]
        [Tooltip("Button to reset ground plane")]
        private Button _selectGroundPlaneButton = null;

        [SerializeField]
        [Tooltip("CanvasGroupAnimator controlling the prompt explaining AR scanning")]
        private CanvasGroupAnimator _scanningPrompt = null;

        [SerializeField]
        [Tooltip("CanvasGroupAnimator controlling the prompt explaining ground plane selection")]
        private CanvasGroupAnimator _selectGroundPlanePrompt = null;

        [SerializeField]
        [Tooltip("Slider that controls AR scale")]
        private SliderController _scaleARSlider = null;

        [SerializeField]
        [Tooltip("Objects that should only be enabled whilst a ground plane is specified")]
        private List<GameObject> _groundRequiredObjects = new List<GameObject>();

        private const float FADED_PROMPT_ALPHA = 0.5f;

        private ARManager _arManager;


        private void Awake()
        {
            _arManager = FindObjectOfType<ARManager>();
            if (_arManager != null)
            {
                _arManager.OnGroundPlaneSelectionStarted += HandleGroundPlaneSelectionStarted;
                _arManager.OnPotentialGroundPlanesFound += HandlePotentialGroundPlanesFound;
                _arManager.OnGroundPlaneSelectionCompleted += HandleGroundPlaneSelectionCompleted;
                _arManager.OnPlaneVisibilityChanged += HandlePlaneVisibilityChanged;
                _arManager.OnARScaleChanged += HandleARScaleChanged;

                if (_arManager.IsSelectingGroundPlane)
                {
                    HandleGroundPlaneSelectionStarted();
                    if (_arManager.GroundPlaneOptionsExist)
                    {
                        HandlePotentialGroundPlanesFound();
                    }
                }
            }
            else
			{
                Debug.LogWarning("Could not find an ARManager in the scene; "
                                 + this + " will not work as intended");
			}
            if (_planeVisibilityToggle != null)
            {
                _planeVisibilityToggle.OnValueChanged.AddListener(
                    HandlePlaneVisibilityToggleChanged);
            }
            else
            {
                Debug.LogWarning("Toggle for controlling plane visibility is not set; "
                                 + this + " will not work as intended");
            }
            if (_selectGroundPlaneButton != null)
            {
                _selectGroundPlaneButton.onClick.AddListener(
                    HandleSelectGroundPlaneButtonClicked);
            }
            else
            {
                Debug.LogWarning("Button to trigger ground plane selection is not set; "
                                 + this + " will not work as intended");
            }
            if (_scaleARSlider != null)
            {
                _scaleARSlider.OnValueChanged.AddListener(HandleSliderValueChanged);
            }
            else
            {
                Debug.LogWarning("Slider to control AR scale is not set; "
                                 + this + " will not work as intended");
            }
            foreach (GameObject obj in _groundRequiredObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (_arManager != null)
            {
                _arManager.OnGroundPlaneSelectionStarted -= HandleGroundPlaneSelectionStarted;
                _arManager.OnPotentialGroundPlanesFound -= HandlePotentialGroundPlanesFound;
                _arManager.OnGroundPlaneSelectionCompleted -= HandleGroundPlaneSelectionCompleted;
                _arManager.OnPlaneVisibilityChanged -= HandlePlaneVisibilityChanged;
                _arManager.OnARScaleChanged -= HandleARScaleChanged;
            }
            if (_planeVisibilityToggle != null)
            {
                _planeVisibilityToggle.OnValueChanged.RemoveListener(
                    HandlePlaneVisibilityToggleChanged);
            }
            if (_selectGroundPlaneButton != null)
            {
                _selectGroundPlaneButton.onClick.RemoveListener(
                    HandleSelectGroundPlaneButtonClicked);
            }
            if (_scaleARSlider != null)
            {
                _scaleARSlider.OnValueChanged.RemoveListener(HandleSliderValueChanged);
            }
        }

        private void HandleGroundPlaneSelectionStarted()
        {
            if (_planeVisibilityToggle != null)
            {
                _planeVisibilityToggle.Interactable = false;
            }
            foreach (GameObject obj in _groundRequiredObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
            if (_scanningPrompt != null)
            {
                _scanningPrompt.TargetAlpha = 1f;
            }
            if (_selectGroundPlanePrompt != null)
            {
                _selectGroundPlanePrompt.TargetAlpha = FADED_PROMPT_ALPHA;
            }
        }

        private void HandlePotentialGroundPlanesFound()
        {
            if (_arManager.IsSelectingGroundPlane)
            {
                if (_scanningPrompt != null)
                {
                    _scanningPrompt.TargetAlpha = FADED_PROMPT_ALPHA;
                }
                if (_selectGroundPlanePrompt != null)
                {
                    _selectGroundPlanePrompt.TargetAlpha = 1f;
                }
            }
        }

        private void HandleGroundPlaneSelectionCompleted()
        {
            if (_planeVisibilityToggle != null)
            {
                _planeVisibilityToggle.Interactable = true;
            }
            foreach (GameObject obj in _groundRequiredObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            if (_scanningPrompt != null)
            {
                _scanningPrompt.TargetAlpha = 0f;
            }
            if (_selectGroundPlanePrompt != null)
            {
                _selectGroundPlanePrompt.TargetAlpha = 0f;
            }
        }

        private void HandlePlaneVisibilityToggleChanged(int newValue)
        {
            if (_arManager != null)
            {
                _arManager.IsShowingARPlanes = newValue > 0;
            }
        }

        private void HandleARScaleChanged(float newScale, float oldScale)
        {
            if (Mathf.Abs(newScale - oldScale) > Litho.EPSILON
                && Mathf.Abs(newScale - _scaleARSlider.Value) > Litho.EPSILON)
            {
                _scaleARSlider.Value = newScale;
            }
        }

        private void HandleSliderValueChanged(float newValue)
        {
            if (_arManager != null)
            {
                _arManager.SetScale(newValue);
            }
        }

        private void HandleSelectGroundPlaneButtonClicked()
        {
            if (_arManager != null)
            {
                // Start the ground plane selection process
                _arManager.StartGroundPlaneSelection();
            }
        }

        private void HandlePlaneVisibilityChanged(bool planesAreVisible)
        {
            if (_planeVisibilityToggle != null)
            {
                _planeVisibilityToggle.SelectValue(planesAreVisible ? 1 : 0);
            }
        }
    }

}
