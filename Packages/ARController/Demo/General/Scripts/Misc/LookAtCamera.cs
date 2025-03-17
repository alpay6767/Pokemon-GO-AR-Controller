/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Rotates this object to look at the camera
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Look At Camera", -9079)]
    public class LookAtCamera : MonoBehaviour
    {
        private enum RotationMode
        {
            Full,
            YawOnly,
            YawRelativeToParent
        }

        [SerializeField]
        [Tooltip("How to constrain axes of rotation")]
        private RotationMode _rotationMode = RotationMode.Full;

        [SerializeField]
        [Tooltip("Whether to flip the z-axis when calculating facing direction")]
        private bool _flipForwardDirection = false;

        [SerializeField]
        [Tooltip("Rate at which to exponentially approach the target rotation" +
                 "(0: no movement; 0.5: fast movement; 1: instant movement)")]
        [Range(0f, 1f)]
        private float _approachRate = 0.2f;


        private void FixedUpdate()
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                (_flipForwardDirection ? -1f : 1f)
                    * (Camera.main.transform.position - transform.position),
                Vector3.up);

            switch (_rotationMode)
            {
                case RotationMode.Full:
                    transform.rotation = Quaternion.Lerp(
                        transform.rotation, targetRotation, _approachRate);
                    break;
                case RotationMode.YawOnly:
                    targetRotation.eulerAngles = Vector3.up * targetRotation.eulerAngles.y;
                    transform.rotation = Quaternion.Lerp(
                        transform.rotation, targetRotation, _approachRate);
                    break;
                case RotationMode.YawRelativeToParent:
                    targetRotation
                        = Quaternion.Inverse(transform.parent?.rotation ?? Quaternion.identity)
                        * targetRotation;
                    targetRotation.eulerAngles = Vector3.up * targetRotation.eulerAngles.y;
                    transform.localRotation = Quaternion.Lerp(
                        transform.localRotation, targetRotation, _approachRate);
                    break;
            }
        }
    }

}
