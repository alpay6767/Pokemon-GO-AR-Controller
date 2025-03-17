/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Allows instances of the given prefab to be grabbed and cloned from its posiiton
    /// </summary>
    [AddComponentMenu("LITHO/Manipulators/Item Spawner", -9897)]
    public class ItemSpawner : Manipulator
    {
        [Header("Spawner Properties")]

        [SerializeField]
        [Tooltip("Prefab of the object that this spawner creates")]
        private GameObject _spawnPrefab = null;

        [SerializeField]
        [Tooltip("Prefab of a container to display around the spawn icon")]
        private GameObject _iconContainerPrefab = null;

        [SerializeField]
        [Tooltip("Scale to use for the icon (also affected by uniform scale of this Transform)")]
        private Vector3 _iconScale = Vector3.one;

        [SerializeField]
        [Tooltip("Scale factor for the inner icon to avoid completely filling the container")]
        private float _containerFillFactor = 0.85f;

        [SerializeField]
        [Tooltip("Transform into which objects should be spawned; note that the parent scale " +
                 "will apply to the spawned prefab")]
        private Transform _spawnParent = null;

        [SerializeField]
        [Tooltip("Scale factor relative to the scale of the spawn prefab that should be applied " +
                 "to spawned objects; note that this is relevant to the spawn parent, if given")]
        public float SpawnPrefabScaleFactor = 1f;

        private GameObject _icon;

        private List<Manipulator> _manipulatorsHoldingIcon = new List<Manipulator>();

        private bool _isResetting;

        public override bool ShowManipulationIndicator => false;

        private readonly List<Type> KEPT_COMPONENTS = new List<Type>()
        {
            typeof(Transform),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(SphereCollider),
            typeof(BoxCollider),
            typeof(CapsuleCollider),
            typeof(WheelCollider),
            typeof(MeshCollider),
            typeof(ReflectionProbe)
        };


        protected override void Reset()
        {
            base.Reset();

            Strength = 0.5f;
            ReleaseRange = 0.1f;
        }

        protected override void Awake()
        {
            base.Awake();

            if (_spawnPrefab == null)
            {
                Debug.LogWarning("No spawn prefab was set on " + this + "; this ItemSpawner " +
                                 "will not work as intended. Provide a spawn prefab on the " +
                                 this + " component to fix this.");
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_icon != null)
            {
                Destroy(_icon);
            }
        }

        protected override void FixedUpdate()
        {
            if (_spawnPrefab != null)
            {
                if (_icon == null || _icon.GetComponent<Positionable>() == null)
                {
                    GenerateIcon();
                }
                else if (_icon != null)
                {
                    if (_icon.GetComponent<Rigidbody>().isKinematic)
                    {
                        _icon.GetComponent<Rigidbody>().isKinematic = false;
                    }
                    if (_isResetting)
                    {
                        _icon.transform.localScale = _icon.transform.localScale * 0.85f;
                        if (_icon.transform.localScale.sqrMagnitude < 0.001f)
                        {
                            _icon.transform.position = TargetGrabPosition;
                            _icon.transform.rotation = Quaternion.Slerp(
                                GetTargetRotationOffset(), _icon.transform.rotation, 0.5f);
                            _isResetting = false;
                        }
                    }
                    else
                    {
                        _icon.transform.localScale
                             = _icon.transform.localScale * 0.85f + _iconScale * 0.15f;
                    }

                    if (Vector3.Distance(_icon.transform.position, TargetGrabPosition)
                        > ReleaseRange * 2f
                        || (Vector3.Distance(_icon.transform.localScale, _iconScale) < 0.01f
                            && Quaternion.Angle(_icon.transform.rotation,
                                                GetTargetRotationOffset()) > 60f))
                    {
                        _isResetting = true;
                    }
                }

                base.FixedUpdate();

                if (!IsGrabbingManipulable)
                {
                    Grab();
                }
            }
        }

        protected override Collider FindTargetCollider()
        {
            return CurrentCollider;
        }

        private void HandleGrabIcon(Manipulation manipulation)
        {
            if (manipulation.Manipulator == this)
            {
                return;
            }
            _manipulatorsHoldingIcon.Add(manipulation.Manipulator);
        }

        private void HandleReleaseIcon(Manipulation manipulation)
        {
            if (manipulation.Manipulator == this && _manipulatorsHoldingIcon.Count > 0)
            {
                Spawn();
            }
            else if (_manipulatorsHoldingIcon.Contains(manipulation.Manipulator))
            {
                _manipulatorsHoldingIcon.Remove(manipulation.Manipulator);
            }
        }

        private void GenerateIcon()
        {
            // Spawn an empty GameObject as the prefab icon
            if (_iconContainerPrefab != null)
            {
                _icon = Instantiate(
                    _iconContainerPrefab, transform.position, transform.rotation, transform);
                _icon.name = "Icon(" + _spawnPrefab.name + ")";
            }
            else
            {
                _icon = new GameObject("Icon(" + _spawnPrefab.name + ")");
                _icon.transform.SetParent(transform, false);
            }

            // Spawn a representation of the prefab to show what will be spawned
            GameObject prefabRep = Instantiate(_spawnPrefab, _icon.transform);

            // Remove all irrelevant/ incompatible components from the prefab representation
            List<Component> components
                = new List<Component>(_icon.GetComponentsInChildren<Component>(true));
            // Sort components by their dependency depths (delete components that are not
            // dependencies first
            components.Sort(
                (c1, c2) => GetRequireComponentDepth(c2) - GetRequireComponentDepth(c1));
            foreach (Component component in components)
            {
                if (!KEPT_COMPONENTS.Contains(component.GetType()))
                {
                    if (component.GetType() == typeof(Rigidbody))
                    {
                        // Set the Rigidbody kinematic to avoid Collider/ CoM recalculations
                        ((Rigidbody)component).isKinematic = true;
                        // If this Rigidbody is not on the root icon object
                        if (component.gameObject != _icon)
                        {
                            Destroy(component);
                        }
                    }
                    else
                    {
                        Destroy(component);
                    }
                }
            }

            // Find or add SphereCollider, Rigidbody, and Movable components to the icon so it
            // can be easily grabbed and moved
            Collider iconCollider = _icon.GetComponentInChildren<Collider>();
            if (iconCollider == null)
            {
                iconCollider = _icon.AddComponent<SphereCollider>();
            }
            if (iconCollider.GetType() == typeof(SphereCollider))
            {
                _iconScale = Vector3.one * Mathf.Min(_iconScale.x, _iconScale.y, _iconScale.z);
            }
            // Find or attach a Rigidbody
            Rigidbody iconRigidbody = _icon.GetComponent<Rigidbody>();
            if (iconRigidbody == null)
            {
                iconRigidbody = _icon.AddComponent<Rigidbody>();
            }
            // Make it kinematic to keep spawn position aligned
            iconRigidbody.isKinematic = true;
            iconRigidbody.useGravity = false;
            // Listen for grab and release events on the icon
            Movable iconMovable = _icon.AddComponent<Movable>();
            iconMovable.OnManipulatorGrab += HandleGrabIcon;
            iconMovable.OnManipulatorRelease += HandleReleaseIcon;

            // Scale components as required to fit inside the ItemSpawner
            float scaleFactor;
            if (_iconContainerPrefab == null)
            {
                Bounds innerBounds = Manipulable.GetApproxBounds(_spawnPrefab.transform);
                scaleFactor = Mathf.Min(_iconScale.x / innerBounds.size.x,
                                        _iconScale.y / innerBounds.size.y,
                                        _iconScale.z / innerBounds.size.z);
            }
            else if (iconCollider.GetType() == typeof(BoxCollider))
            {
                BoxCollider box = (BoxCollider)iconCollider;
                Bounds innerBounds = Manipulable.GetApproxBounds(_spawnPrefab.transform);
                scaleFactor = Mathf.Min(_iconScale.x * box.size.x / innerBounds.size.x,
                                        _iconScale.y * box.size.y / innerBounds.size.y,
                                        _iconScale.z * box.size.z / innerBounds.size.z);
            }
            else
            {
                scaleFactor = 0.5f / Manipulable.GetApproxRadius(prefabRep.transform, false);
            }
            prefabRep.transform.localScale = Vector3.Scale(
                _spawnPrefab.transform.localScale, new Vector3(
                    1f / _iconScale.x, 1f / _iconScale.y, 1f / _iconScale.z)) * scaleFactor;
            if (_iconContainerPrefab != null)
            {
                prefabRep.transform.localScale *= _containerFillFactor;
            }

            // Prepare icon to be scaled up to full size from nothing
            _icon.transform.localScale = Vector3.zero;

            // Make sure the current collider is updated, as this is not done automatically
            CurrentCollider = iconCollider;
        }

        private int GetRequireComponentDepth(Component component)
        {
            int depth = 0;
            RequireComponent[] requiredComponentsAtts = Attribute.GetCustomAttributes(
                component.GetType(), typeof(RequireComponent), true) as RequireComponent[];
            foreach (RequireComponent rc in requiredComponentsAtts)
            {
                if (rc != null && component.GetComponent(rc.m_Type0) != null)
                {
                    depth = Math.Max(
                        depth, GetRequireComponentDepth(component.GetComponent(rc.m_Type0)) + 1);
                }
            }
            return depth;
        }

        private void Spawn()
        {
            GameObject spawnedObject = null;
            Positionable spawnedPositionable = null;
            // For every Manipulator that is holding the icon (typically only one Manipulator)
            for (int m = _manipulatorsHoldingIcon.Count - 1; m >= 0; m--)
            {
                // Spawn an instance of the given prefab
                spawnedObject = Instantiate(
                    _spawnPrefab, _icon.transform.position, _icon.transform.rotation, _spawnParent);
                spawnedObject.transform.localScale *= SpawnPrefabScaleFactor;

                // Find a Positionable on the spawned object (or attach one)
                spawnedPositionable = spawnedObject.GetComponentInChildren<Positionable>(true);
                if (spawnedPositionable != null)
                {
                    if (_manipulatorsHoldingIcon[m].GetType() == typeof(Pointer))
                    {
                        Litho.PlayHapticEffect(
                            HapticEffect.Type.TransitionRampUpShortSmooth_1_0_to_50);
                    }
                    // Hand the spawned Positionable to the Manipulator that was trying to spawn it
                    _manipulatorsHoldingIcon[m].GrabManipulable(spawnedPositionable);
                }
                else
                {
                    Debug.LogWarning("Prefab spawned by " + this + " does not have a Positionable " +
                                     "component attached to it, so cannot be moved by " +
                                     _manipulatorsHoldingIcon[m] + "; attach a Positionable " +
                                     "component to the " + _spawnPrefab.name + " prefab or " +
                                     "select a different prefab for " + this + " to fix this " +
                                     "issue.");
                    // Force the Manipulator to let go of the icon
                    _manipulatorsHoldingIcon[m].Release();
                }
            }
            // Move the icon back to its origin and prepare it to scale into view again
            _icon.transform.localPosition = Vector3.zero;
            _icon.transform.localScale = Vector3.zero;
        }

        public override Quaternion GetIconRotation()
        {
            if (Vector3.Distance(GrabPosition, transform.position) > DISTANCE_OFFSET)
            {
                return Quaternion.LookRotation(transform.position - GrabPosition, Vector3.up);
            }
            return transform.rotation;
        }
    }

}
