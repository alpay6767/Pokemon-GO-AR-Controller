/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEditor;

namespace LITHO
{

    /// <summary>
    /// Handles ground plane selection and AR plane visibility; informs Litho of updates to the
    /// height of the ground
    /// </summary>
    [AddComponentMenu("LITHO/AR/AR Manager", -9900)]
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(Utility.DontDestroyOnLoad))]
    [RequireComponent(typeof(ARSessionOrigin))]
    public class ARManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Object representing the floor of the scene")]
        private Transform _floorObject = null;
        public Transform FloorObject => _floorObject;

        [SerializeField]
        [Tooltip("Object to be placed on the floor as the focus of the scene")]
        private Transform _focusObject = null;
        public Transform FocusObject
        {
            get { return _focusObject; }
            private set { _focusObject = value; }
        }

        [SerializeField]
        [Tooltip("Minimum height difference for a horizontal plane to be considered separate " +
                 "from the ground plane (metres)")]
        private float _minGroundPlaneOffset = 0.1f;

        [SerializeField]
        [Tooltip("Prefab to spawn to represent AR planes")]
        private GameObject _planePrefab = null;

        [SerializeField]
        [PlaneDetectionModeMask]
        [Tooltip("The types of planes to detect")]
        private PlaneDetectionMode _planeDetectionMode = (PlaneDetectionMode)(-1);

        [SerializeField]
        [Tooltip("The quality of human depth segmentation to use")]
        private HumanSegmentationDepthMode _segmentationDepthMode = HumanSegmentationDepthMode.Best;

        [SerializeField]
        [Tooltip("The quality of human stencil segmentation to use")]
        private HumanSegmentationStencilMode _segmentationStencilMode = HumanSegmentationStencilMode.Best;

        private ARSessionOrigin _arOrigin;

        private List<ARPlane> _allARPlanes = new List<ARPlane>();
        private List<ARPlane> _groundCoplanarARPlanes = new List<ARPlane>();

        public delegate void GroundPlaneSelectionUpdateHandler();
        public event GroundPlaneSelectionUpdateHandler OnGroundPlaneSelectionStarted,
                                                       OnPotentialGroundPlanesFound,
                                                       OnGroundPlaneSelectionCompleted;

        public delegate void FloorHeightChangeHandler(float newFloorHeight);
        public event FloorHeightChangeHandler OnFloorHeightChanged;

        public delegate void PlaneVisibilityChangeHandler(bool planesAreVisible);
        public event PlaneVisibilityChangeHandler OnPlaneVisibilityChanged;

        public delegate void ARScaleChangeHandler(float newARScale, float oldARScale);
        public event ARScaleChangeHandler OnARScaleChanged;

        public bool HasARGroundPlane
        {
            get
            {
                return _groundCoplanarARPlanes != null && _groundCoplanarARPlanes.Count > 0;
            }
        }

        public ARPlane GroundARPlane => HasARGroundPlane ? _groundCoplanarARPlanes[0] : null;

        public float FloorHeight { get; private set; }

        public Vector3 FocusPoint { get; private set; }

        private bool _hasIssuedOnPotentialGroundPlanesFound;
        public bool GroundPlaneOptionsExist
        {
            get
            {
                if (_allARPlanes != null && _allARPlanes.Count > 0)
                {
                    int horizontalCount = 0;
                    bool ishorizontal;
                    float area = 0f;
                    foreach (ARPlane plane in _allARPlanes)
                    {
                        ishorizontal = GetPlaneIsHorizontal(plane);
                        if (ishorizontal)
                        {
                            horizontalCount++;
                        }
                        if (horizontalCount > 2)
                        {
                            return true;
                        }
                        if (ishorizontal)
                        {
                            for (int p = 2; p < plane.boundary.Length; p++)
                            {
                                area += Vector3.Cross(
                                    plane.boundary[p] - plane.boundary[0],
                                    plane.boundary[p - 1] - plane.boundary[0]).magnitude;
                            }
                            if (area > 6f)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        private bool _isShowingARPlanes;
        public bool IsShowingARPlanes
        {
            get { return _isShowingARPlanes; }
            set
            {
                SetARPlaneVisibilities(value);
            }
        }

        public bool IsSelectingGroundPlane { get; private set; }

        private float _groundPlaneSelectionStartTime;

        private float _initialCameraNearPlane, _initialCameraFarPlane, _initialShadowDistance;

        private static ARManager _instance = null;

        private const string PREF_KEY_AR_SCALE = "litho_ar_scale";


        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;

                // Enable attached AR components
                ARCameraManager cameraManager = GetComponentInChildren<ARCameraManager>();
                cameraManager.enabled = true;
                ARSessionOrigin origin = GetComponent<ARSessionOrigin>();
                origin.enabled = true;
                ARSession session = GetComponent<ARSession>();
                session.enabled = true;

                // Create and set up other AR components
                gameObject.AddComponent<ARInputManager>();
                ARPlaneManager planeManager = gameObject.AddComponent<ARPlaneManager>();
                planeManager.detectionMode = _planeDetectionMode;
                planeManager.planePrefab = _planePrefab;
                planeManager.planesChanged += PlanesChanged;
                AROcclusionManager occlusionManager
                    = cameraManager.gameObject.AddComponent<AROcclusionManager>();
                occlusionManager.humanSegmentationDepthMode = _segmentationDepthMode;
                occlusionManager.humanSegmentationStencilMode = _segmentationStencilMode;

                if (FocusObject != null)
                {
                    if (!FocusObject.IsChildOf(transform))
                    {
                        FocusPoint = FocusObject.position;
                    }
                    else
                    {
                        Debug.LogWarning("Focus object of " + this + " should not be a child of " +
                                         "ARSessionOrigin; move your main content outside of " +
                                         "the LithoARSessionOrigin to enable correct AR scaling" +
                                         "when running in AR.");
                        FocusObject = null;
                    }
                }
                else
                {
                    Debug.LogWarning("Focus object is not set on " + this + "; set this value " +
                                     "to the main/ root content of your scene in order to place " +
                                     "it on the ground when running in AR.");
                }

                _initialCameraNearPlane = Camera.main.nearClipPlane;
                _initialCameraFarPlane = Camera.main.farClipPlane;
                _initialShadowDistance = QualitySettings.shadowDistance;
            }
            else
            {
                // This is not the first session manager in the scene
                if (FocusObject != null)
                {
                    // Assign the FocusObject from the ARManager in the new scene to the persistent
                    // ARManager that moved in from the old scene, resetting position and rotation
                    _instance.FocusObject = FocusObject;
                    _instance.SetWorldEnabled(
                        _instance.FocusPoint,
                        Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f));
                }
                // Destroy this (the new) ARManager, as there may only be one, and the existing one
                // contains the already-connected Litho object
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            RestoreARScale();
            if (!HasARGroundPlane && !IsSelectingGroundPlane)
            {
                StartGroundPlaneSelection();
#if UNITY_EDITOR
                // In the Unity Editor, make the first fake ground plane selection happen sooner
                _groundPlaneSelectionStartTime -= 2.75f;
#endif
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (IsSelectingGroundPlane && Time.unscaledTime - _groundPlaneSelectionStartTime > 3f)
            {
                // In the Unity Editor, fake a ground plane selection to begin the scene without AR
                SelectGroundPlane(null, null);
            }
        }
#endif

        private void OnDestroy()
        {
            if (this == _instance)
            {
                _instance = null;
            }
        }

        private void PlanesChanged(ARPlanesChangedEventArgs e)
        {
            ARPlane oldGround = (_groundCoplanarARPlanes != null && _groundCoplanarARPlanes.Count > 0)
                ? _groundCoplanarARPlanes[0] : null;

            foreach (ARPlane addedPlane in e.added)
            {
                _allARPlanes.Add(addedPlane);
                SetARPlaneVisibility(addedPlane, IsShowingARPlanes);
                if (IsCoplanarWithGroundPlane(addedPlane))
                {
                    _groundCoplanarARPlanes.Add(addedPlane);
                    SetARPlaneInteractability(addedPlane, false);
                    addedPlane.gameObject.SetActive(false);
                }
            }

            bool isCoplanarWithGround, isInCoplanarList;
            foreach (ARPlane updatedPlane in e.updated)
            {
                // Check whether the updated plane should be considered a ground plane and apply
                // necessary changes to its status
                isCoplanarWithGround = IsCoplanarWithGroundPlane(updatedPlane);
                isInCoplanarList = IsInGroundCoplanarList(updatedPlane);
                if (isCoplanarWithGround && !isInCoplanarList)
                {
                    _groundCoplanarARPlanes.Add(updatedPlane);
                }
                else if (!isCoplanarWithGround && isInCoplanarList)
                {
                    _groundCoplanarARPlanes.Remove(updatedPlane);
                }
                SetARPlaneInteractability(
                    updatedPlane, IsSelectingGroundPlane && GetPlaneIsHorizontal(updatedPlane));
                updatedPlane.gameObject.SetActive(IsSelectingGroundPlane || !isCoplanarWithGround);
            }

            foreach (ARPlane removedPlane in e.removed)
            {
                _allARPlanes.Remove(removedPlane);
                _groundCoplanarARPlanes.Remove(removedPlane);
            }

            if (!_hasIssuedOnPotentialGroundPlanesFound
                && (e.added.Count > 0 || e.updated.Count > 0)
                && Time.time - _groundPlaneSelectionStartTime > 3f
                && GroundPlaneOptionsExist)
            {
                _hasIssuedOnPotentialGroundPlanesFound = true;
                OnPotentialGroundPlanesFound?.Invoke();
            }
        }

        public void StartGroundPlaneSelection()
        {
            _groundPlaneSelectionStartTime = Time.unscaledTime;
            if (IsSelectingGroundPlane)
            {
                return;
            }
            ResetGroundPlanes();
            IsSelectingGroundPlane = true;
            SetWorldEnabled(false);
            SetARPlaneVisibilities(true);
            SetARPlaneInteractabilities(true);
            _hasIssuedOnPotentialGroundPlanesFound = false;
            OnGroundPlaneSelectionStarted?.Invoke();
        }

        private void SetWorldEnabled(bool shouldBeEnabled)
        {
            if (FloorObject != null)
            {
                FloorObject.gameObject.SetActive(shouldBeEnabled);
            }
            if (FocusObject != null)
            {
                FocusObject.gameObject.SetActive(shouldBeEnabled);
            }
        }
        private void SetWorldEnabled(Vector3 startPosition)
        {
            // Update the position of the floor object
            if (FloorObject != null)
            {
                FloorObject.position = startPosition;
            }
            if (FocusObject != null)
            {
                FocusObject.position = startPosition;
            }
            SetWorldEnabled(true);
        }
        private void SetWorldEnabled(Vector3 startPosition, Quaternion startRotation)
        {
            // Update the rotation of the floor object
            if (FloorObject != null)
            {
                FloorObject.rotation = startRotation;
            }
            if (FocusObject != null)
            {
                FocusObject.rotation = startRotation;
            }
            SetWorldEnabled(startPosition);
        }

        public static bool GetPlaneIsHorizontal(ARPlane plane)
        {
            return plane != null
                && Vector3.Scale(plane.normal, new Vector3(1, 0, 1)).magnitude < 0.1f;
        }

        public static float GetARPlaneHeight(ARPlane plane)
        {
            return plane.transform.position.y;
        }

        private bool IsCoplanarWithGroundPlane(ARPlane testPlane)
        {
            return HasARGroundPlane
                && Mathf.Abs(GetARPlaneHeight(testPlane) - FloorHeight) < _minGroundPlaneOffset;
        }

        private bool IsInGroundCoplanarList(ARPlane testPlane)
        {
            return HasARGroundPlane && _groundCoplanarARPlanes.Contains(testPlane);
        }

        public void SelectGroundPlane(ARPlane newGroundPlane, Vector3? focusPoint = null)
        {
            if (newGroundPlane == null || GetPlaneIsHorizontal(newGroundPlane))
            {
                ResetGroundPlanes();
                if (newGroundPlane != null)
                {
                    _groundCoplanarARPlanes.Add(newGroundPlane);
                    // Disable any plane that overlaps the ground plane
                    foreach (ARPlane plane in _allARPlanes)
                    {
                        if (IsCoplanarWithGroundPlane(plane)
                            && !_groundCoplanarARPlanes.Contains(plane))
                        {
                            _groundCoplanarARPlanes.Add(plane);
                            SetARPlaneVisibility(plane, false);
                            SetARPlaneInteractability(plane, false);
                            plane.gameObject.SetActive(false);
                        }
                    }
                }

                if (focusPoint != null)
                {
                    FocusPoint = (Vector3)focusPoint;
                }
                else if (newGroundPlane != null)
                {
                    FocusPoint = newGroundPlane.center;
                }
                else if (FocusObject != null)
                {
                    FocusPoint = FocusObject.position;
                }
                else if (FloorObject != null)
                {
                    FocusPoint = FloorObject.position;
                }

                SetWorldEnabled(FocusPoint,
                                Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f));

                if (FloorObject != null)
                {
                    FloorHeight = FloorObject.localPosition.y;
                    Litho.UpdateGroundHeight(FloorHeight);
                    OnFloorHeightChanged?.Invoke(FloorHeight);
                }

                _arOrigin = FindObjectOfType<ARSessionOrigin>();
                if (_arOrigin != null && FocusObject != null)
                {
                    _arOrigin.MakeContentAppearAt(FocusObject, FocusPoint);
                }

                SetARPlaneVisibilities(false);
                SetARPlaneInteractabilities(false);

                IsSelectingGroundPlane = false;
                OnGroundPlaneSelectionCompleted?.Invoke();

                Debug.Log("New ground plane set: "
                          + (newGroundPlane != null ? newGroundPlane.name : "null"));
            }
            else
            {
                Debug.LogWarning("Cannot set ground plane to a non-horizontal plane");
            }
        }

        private void ResetGroundPlanes()
        {
            foreach (ARPlane plane in _groundCoplanarARPlanes)
            {
                plane.gameObject.SetActive(true);
            }
            _groundCoplanarARPlanes.Clear();
        }

        private void SetARPlaneVisibilities(bool shouldBeVisible)
        {
            foreach (ARPlane plane in _allARPlanes)
            {
                SetARPlaneVisibility(plane, shouldBeVisible);
            }

            bool wasShowingARPlanes = _isShowingARPlanes;
            _isShowingARPlanes = shouldBeVisible;
            if (_isShowingARPlanes && !wasShowingARPlanes)
            {
                OnPlaneVisibilityChanged?.Invoke(true);
            }
            else if (!_isShowingARPlanes && wasShowingARPlanes)
            {
                OnPlaneVisibilityChanged?.Invoke(false);
            }
        }

        private void SetARPlaneVisibility(ARPlane plane, bool visible)
        {
            if (plane != null)
            {
                ARPlaneController planeController = plane.GetComponent<ARPlaneController>();
                if (planeController != null)
                {
                    planeController.SetVisibility(visible);
                }
                else
                {
                    plane.GetComponent<LineRenderer>().enabled = visible;
                }
            }
        }

        private void SetARPlaneInteractabilities(bool shouldBeInteractable,
                                                 bool onlyHorizontal = true)
        {
            foreach (ARPlane plane in _allARPlanes)
            {
                SetARPlaneInteractability(
                    plane,
                    shouldBeInteractable && (!onlyHorizontal || GetPlaneIsHorizontal(plane)));
            }
        }

        private void SetARPlaneInteractability(ARPlane plane, bool interactable)
        {
            if (plane != null)
            {
                ARPlaneController planeController = plane.GetComponent<ARPlaneController>();
                if (planeController != null)
                {
                    planeController.SetInteractability(interactable);
                }
            }
        }

        public void SetScale(float newScale)
        {
            if (_arOrigin == null)
            {
                _arOrigin = FindObjectOfType<ARSessionOrigin>();
            }
            if (_arOrigin != null)
            {
                float oldScale = _arOrigin.transform.localScale.magnitude / Vector3.one.magnitude;
                _arOrigin.transform.localScale = Vector3.one / newScale;
                SaveARScale(newScale);
                OnARScaleChanged?.Invoke(newScale, oldScale);

                Camera.main.nearClipPlane = _initialCameraNearPlane / newScale;
                Camera.main.farClipPlane = _initialCameraFarPlane / newScale;
                QualitySettings.shadowDistance = _initialShadowDistance / newScale;
            }
        }

        private void SaveARScale(float scale)
        {
            PlayerPrefs.SetFloat(PREF_KEY_AR_SCALE, scale);
        }

        private void RestoreARScale()
        {
            // Attempt to recover a stored value for AR scale and apply it
            SetScale(PlayerPrefs.GetFloat(PREF_KEY_AR_SCALE, 1f));
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ARManager))]
    [CanEditMultipleObjects]
    public class ARManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.HelpBox("Note that this component will enable the ARSession and " +
                                    "ARCameraManager components as appropriate at runtime; an " +
                                    "ARInputManager and ARPlaneManager will be added at the " +
                                    "start of the first scene, and will persist through all " +
                                    "scene changes to enable persistent AR tracking.",
                                    MessageType.Info);
        }
    }
#endif

}
