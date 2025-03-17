/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using System;
using UnityEngine;

namespace LITHO
{
    /// <summary>
    /// Implements a LaserManipulator such that it can be intuitively controlled by a Litho device
    /// </summary>
    [AddComponentMenu("LITHO/Manipulators/Pointer", -9900)]
    public class Pointer : LaserManipulator
    {
        [Header("Pointer Properties")]

        [Tooltip("Map from twist to depth factor for the point grip")]
        private AnimationCurve _mapTwistToDepthFactor = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(MIN_RELATIVE_TWIST, 0f, 0f, 0f, 1f, 0.2f),
            new Keyframe(0f, 1f, 0f, 0f, 0.3f, 0.3f),
            new Keyframe(MAX_RELATIVE_TWIST, DEFAULT_MAX_DEPTH_FACTOR, 0f, 0f, 0.4f, 1f)
        });

        [Tooltip("Map from twist to yaw offset for the point grip")]
        private AnimationCurve _mapTouchPosToYaw = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, -180f, 0f, 0f, 1f, 0.2f),
            new Keyframe(0.5f, 1f, 0f, 0f, 0.2f, 0.2f),
            new Keyframe(1f, 180f, 0f, 0f, 0.4f, 1f)
        });

        [Tooltip("Map from touch pos to depth factor for the clutch grip")]
        private AnimationCurve _mapTouchPosToDepthFactor = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 0f, 0f, 0f, 1f, 0.2f),
            new Keyframe(0.5f, 1f, 0f, 0f, 0.3f, 0.3f),
            new Keyframe(1f, DEFAULT_MAX_DEPTH_FACTOR, 0f, 0f, 0.4f, 1f)
        });

        [Tooltip("Map from twist to yaw offset for the clutch grip")]
        private AnimationCurve _mapTwistToRoll = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(MIN_RELATIVE_TWIST, -60f, 0f, 0f, 0f, 0.25f),
            new Keyframe(MAX_RELATIVE_TWIST, 60f, 0f, 0f, 0.25f, 0f)
        });

        [SerializeField]
        [Tooltip("How fast manipulation depth should increase when holding an object at maximum " +
                 "depth")]
        private float _depthFactorExtendRate = 0.5f;


        protected override float DepthFactor
        {
            get
            {
                float depthFactor = 1f;
                if (IsInteracting && CurrentManipulable.SupportsDepthControl)
                {
                    if (Litho.GripType == GripType.Point)
                    {
                        depthFactor = _mapTwistToDepthFactor.Evaluate(GetRelativeTwist());
                        if (depthFactor >= _mapTwistToDepthFactor.keys[2].value * 0.99f)
                        {
                            ExtendDepthFactorExtent();
                        }
                    }
                    else if (Litho.GripType == GripType.Clutch)
                    {
                        float touchPos = LastTouchPosition.x;
                        depthFactor = _mapTouchPosToDepthFactor.Evaluate(touchPos);
                        if (depthFactor >= _mapTouchPosToDepthFactor.keys[2].value * 0.99f)
                        {
                            ExtendDepthFactorExtent();
                        }
                    }
                }
                return depthFactor;
            }
        }

        protected override Quaternion OffsetRotation
        {
            get
            {
                if (Litho.GripType == GripType.Point)
                {
                    return Quaternion.AngleAxis(
                        _mapTouchPosToYaw.Evaluate(CurrentTouchPosition.x)
                        - _mapTouchPosToYaw.Evaluate(InitialTouchPosition.x),
                        Vector3.up);
                }
                else
                {
                    return Quaternion.AngleAxis(90f * Mathf.Round(
                        (_mapTwistToRoll.Evaluate(GetRelativeTwist())
                         - _mapTwistToRoll.Evaluate(_initialTwist)) / 90f), Vector3.forward)
                        * Quaternion.AngleAxis(_cumulativeYaw, Vector3.up);
                }
            }
        }

        private float _initialTwist;
        public Vector2 InitialTouchPosition { get; private set; } = Vector2.zero;
        public Vector2 CurrentTouchPosition { get; private set; } = Vector2.zero;
        public Vector2 LastTouchPosition { get; private set; } = Vector2.zero;

        private float _cumulativeYaw;

        private const float MIN_RELATIVE_TWIST = -60f;
        private const float MAX_RELATIVE_TWIST = 60f;
        private const float DEFAULT_MAX_DEPTH_FACTOR = 3f;
        private const float TOUCH_SEPARATION_THRESHOLD = 0.2f;


        protected override void Awake()
        {
            base.Awake();

            // Listen for Litho touch events
            Litho.OnTouchStart += HandleTouchStart;
            Litho.OnTouchHold += HandleTouchHold;
            Litho.OnTouchEnd += HandleTouchEnd;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unsubscribe from Litho touch events
            Litho.OnTouchStart -= HandleTouchStart;
            Litho.OnTouchHold -= HandleTouchHold;
            Litho.OnTouchEnd -= HandleTouchEnd;
        }

        public override void Grab()
        {
            base.Grab();

            ResetDepthCurves();

            _initialTwist = GetRelativeTwist(true);
            _cumulativeYaw = 0f;

            RemapTouchToDepth();
        }

        protected override void HandleEnterManipulable()
        {
            base.HandleEnterManipulable();

            Litho.PlayHapticEffect(HapticEffect.Type.SharpTick_3_60);
        }

        protected override void HandleExitManipulable()
        {
            base.HandleExitManipulable();

            Litho.PlayHapticEffect(HapticEffect.Type.SoftBump_30);
        }

        private void HandleTouchStart(Vector2 position, Vector2 worldPosition)
        {
            InitialTouchPosition = position;
            CurrentTouchPosition = position;
            LastTouchPosition = position;
            Grab();
        }

        private void HandleTouchHold(Vector2 position, Vector2 worldPosition)
        {
            LastTouchPosition = CurrentTouchPosition;
            CurrentTouchPosition = position;
            if (Litho.GripType == GripType.Clutch)
            {
                _cumulativeYaw += (_mapTouchPosToYaw.Evaluate(CurrentTouchPosition.y)
                                   - _mapTouchPosToYaw.Evaluate(LastTouchPosition.y))
                    * (Mathf.Abs(CurrentTouchPosition.x - LastTouchPosition.x)
                       / Time.deltaTime > TOUCH_SEPARATION_THRESHOLD ? 0f : 1f);
            }
        }

        private void HandleTouchEnd(Vector2 position, Vector2 worldPosition)
        {
            LastTouchPosition = position;
            CurrentTouchPosition = position;
            Release();
            ResetDepthCurves();
        }

        private void HandleTap(Vector2 position, Vector2 worldPosition)
        {
            LastTouchPosition = position;
            CurrentTouchPosition = position;
            Tap();
        }

        private void HandleLongHold(Vector2 position, Vector2 worldPosition)
        {
            LongHold();
        }

        private float GetRelativeTwist(bool resetReference = false)
        {
            if (resetReference)
            {
                _initialTwist = 0f;
            }
            float twist = Litho.Rotation.eulerAngles.z - _initialTwist;
            while (twist > 180f)
            {
                twist -= 360f;
            }
            while (twist <= -180f)
            {
                twist += 360f;
            }
            return twist;
        }

        private void RemapTouchToDepth()
        {
            // Remap the touch-to-depth curve to centre around the start position
            // Move endpoints out of bounds if their defaults would create steep
            // changes in depth
            Keyframe[] keyFrames = _mapTouchPosToDepthFactor.keys;
            keyFrames[0].time = Mathf.Min(0f, InitialTouchPosition.x - 0.35f);
            keyFrames[1].time = InitialTouchPosition.x;
            keyFrames[2].time = Mathf.Max(1f, InitialTouchPosition.x + 0.35f);
            _mapTouchPosToDepthFactor.keys = keyFrames;
        }

        private void ResetDepthCurves()
        {
            float maxDepthFactor = DEFAULT_MAX_DEPTH_FACTOR;
            if (InitialManipulableDepth > 0f)
            {
                maxDepthFactor = Mathf.Max(maxDepthFactor,
                                           transform.lossyScale.y * DEFAULT_MAX_DEPTH_FACTOR
                                           / InitialManipulableDepth);
            }
            // Reset the curve for twist to depth factor mapping
            Keyframe[] keyFrames = _mapTwistToDepthFactor.keys;
            if (Litho.Handedness == Handedness.Right)
            {
                keyFrames[0].time = MIN_RELATIVE_TWIST;
                keyFrames[0].value = 0f;
                keyFrames[2].time = MAX_RELATIVE_TWIST;
                keyFrames[2].value = maxDepthFactor;
            }
            else
            {
                keyFrames[0].time = -MAX_RELATIVE_TWIST;
                keyFrames[0].value = maxDepthFactor;
                keyFrames[2].time = -MIN_RELATIVE_TWIST;
                keyFrames[2].value = 0f;
            }
            // Flip the rotation limits if using in left-handed mode
            // Pass the modified keyframes back to the curve
            _mapTwistToDepthFactor.keys = keyFrames;

            // Reset the curve for the touch to depth mapping
            keyFrames = _mapTouchPosToDepthFactor.keys;
            keyFrames[2].value = maxDepthFactor;
            _mapTouchPosToDepthFactor.keys = keyFrames;
        }

        private void ExtendDepthFactorExtent()
        {
            // Gradually move the end of the manipulation range further away
            // Do this for both the twist-depth mapping
            Keyframe[] keyFrames = _mapTwistToDepthFactor.keys;
            int index = (Litho.Handedness == Handedness.Right) ? 2 : 0;
            keyFrames[index].value
                += _depthFactorExtendRate * Time.deltaTime;
            _mapTwistToDepthFactor.keys = keyFrames;
            // And for the touch-depth mapping
            keyFrames = _mapTouchPosToDepthFactor.keys;
            keyFrames[2].value += _depthFactorExtendRate * Time.deltaTime;
            _mapTouchPosToDepthFactor.keys = keyFrames;
        }
    }
}
