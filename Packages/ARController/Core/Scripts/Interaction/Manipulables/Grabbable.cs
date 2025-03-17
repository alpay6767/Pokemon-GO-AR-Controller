/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.Events;

namespace LITHO
{

    /// <summary>
    /// Extends Manipulable with the additional option to make Manipulators either grip or slip off
    /// of this object when it is grabbed
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Grabbable", -9779)]
    public class Grabbable : Manipulable
    {
        [SerializeField]
        [Tooltip("Determines how quickly a Manipulator will slip off of this object when pulled " +
                 "away whilst grabbing it")]
        [Range(0f, 1f)]
        protected float _slipperiness;

        // Determines how quickly a Manipulator will slip off of this object when pulled away
        // whilst grabbing it
        public float Slipperiness => _slipperiness;

        [Tooltip("Occurs, if this object is being interacted with by a Manipulator, when that " +
                 "Manipulator's 'release' action is triggered due to 'exit' being triggered")]
        public ManipulationUnityEvent OnManipulatorExitRelease = new ManipulationUnityEvent();

        [Tooltip("Occurs, if this object is being interacted with by a Manipulator, when that " +
                 "Manipulator's 'release' action without 'exit' being triggered")]
        public ManipulationUnityEvent OnManipulatorNoExitRelease = new ManipulationUnityEvent();

        public override bool SupportsDepthControl => false;


        protected override void Awake()
        {
            base.Awake();

            OnManipulatorRelease += HandleManipulatorRelease;
        }

        protected virtual void OnDestroy()
        {
            OnManipulatorRelease -= HandleManipulatorRelease;
        }

        private void HandleManipulatorRelease(Manipulation manipulation)
        {
            if (manipulation.EndedByExit)
            {
                OnManipulatorExitRelease?.Invoke(manipulation);
            }
            else
            {
                OnManipulatorNoExitRelease?.Invoke(manipulation);
            }
        }

        public override Vector3 GetUpdatedGrabPosition(Manipulation manipulation)
        {
            // Project target grab position onto the plane of interaction
            Vector3 intersection = manipulation.Manipulator.GetManipulableIntersectionPoint();
            Vector3 normal = manipulation.Manipulator.GetManipulableIntersectionDirection();
            Vector3 grabVector = manipulation.Manipulator.TargetGrabPosition
                - manipulation.Manipulator.transform.position;
            // If pulling toward or away from the object, adjust the 
            float normalOffsetAngle = Vector3.Angle(-normal, grabVector);
            if (normalOffsetAngle > 60f)
            {
                normal = Vector3.Slerp(
                    normal, -grabVector.normalized, (normalOffsetAngle - 60f) / 135f);
            }
            Vector3 projectedTarget = intersection + Vector3.ProjectOnPlane(
                manipulation.Manipulator.TargetGrabPosition - intersection, normal);
            // Interpolate as a function of slipperiness towards the target grab position
            return Vector3.Lerp(
                manipulation.Manipulator.GrabPosition, projectedTarget,
                Slipperiness * Slipperiness * Slipperiness * Slipperiness);
        }
    }

}
