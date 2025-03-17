/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Grabs items near its position and deletes them
    /// </summary>
    [AddComponentMenu("LITHO/Manipulators/Item Deleter", -9896)]
    public class ItemDeleter : ItemHolder
    {
        [SerializeField]
        [Tooltip("Vector by which to offset as the deletion target shrinks")]
        private Vector3 _deletionOffsetVector = Vector3.zero;

        [SerializeField]
        [Tooltip("Length of time over which to shrink objects to deletion")]
        private float _deletionTime = 0.5f;

        [SerializeField]
        [Tooltip("Whether to rapidly delete objects")]
        private bool _fastMode = false;
        public bool FastMode { get { return _fastMode; } set { _fastMode = value; } }

        private Transform _deletionTarget;

        private Vector3 _deletionTargetInitialPosition, _deletionTargetInitialScale;

        private float _deletionProgress;

        private Vector3 DeletionPosition => transform.TransformPoint(_deletionOffsetVector);


        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            // If not already deleting something
            // and this is the only thing holding the current Manipulable
            // and it is close to the target grab position
            if (_deletionTarget == null
                && CurrentManipulable != null && CurrentManipulable.Manipulations.ContainsKey(this)
                && CurrentManipulable.Manipulations[this].IsGrabbed
                && CurrentManipulable.GrabCount == 1
                && (Vector3.Distance(CurrentManipulable.Target.transform.position,
                                     TargetGrabPosition) < ReleaseRange * 0.4f
                    || Vector3.Angle(
                        CurrentManipulable.Target.transform.position - DeletionPosition,
                        TargetGrabPosition - DeletionPosition) < 20f * (_fastMode ? 4f : 1f)))
            {
                // Turn the current Manipulable into the deletion target
                _deletionTarget = CurrentManipulable.Target.transform;
                // Disable all Manipulables associated with the current Manipulable
                foreach (Manipulable manipulable
                         in CurrentManipulable.Target.GetComponentsInChildren<Manipulable>())
                {
                    manipulable.Interactable = false;
                }

                _deletionTargetInitialPosition = CurrentManipulable.Target.transform.position;
                _deletionTargetInitialScale = _deletionTarget.localScale;
                _deletionProgress = 0f;
            }
            if (_deletionTarget != null)
            {
                _deletionTarget.position
                    = _deletionTargetInitialPosition * (1f - _deletionProgress)
                    + DeletionPosition * _deletionProgress;
                _deletionTarget.localScale = _deletionTargetInitialScale * (1f - _deletionProgress);

                // Increase the deletion progress (twice as fast if another Manipulable is waiting)
                _deletionProgress += Time.fixedDeltaTime * (_fastMode ? 3f : 1f)
                    / _deletionTime;

                // If shrinkage is complete, delete the object
                if (_deletionProgress >= 1f)
                {
                    Destroy(_deletionTarget.gameObject);
                }
            }
        }

        public override Vector3 GetManipulableIntersectionDirection()
        {
            return _deletionOffsetVector;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(DeletionPosition, 0.025f);
            if (_deletionTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_deletionTargetInitialPosition, 0.025f);
            }
        }
#endif
    }

}
