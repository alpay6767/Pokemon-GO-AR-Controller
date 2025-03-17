/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace LITHO
{

    public delegate void ManipulationEvent(Manipulation manipulation);
    [System.Serializable]
    public class ManipulationUnityEvent : UnityEvent<Manipulation> { }

    /// <summary>
    /// Handles manipulation of a simple property of the target object when it is interacted with
    /// by a Manipulator; this class should be extended with different property manipulations
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Manipulable", -9780)]
    public class Manipulable : MonoBehaviour
    {
        public enum GrabType
        {
            HoldTargetObject = 0,
            HoldThisHandle
        }

        [SerializeField]
        [Tooltip("Whether this object is currently responding to Manipulator events")]
        private bool _interactable = true;
        public bool Interactable
        {
            get { return _interactable; }
            set
            {
                _interactable = value;
                if (!_interactable && _wasInteractable)
                {
                    EndAllManipulations();
                }
                _wasInteractable = _interactable;
            }
        }
        private bool _wasInteractable = true;

        [SerializeField]
        [Tooltip("Object that should be affected when this object is grabbed " +
                 "(default: closest ancestor with a Rigidbody; otherwise: this object)")]
        private Transform _target;
        public Transform Target
        {
            get { return _target; }
            protected set { _target = value; }
        }

        [SerializeField]
        [Tooltip("Whether to grab onto the target or this handle when grabbed")]
        protected GrabType _grabMode = GrabType.HoldTargetObject;
        public GrabType GrabMode => _grabMode;

        [SerializeField]
        [Tooltip("Prefab to spawn to illustrate that this manipulation is available/ active")]
        private GameObject _indicatorPrefab = null;
        public GameObject IndicatorPrefab => _indicatorPrefab;


        // List of Manipulations currently occuring on this object (indexed by Manipulator)
        public Dictionary<Manipulator, Manipulation> Manipulations { get; private set; }
            = new Dictionary<Manipulator, Manipulation>();

        public bool IsHovered
        {
            get
            {
                // Consider this object 'hovered' if there is at least one Manipulator hovering it
                // (This can be determined by the existence of any one Manipulation, as
                // Manipulations only exist whilst a Manipulator is hovering a Manipulable)
                return Manipulations.Count > 0;
            }
        }

        public int HoverCount
        {
            get
            {
                // Count how many Manipulators are hovering on this object
                // The existence of each Manipulation implies a hovering Manipulator
                return Manipulations.Count;
            }
        }

        public bool IsGrabbed
        {
            get
            {
                // Consider this object 'grabbed' if there is at least one Manipulation grabbing it
                foreach (Manipulation manipulation in Manipulations.Values)
                {
                    if (manipulation.IsGrabbed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int GrabCount
        {
            get
            {
                // Count how many Manipulators are grabbing on this object
                int grabCount = 0;
                foreach (Manipulation manipulation in Manipulations.Values)
                {
                    if (manipulation.IsGrabbed)
                    {
                        grabCount++;
                    }
                }
                return grabCount;
            }
        }

        // Indicates whether this Manipulable responds to depth manipulation (i.e. manipulation
        // beyond the 2D plane of interaction)
        public virtual bool SupportsDepthControl => true;


        // Occurs when a Manipulator start hovering (highlighting) this object
        public event ManipulationEvent OnManipulatorEnter;

        // Occurs once per frame for each Manipulator that is hovering this object
        public event ManipulationEvent OnManipulatorStay;

        // Occurs when a Manipulator is no longer hovering this object, and is also no longer
        // grabbing this object
        public event ManipulationEvent OnManipulatorExit;


        // Occurs when a Manipulator is hovering this object and that Manipulator's 'grab'
        // action is triggered (e.g. for a Pointer, when Litho.OnTouchDown occurs)
        public event ManipulationEvent OnManipulatorGrab;

        // Occurs once per frame between OnManipulatorGrab and OnManipulatorRelease, for each
        // Manipulator that is interacting with this object
        public event ManipulationEvent OnManipulatorHold;

        // Occurs, if this object is being interacted with by a Manipulator, when that
        // Manipulator's 'release' action is triggered (e.g. for a Pointer, when Litho.OnTouchUp
        // occurs)
        public event ManipulationEvent OnManipulatorRelease;

        // Occurs when a Manipulator is hovering this object and that Manipulator's 'tap' action is
        // triggered (when 'release' is triggered a fraction of a second after 'grab')
        public event ManipulationEvent OnManipulatorTap;

        // Occurs when a Manipulator is grabbing this object and that Manipulator's 'long hold'
        // action is triggered (when 'hold' is triggered and it has been more than a fraction of a
        // second since 'grab' was triggered)
        public event ManipulationEvent OnManipulatorLongHold;


        protected virtual void Reset()
        {
            GetDefaultTarget();
        }

        private void OnValidate()
        {
            if (!_interactable && _wasInteractable)
            {
                EndAllManipulations();
            }
            _wasInteractable = _interactable;
        }

        protected virtual void Awake()
        {
            if (Target == null)
            {
                Rigidbody[] rigidbodies = GetComponentsInParent<Rigidbody>(true);
                if (rigidbodies.Length > 0 && rigidbodies[0] != null)
                {
                    Target = rigidbodies[0].transform;
                }
                if (Target == null)
                {
                    Target = transform;
                }
            }

            if (GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogError(this + " is Manipulable, but it does not have a Collider " +
                               "attached to it or to any of its children; this Manipulable " +
                               "component will not work as intended. Attach a Collider to " +
                               this + " or to one of its children to fix this issue.");
            }
        }

        private void OnDestroy()
        {
            EndAllManipulations();
        }

        private void Update()
        {
            if (!_interactable && _wasInteractable)
            {
                EndAllManipulations();
            }
            _wasInteractable = _interactable;
        }

        private void GetDefaultTarget()
        {
            Target = GetComponentInParent<Rigidbody>()?.transform;
            if (Target == null)
            {
                Target = transform;
            }
        }

        private void EnsureHasTarget()
        {
            if (Target == null)
            {
                GetDefaultTarget();
                Debug.LogWarning("Target variable for " + this + " was automatically reset to " +
                                 Target);
            }
        }

        public bool InvokeManipulatorEnter(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (!Manipulations.ContainsKey(manipulator))
            {
                CreateManipulation(manipulator);
            }
            OnManipulatorEnter?.Invoke(Manipulations[manipulator]);
            return true;
        }

        public bool InvokeManipulatorStay(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                OnManipulatorStay?.Invoke(Manipulations[manipulator]);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }

        public bool InvokeManipulatorExit(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                OnManipulatorExit?.Invoke(Manipulations[manipulator]);
                EndManipulation(manipulator);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }

        public bool InvokeManipulatorGrab(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                OnManipulatorGrab?.Invoke(Manipulations[manipulator]);
                InitializeManipulation(Manipulations[manipulator]);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }

        public bool InvokeManipulatorHold(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                OnManipulatorHold?.Invoke(Manipulations[manipulator]);
                PerformManipulation(Manipulations[manipulator]);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }

        public bool InvokeManipulatorRelease(Manipulator manipulator, bool causedByExit = false)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                Manipulations[manipulator].EndedByExit = causedByExit;
                OnManipulatorRelease?.Invoke(Manipulations[manipulator]);
                FinalizeManipulation(Manipulations[manipulator]);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }

        public bool InvokeManipulatorTap(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                OnManipulatorTap?.Invoke(Manipulations[manipulator]);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }

        public bool InvokeManipulatorLongHold(Manipulator manipulator)
        {
            if (!_interactable)
            {
                return false;
            }
            EnsureHasTarget();

            if (Manipulations.ContainsKey(manipulator))
            {
                OnManipulatorLongHold?.Invoke(Manipulations[manipulator]);
                return true;
            }
            else
            {
                Debug.LogWarning("Invalid Manipulation state");
                return false;
            }
        }


        private void CreateManipulation(Manipulator manipulator)
        {
            if (manipulator == null)
            {
                Debug.LogWarning("The provided Manipulator is null; cannot create a Manipulation");
                return;
            }
            Manipulation newManipulation = gameObject.AddComponent<Manipulation>();
            newManipulation.Initialize(this, manipulator, _indicatorPrefab);
            Manipulations.Add(manipulator, newManipulation);
        }

        private void EndManipulation(Manipulator manipulator)
        {
            manipulator.Release(false);
            OnManipulatorExit?.Invoke(Manipulations[manipulator]);
            if (Manipulations[manipulator] != null)
            {
                Destroy(Manipulations[manipulator]);
            }
            Manipulations.Remove(manipulator);
        }

        public void EndAllManipulations()
        {
            while (Manipulations.Count > 0)
            {
                EndManipulation(Manipulations.ElementAt(0).Key);
            }
        }

        public float GetApproxRadius(bool worldScale = true)
        {
            EnsureHasTarget();
            return GetApproxRadius(Target, worldScale);
        }

        public static float GetApproxRadius(Transform target, bool worldScale = true)
        {
            // Assume this object has a zero radius by default
            Vector3 extents = Vector3.zero;
            // See if the radius can be calculated from renderers
            foreach (Renderer rendererOnChild in target.GetComponentsInChildren<Renderer>())
            {
                extents = Vector3.Max(extents, rendererOnChild.bounds.extents);
            }
            // If no significant size was determined from the renderers
            if (extents.sqrMagnitude < Litho.EPSILON)
            {
                // See if the radius can be calculated from colliders
                foreach (Collider col in target.GetComponentsInChildren<Collider>())
                {
                    extents = Vector3.Max(extents, col.bounds.extents);
                }
            }
            return worldScale ? extents.magnitude
                : target.parent.InverseTransformVector(extents).magnitude;
        }

        public Bounds GetApproxBounds(bool includeInactive = false)
        {
            EnsureHasTarget();
            return GetApproxBounds(Target, includeInactive);
        }

        public static Bounds GetApproxBounds(Transform target, bool includeInactive = false)
        {
            // Assume the object has a zero radius by default
            Bounds bounds = new Bounds(target.position, Vector3.zero);
            // See if the radius can be calculated from renderers
            foreach (Renderer rendererOnChild
                     in target.GetComponentsInChildren<Renderer>(includeInactive))
            {
                bounds.Encapsulate(rendererOnChild.bounds);
            }
            // If no significant size was determined from the renderers
            if (bounds.extents.sqrMagnitude < Litho.EPSILON)
            {
                // See if the radius can be calculated from colliders
                foreach (Collider col in target.GetComponentsInChildren<Collider>(includeInactive))
                {
                    bounds.Encapsulate(col.bounds);
                }
            }
            return bounds;
        }

        public virtual Vector3 GetOffsetEulers(Manipulator manipulator)
        {
            return transform.eulerAngles;
        }

        public virtual void InitializeManipulation(Manipulation manipulation) { }

        public virtual void PerformManipulation(Manipulation manipulation) { }

        public virtual void FinalizeManipulation(Manipulation manipulation) { }

        public virtual Vector3 GetUpdatedGrabPosition(Manipulation manipulation)
        {
            return manipulation.Manipulator.TargetGrabPosition;
        }

        public Vector3 ConvertToWorldSpace(Vector3 point)
        {
            if (GrabMode == GrabType.HoldTargetObject)
            {
                EnsureHasTarget();
                return Target.TransformPoint(point);
            }
            else
            {
                return transform.TransformPoint(point);
            }
        }

        public Vector3 ConvertToLocalSpace(Vector3 point)
        {
            if (GrabMode == GrabType.HoldTargetObject)
            {
                EnsureHasTarget();
                return Target.InverseTransformPoint(point);
            }
            else
            {
                return transform.InverseTransformPoint(point);
            }
        }
    }

}
