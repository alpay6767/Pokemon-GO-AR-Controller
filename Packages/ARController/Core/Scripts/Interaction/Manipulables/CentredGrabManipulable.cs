/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Handles manipulation of the target object, with the additional feature of transitioning the
    /// grab point of the target object towards the object centre
    /// </summary>
    public class CentredGrabManipulable : Manipulable
    {
        [SerializeField]
        [Tooltip("Time period over which to transition the grab point from the object surface " +
                 "to its pivot; use -1 to simply grab the surface of objects")]
        private float _recentreGrabPointTime = 0.5f;
        public float RecentreGrabPointTime { get { return _recentreGrabPointTime; } }


        public override Vector3 GetUpdatedGrabPosition(Manipulation manipulation)
        {
            if (IsGrabbed && RecentreGrabPointTime >= 0f)
            {
                if (Time.time <= manipulation.Manipulator.GrabStartTime + RecentreGrabPointTime)
                {
                    // Gradually move the Manipulator's grab point to the centre of the handle
                    float interpolant = Mathf.InverseLerp(
                        manipulation.Manipulator.GrabStartTime,
                        manipulation.Manipulator.GrabStartTime + RecentreGrabPointTime,
                        Time.time);

                    return Vector3.Lerp(manipulation.InitialGrabPosition,
                                        ConvertToWorldSpace(Vector3.zero),
                                        interpolant);
                }
                else
                {
                    return ConvertToWorldSpace(Vector3.zero);
                }
            }
            return base.GetUpdatedGrabPosition(manipulation);
        }
    }

}
