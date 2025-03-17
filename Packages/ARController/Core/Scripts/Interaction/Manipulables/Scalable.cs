/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Handles manipulation of the local scale of the target object whilst it is being interacted
    /// with by a Manipulator
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Scalable", -9776)]
    public class Scalable : SliderManipulable
    {
        [SerializeField]
        [Tooltip("Smallest scale this object may have compared to its initial scale")]
        private float _minScaleFactor = 0.6f;

        [SerializeField]
        [Tooltip("Largest scale this object may have compared to its initial scale")]
        private float _maxScaleFactor = 2f;

        protected override float Value
        {
            get
            {
                return Target.localScale.x / _baseScale.x;
            }
        }

        protected override float MinValue => _minScaleFactor;

        protected override float MaxValue => _maxScaleFactor;

        private Vector3 _baseScale;


        protected override void Awake()
        {
            base.Awake();

            _baseScale = Target.localScale;
            if (_baseScale.x < Litho.EPSILON)
            {
                _baseScale.x = Litho.EPSILON;
            }

            if (_minScaleFactor <= Litho.EPSILON)
            {
                _minScaleFactor = Litho.EPSILON;
            }
            if (_maxScaleFactor < _minScaleFactor)
            {
                _maxScaleFactor = _minScaleFactor;
                Debug.LogWarning("Maximum relative scale is smaller than or equal to minimum" +
                                 "relative scale; scaling will not be performed on " + this + ".");
            }
        }

        protected override void UpdateValue(float targetValue)
        {
            Target.localScale = _baseScale * targetValue;
        }
    }

}
