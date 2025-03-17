/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Allows a property to be linearly modified by Manipulator motion along a chosen axis
    /// </summary>
    public abstract class SliderManipulable : CentredGrabManipulable
    {
        [SerializeField]
        [Tooltip("Multiplier for how much Manipulator movement affects scale")]
        private float _sensitivityFactor = 4f;

        protected virtual Vector3 Axis => Vector3.up;

        protected abstract float Value { get; }

        protected abstract float MinValue { get; }

        protected abstract float MaxValue { get; }

        private float _initialGrabDistance;
        protected float _initialValue;

        public override bool SupportsDepthControl => false;


        public override void InitializeManipulation(Manipulation manipulation)
        {
            base.InitializeManipulation(manipulation);

            _initialGrabDistance = CalculateGrabDistance(manipulation.Manipulator);
            _initialValue = Value;
        }

        public sealed override void PerformManipulation(Manipulation manipulation)
        {
            base.PerformManipulation(manipulation);

            // Adjust sensitivity based on manipulator distance from the object
            float tempSensitivityFactor = _sensitivityFactor;
            if (Target.position != manipulation.Manipulator.transform.position)
            {
                tempSensitivityFactor /= 1f + 
                    Vector3.Distance(Target.position, manipulation.Manipulator.transform.position);
            }
            // Update the scale of this object
            float distance = CalculateGrabDistance(manipulation.Manipulator);
            float delta = distance - _initialGrabDistance;

            UpdateValue(Mathf.Clamp(_initialValue + delta * tempSensitivityFactor
                                    * (MaxValue - MinValue), MinValue, MaxValue));
        }

        private float CalculateGrabDistance(Manipulator manipulator)
        {
            Vector3 lookDirection = Target.position - manipulator.transform.position;
            Vector3 transformedAxis = Quaternion.LookRotation(lookDirection) * Axis;
            Vector3 pullDirection = manipulator.TargetGrabPosition - Target.position;
            Vector3 projectedPullDirection = Vector3.Project(pullDirection, transformedAxis);

            Debug.DrawRay(Target.position, transformedAxis, Color.magenta);
            Debug.DrawLine(Target.position, Target.position + projectedPullDirection, Color.green);
            Debug.DrawLine(Target.position, Target.position + pullDirection, Color.cyan);

            return projectedPullDirection.magnitude
                * Mathf.Sign(Vector3.Dot(pullDirection, transformedAxis));
        }

        protected abstract void UpdateValue(float targetValue);
    }

}
