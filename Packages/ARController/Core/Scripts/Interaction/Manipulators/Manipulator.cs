/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Handles hovering and grabbing of Litho Manipulables
    /// </summary>
    public abstract class Manipulator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Strength factor (relative to other Manipulators)")]
        [Range(0f, 10f)]
        private float _strength = 1f;
        public float Strength
        {
            get
            {
                return _strength;
            }
            protected set
            {
                _strength = value;
            }
        }

        [SerializeField]
        [Tooltip("The layers to collide with when interacting with objects")]
        protected LayerMask _ignoreLayers;
        public LayerMask IgnoreLayers
        {
            get
            {
                return _ignoreLayers;
            }
            set
            {
                _ignoreLayers = value;
            }
        }

        [SerializeField]
        [Tooltip("Range at which to give up and release a grabbed object (-1 for no limit)")]
        private float _releaseRange = -1f;
        public float ReleaseRange
        {
            get
            {
                return _releaseRange * transform.lossyScale.magnitude / Vector3.one.magnitude;
            }
            set
            {
                _releaseRange = value;
            }
        }

        public Manipulable CurrentManipulable { get; private set; } = null;
        protected Manipulable LastManipulable { get; private set; } = null;
        public Collider CurrentCollider { get; protected set; } = null;

        // Whether a grab interaction is being performed
        public bool IsGrabbing { get; private set; }
        // More specifically, whether a manipulable was targeted when the grab started
        public bool IsGrabbingManipulable { get; private set; }

        public float GrabStartTime { get; private set; }

        public bool CouldGrab => CurrentManipulable?.Manipulations.ContainsKey(this) ?? false;

        public bool IsInteracting => CouldGrab && IsGrabbingManipulable;

        public Manipulation CurrentManipulation
            => CouldGrab ? CurrentManipulable?.Manipulations[this] : null;

        // Where this Manipulator is holding the currently-interacted ManipulationController
        public Vector3 GrabPosition { get; protected set; }
        // Where this Manipulator is trying to move the GrabPosition position to
        public Vector3 TargetGrabPosition { get; protected set; } = Vector3.zero;

        public virtual bool ShowManipulationIndicator => true;

        protected const float DISTANCE_OFFSET = 0.05f;


        protected virtual void Reset()
        {
            _ignoreLayers.value =
                (1 << LayerMask.NameToLayer("TransparentFX"))
                | (1 << LayerMask.NameToLayer("Ignore Raycast"))
                | (1 << LayerMask.NameToLayer("Water"))
                | (1 << LayerMask.NameToLayer("UI"));

            UpdateGrabPosition();
        }

        protected virtual void Awake()
        {
            UpdateGrabPosition();
        }

        protected virtual void FixedUpdate()
        {
            Move();

            if (IsInteracting)
            {
                Hold();
            }

            if (ShouldRelease())
            {
                Release();
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateGrabPosition();

            // Check for changes to the currently targeted Collider
            CurrentCollider = FindTargetCollider();
            // Switch to a new Manipulable if one is found
            HoverNewManipulable(GetManipulable(CurrentCollider), true);
        }

        protected virtual void OnDisable()
        {
            Release();
            CurrentCollider = null;
            CurrentManipulable = null;
            LastManipulable = null;
        }

        protected virtual void Move()
        {
            if (CurrentManipulable != null)
            {
                CurrentManipulable.InvokeManipulatorStay(this);
            }
        }

        public virtual void Grab()
        {
            if (IsGrabbingManipulable)
            {
                return;
            }
            IsGrabbing = true;
            GrabStartTime = Time.time;
            if (CurrentManipulable != null && CurrentManipulable.InvokeManipulatorGrab(this))
            {
                IsGrabbingManipulable = true;
            }
        }

        protected virtual void Hold(bool automateLongHold = true)
        {
            if (!IsGrabbingManipulable)
            {
                return;
            }
            if (CurrentManipulable != null)
            {
                CurrentManipulable.InvokeManipulatorHold(this);
            }
            if (automateLongHold && Time.time - GrabStartTime > Litho.TAP_TIME)
            {
                LongHold();
            }
        }

        public virtual void Release(bool automateTap = true)
        {
            if (!IsGrabbing)
            {
                return;
            }
            IsGrabbing = false;
            if (!IsGrabbingManipulable)
            {
                return;
            }
            IsGrabbingManipulable = false;
            if (CurrentManipulable != null)
            {
                CurrentManipulable.InvokeManipulatorRelease(this);
            }
            if (automateTap && Time.time - GrabStartTime <= Litho.TAP_TIME)
            {
                Tap();
            }
        }

        protected virtual void Tap()
        {
            if (CurrentManipulable != null)
            {
                CurrentManipulable.InvokeManipulatorTap(this);
            }
        }

        protected virtual void LongHold()
        {
            if (CurrentManipulable != null)
            {
                CurrentManipulable.InvokeManipulatorLongHold(this);
            }
        }

        protected abstract Collider FindTargetCollider();

        protected Manipulable GetManipulable(Collider targetCollider)
        {
            // If a Collider is given, it is possible to find a relevant Manipulable
            if (targetCollider != null)
            {
                if (CurrentManipulable != null
                    && targetCollider.transform.IsChildOf(CurrentManipulable.transform))
                {
                    return CurrentManipulable;
                }
                foreach (Manipulable manipulable
                         in targetCollider.GetComponentsInParent<Manipulable>(true))
                {
                    // If the next Manipulable in the list is usable
                    if (manipulable != null && manipulable.Interactable
                        && !transform.IsChildOf(manipulable.transform))
                    {
                        // Use this Manipulable
                        return manipulable;
                    }
                }
            }
            return null;
        }

        protected virtual void UpdateGrabPosition()
        {
            TargetGrabPosition = GetTargetGrabPosition();
            GrabPosition = IsInteracting
                ? CurrentManipulable.GetUpdatedGrabPosition(CurrentManipulation)
                : TargetGrabPosition;
        }

        protected virtual Vector3 GetTargetGrabPosition()
        {
            return transform.position;
        }

        public Collider HoverNewManipulable(Manipulable newManipulable, bool causedByExit = false)
        {
            LastManipulable = CurrentManipulable;
            CurrentManipulable = newManipulable;

            // If the new object is not already being hovered
            if (LastManipulable != CurrentManipulable)
            {
                if (LastManipulable != null)
                {
                    if (IsGrabbingManipulable)
                    {
                        // Trigger a Manipulator 'release' event on the old Manipulable
                        LastManipulable.InvokeManipulatorRelease(this, causedByExit);
                    }
                }
                if (LastManipulable != null)
                {
                    // Trigger a Manipulator 'exit' event on the old Manipulable
                    LastManipulable.InvokeManipulatorExit(this);
                    HandleExitManipulable();
                }
                if (CurrentManipulable != null)
                {
                    // Trigger a Manipulator 'enter' event on the new Manipulable
                    CurrentManipulable.InvokeManipulatorEnter(this);
                    HandleEnterManipulable();
                }
            }
            return CurrentManipulable?.GetComponentInChildren<Collider>();
        }

        protected virtual void HandleEnterManipulable() { }

        protected virtual void HandleExitManipulable() { }

        protected virtual bool ShouldRelease()
        {
            return ReleaseRange > 0f
                && Vector3.Distance(GrabPosition, TargetGrabPosition) > ReleaseRange;
        }

        public void GrabManipulable(Manipulable newManipulable)
        {
            // Release the current Manipulable
            Release();
            // Switch which object is being hovered
            CurrentCollider = HoverNewManipulable(newManipulable);
            // Grab the new Manipulable
            Grab();
        }

        public virtual Vector3 GetPointInFrontOfTarget(float offsetDistance = 0f)
        {
            float distance = GetDistanceInFrontOfTarget();
            distance += offsetDistance;

            return GrabPosition - transform.forward * distance;
        }

        protected virtual float GetDistanceInFrontOfTarget()
        {
            return CurrentManipulable.GetApproxRadius() * 1.1f + DISTANCE_OFFSET;
        }

        public virtual Quaternion GetTargetRotationOffset()
        {
            return transform.rotation;
        }

        public virtual Vector3 GetManipulableIntersectionPoint()
        {
            return GrabPosition;
        }

        public virtual Vector3 GetManipulableIntersectionDirection()
        {
            return transform.forward;
        }

        public virtual Quaternion GetIconRotation()
        {
            return transform.rotation;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                UpdateGrabPosition();
            }
            if (enabled)
            {
                Gizmos.color = Color.red;
                // Draw a sphere indicating the release range
                if (ReleaseRange >= 0f)
                {
                    Gizmos.DrawWireSphere(TargetGrabPosition, ReleaseRange);
                }
                if (Application.isPlaying)
                {
                    // Draw the target grab position
                    Gizmos.DrawSphere(TargetGrabPosition, 0.05f);
                    Gizmos.DrawLine(transform.position, TargetGrabPosition);

                    // Draw the grab position
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(GrabPosition, 0.05f);
                    Gizmos.DrawLine(transform.position, GrabPosition);
                }
            }
        }
#endif
    }

}
