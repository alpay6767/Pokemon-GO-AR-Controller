/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Attempts to pull other alignment points in line with itself
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Alignment Point", -9077)]
    [RequireComponent(typeof(SphereCollider))]
    public class AlignmentPoint : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How much force to apply to stay aligned with other alignment points")]
        private float _strength = 100f;

        [SerializeField]
        [Tooltip("The group that this alignment point belongs to")]
        private int _group = 0;

        [SerializeField]
        [Tooltip("The group that this alignment point targets")]
        private int _targetGroup = 0;

        private Rigidbody _rb;


        private void OnValidate()
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
        }

        private void Awake()
        {
            _rb = GetComponentInParent<Rigidbody>();
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (!enabled)
            {
                return;
            }
            AlignmentPoint other = otherCollider.GetComponent<AlignmentPoint>();
            // Avoid targeting nothing, self or members of the wrong group
            // and do not bother with calculations if there are no Rigidbodies to affect
            if (other == null || !other.enabled || this == other
                || other._group != _targetGroup
                || (_rb == null && other._rb == null))
            {
                return;
            }
            // Apply equal and opposite forces to this and the other alignment point
            Vector3 offset = other.transform.position - transform.position;
            if (_rb != null)
            {
                _rb.AddForceAtPosition(offset * _strength, transform.position);
            }
            if (other._rb != null)
            {
                other._rb.AddForceAtPosition(offset * -_strength, other.transform.position);
            }
        }
    }

}
