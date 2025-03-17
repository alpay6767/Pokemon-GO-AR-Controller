/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Holds a Positionable object at its position
    /// </summary>
    [AddComponentMenu("LITHO/Manipulators/Item Holder", -9898)]
	public class ItemHolder : Manipulator
    {
        [SerializeField]
        [Tooltip("Vector by which to offset proportionally to the radius of the held object")]
        private Vector3 _radiusOffsetVector = Vector3.zero;

        private Manipulable _lastGrabbedManipulable;

        public override bool ShowManipulationIndicator => false;


        protected override void Reset()
        {
            base.Reset();

            Strength = 0.25f;
            ReleaseRange = 0.1f;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (CurrentManipulable != _lastGrabbedManipulable
                && !IsGrabbingManipulable && CouldGrab)
            {
                Grab();
            }
            else
            {
                _lastGrabbedManipulable = null;
            }
        }

        protected override void HandleEnterManipulable()
        {
            base.HandleEnterManipulable();

            // If a Pointer is holding the just-grabbed object
            Pointer pointer = FindObjectOfType<Pointer>();
            if (CurrentManipulable.Manipulations.ContainsKey(pointer)
                && CurrentManipulable.Manipulations[pointer].IsGrabbed)
            {
                Litho.PlayHapticEffect(HapticEffect.Type.TransitionHum_4_40);
            }
        }

        protected override void HandleExitManipulable()
        {
            base.HandleExitManipulable();

            // If a Pointer is holding the just-released object
            Pointer pointer = FindObjectOfType<Pointer>();
            if (LastManipulable.Manipulations.ContainsKey(pointer)
                && LastManipulable.Manipulations[pointer].IsGrabbed)
            {
                Litho.PlayHapticEffect(HapticEffect.Type.TransitionRampDownShortSmooth_1_50_to_0);
            }
        }

        protected override void Hold(bool automateLongHold = true)
        {
            // Do not automate 'long hold' events
            base.Hold(false);
        }

        public override void Release(bool automateTap = true)
        {
            // Do not automate 'tap' events
            base.Release(false);

            _lastGrabbedManipulable = CurrentManipulable;
        }

        protected override Collider FindTargetCollider()
        {
            Collider[] colliders = Physics.OverlapSphere(
                GrabPosition, ReleaseRange,
                ~_ignoreLayers, QueryTriggerInteraction.Ignore);
            Manipulable newManipulable = null;
            foreach (Collider c in colliders)
            {
                if (c != null)
                {
                    newManipulable = GetManipulable(c);
                }
                // If the found object has a Manipulable component, set the object as the target
                if (newManipulable != null && (newManipulable.GetType() == typeof(Positionable)
                    || newManipulable.GetType().IsSubclassOf(typeof(Positionable)))
                    && !transform.IsChildOf(newManipulable.Target))
                {
                    // Check whether the corresponding Manipulable is held by an ItemSpawner
                    bool isHeldByItemSpawner = false;
                    foreach (Manipulator manipulator in newManipulable.Manipulations.Keys)
                    {
                        if (manipulator.GetType() == typeof(ItemSpawner))
                        {
                            isHeldByItemSpawner = true;
                        }
                    }
                    // Only allow this Collider to be grabbed if the corresponding Manipulable is
                    // not an ItemSpawner icon (which it mush be if it is held by an ItemSpawner)
                    if (!isHeldByItemSpawner)
                    {
                        return c;
                    }
                }
            }
            return null;
        }

        protected override bool ShouldRelease()
        {
            // Determine whether the current Manipulable is in range
            Collider[] colliders = Physics.OverlapSphere(
                transform.position, ReleaseRange,
                ~_ignoreLayers, QueryTriggerInteraction.Ignore);
            bool manipulableIsInRange = false;
            foreach (Collider c in colliders)
            {
                if (c != null && CurrentManipulable == GetManipulable(c))
                {
                    manipulableIsInRange = true;
                }
            }
            // If the current Manipulable is still within range
            if (manipulableIsInRange)
            {
                // It should not be released
                return false;
            }
            else
            {
                // Allow the base class to decide
                return base.ShouldRelease();
            }
        }

        protected override Vector3 GetTargetGrabPosition()
        {
            if (IsInteracting)
            {
                return transform.TransformPoint(
                    _radiusOffsetVector * CurrentManipulable.GetApproxRadius());
            }
            return base.GetTargetGrabPosition();
        }

        public override Vector3 GetManipulableIntersectionDirection()
        {
            return _radiusOffsetVector;
        }
    }

}
