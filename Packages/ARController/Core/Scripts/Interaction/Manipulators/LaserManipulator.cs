/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Implements a ranged Manipulator by raycasting forwards from the transform position (the
    /// laser source) to a target position, allowing selection of things the laser is pointed at
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public abstract class LaserManipulator : Manipulator
    {
        protected float InitialManipulableDepth { get; private set; }
        private float _depth;
        protected abstract float DepthFactor { get; }

        protected float InitialYawOffset { get; private set; }
        protected abstract Quaternion OffsetRotation { get; }

        private RaycastHit _laserHit;

        private float _initialLaserWidth;

        private LineRenderer _laser;

        protected const float DEFAULT_DISTANCE_IN_FRONT_OF_SOURCE = 1.5f;
        public const float MAX_RAYCAST_DISTANCE = 1000f;


        protected override void Awake()
        {
            base.Awake();

            _initialLaserWidth = _laser.widthMultiplier;

            ARManager arManager = FindObjectOfType<ARManager>();
            if (arManager != null)
            {
                arManager.OnARScaleChanged += HandleARScaleChanged;
            }
        }

        protected override void FixedUpdate()
        {
            // Shoot the laser off into the distance
            // (note: this is overridden if an object is targeted)
            _depth += Mathf.Min(MAX_RAYCAST_DISTANCE - _depth, 50f * Time.deltaTime);

            base.FixedUpdate();
        }

        protected virtual void OnDestroy()
        {
            ARManager arManager = FindObjectOfType<ARManager>();
            if (arManager != null)
            {
                arManager.OnARScaleChanged -= HandleARScaleChanged;
            }
        }

        public override void Grab()
        {
            base.Grab();

            InitialManipulableDepth = Vector3.Project(
                GrabPosition - transform.position, transform.forward).magnitude;
            InitialYawOffset = 0f;
            if (CurrentManipulable != null)
            {
                InitialYawOffset = CurrentManipulable.GetOffsetEulers(this).y;
            }
        }

        protected override Collider FindTargetCollider()
        {
            if (IsInteracting)
            {
                RaycastAlongLaser(ref _laserHit, CurrentManipulable);
            }
            else
            {
                RaycastForward(ref _laserHit);
            }

            return _laserHit.collider;
        }

        protected override void UpdateGrabPosition()
        {
            base.UpdateGrabPosition();

            UpdateLaser();
        }

        protected sealed override Vector3 GetTargetGrabPosition()
        {
            _depth = MAX_RAYCAST_DISTANCE;
            if (IsInteracting)
            {
                _depth = InitialManipulableDepth * DepthFactor;
            }
            else if (_laserHit.collider != null)
            {
                // Use the hit point from the last laser raycast
                return _laserHit.point;
            }
            return transform.position + transform.forward * _depth;
        }

        public sealed override Quaternion GetTargetRotationOffset()
        {
            return Quaternion.LookRotation(_laser.GetPosition(_laser.positionCount - 1)
                                               - _laser.GetPosition(_laser.positionCount - 2),
                                           Vector3.up) * OffsetRotation
                * Quaternion.Euler(0f, InitialYawOffset, 0f);
        }

        public override Quaternion GetIconRotation()
        {
            if (Vector3.Distance(GrabPosition, transform.position) > DISTANCE_OFFSET)
            {
                return Quaternion.LookRotation(_laser.GetPosition(_laser.positionCount - 2)
                                               - _laser.GetPosition(_laser.positionCount - 1),
                                               Vector3.up);
            }
            return transform.rotation;
        }

        private void UpdateLaser()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (_laser == null)
            {
                _laser = GetComponent<LineRenderer>();
            }
            _laser.useWorldSpace = true;
            _laser.loop = false;
            if (_laser.positionCount < 21)
            {
                _laser.positionCount = 21;
            }
            Vector3 directionA = TargetGrabPosition - transform.position;
            Vector3 directionB = GrabPosition - transform.position;
            float countFactor = 1.0f / (_laser.positionCount - 1f);
            float switchFactor = 0f;
            for (int p = 0; p < _laser.positionCount; p++)
            {
                _laser.SetPosition(p, transform.position
                                  + directionA * (1 - switchFactor) * switchFactor
                                  + directionB * switchFactor * switchFactor);
                switchFactor += countFactor;
            }
        }

        protected void RaycastForward(ref RaycastHit hit, Collider confirmCollider = null)
        {
            RaycastHit newHit = new RaycastHit();
            Physics.Raycast(transform.position, transform.forward, out newHit,
                            MAX_RAYCAST_DISTANCE, ~_ignoreLayers,
                            QueryTriggerInteraction.Ignore);
            if (confirmCollider == null || newHit.collider == confirmCollider)
            {
                hit = newHit;
                return;
            }
            hit = new RaycastHit();
        }

        protected void RaycastAlongLaser(ref RaycastHit hit, Manipulable confirmManipulable = null)
        {
            if (_laser == null)
            {
                return;
            }
            RaycastHit newHit = new RaycastHit();
            Vector3 difference = transform.forward;
            for (int p = 0; p < _laser.positionCount - 1; p++)
            {
                difference = _laser.GetPosition(p + 1) - _laser.GetPosition(p);
                if (Physics.Raycast(_laser.GetPosition(p), difference, out newHit,
                                    difference.magnitude * 1.001f, ~_ignoreLayers,
                                    QueryTriggerInteraction.Ignore)
                    && (confirmManipulable == null
                        || newHit.collider.transform == confirmManipulable.Target
                        || newHit.collider.transform.IsChildOf(confirmManipulable.Target)))
                {
                    hit = newHit;
                    return;
                }
            }
            // Check if an object is hit beyond the tip of the laser, in the direction of the tip
            if (Physics.Raycast(_laser.GetPosition(_laser.positionCount - 1), difference,
                                out newHit, MAX_RAYCAST_DISTANCE, ~_ignoreLayers,
                                QueryTriggerInteraction.Ignore)
                    && (confirmManipulable == null
                        || newHit.collider.transform == confirmManipulable.Target
                        || newHit.collider.transform.IsChildOf(confirmManipulable.Target)))
            {
                hit = newHit;
                return;
            }
            if (confirmManipulable != null)
            {
                // Also check starting from behind the laser for the case where the object is
                // held very close (and so entirely contains the rendered laser)
                float distance = confirmManipulable.GetApproxRadius() * 2f;
                if (Physics.Raycast(
                    transform.position - transform.forward * distance, transform.forward,
                    out newHit, distance * 1.001f, ~_ignoreLayers, QueryTriggerInteraction.Ignore)
                    && (newHit.collider.transform == confirmManipulable.Target
                        || newHit.collider.transform.IsChildOf(confirmManipulable.Target)))
                {
                    hit = newHit;
                    return;
                }
            }
            hit = new RaycastHit();
        }

        public Vector3 GetPointOnLaserFromSource(
            float distance = DEFAULT_DISTANCE_IN_FRONT_OF_SOURCE)
        {
            // TODO: Make this trace along LineRenderer points, rather than just the first section
            return transform.position + (_laser.GetPosition(1) - _laser.GetPosition(0)).normalized
                * GetDistanceInFrontOfTarget();
        }

        public override Vector3 GetPointInFrontOfTarget(float offsetDistance = 0f)
        {
            // TODO: Make this trace along LineRenderer points, rather than just the last section
            return GrabPosition + (_laser.GetPosition(_laser.positionCount - 2)
                                   - _laser.GetPosition(_laser.positionCount - 1)).normalized
                                       * (GetDistanceInFrontOfTarget() + offsetDistance);
        }

        public override Vector3 GetManipulableIntersectionPoint()
        {
            return _laserHit.collider != null ? _laserHit.point : GrabPosition;
        }

        public override Vector3 GetManipulableIntersectionDirection()
        {
            return _laserHit.collider != null ? _laserHit.normal : transform.forward;
        }

        private void HandleARScaleChanged(float newScale, float oldScale)
        {
            _laser.widthMultiplier = _initialLaserWidth / newScale;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && enabled && _laser != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                for (int p = 1; p < _laser.positionCount - 1; p++)
                {
                    Gizmos.DrawSphere(_laser.GetPosition(p), 0.02f);
                }
            }
        }
#endif
    }

}
