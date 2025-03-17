/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.UI
{

    /// <summary>
    /// Handles animation of a value between integer values
    /// </summary>
    public abstract class ValueAnimator : MonoBehaviour
    {
        private float _lastAnimationDelta;

        protected float _maxAnimationSpeed = 100f;
        protected float _softenAnimationFactor = 0.2f;


        protected virtual void Update()
        {
            AnimateValue();
        }

        protected abstract void AnimateValue();

        protected float GetNewValue(float target, float current, ref bool complete)
        {
            if (Application.isPlaying)
            {
                float animationDelta =
                    _maxAnimationSpeed * Time.deltaTime * _softenAnimationFactor
                    + _lastAnimationDelta * (1f - _softenAnimationFactor);
                _lastAnimationDelta = animationDelta;
                if (current > target + Litho.EPSILON)
                {
                    complete = false;
                    return current - Mathf.Min(animationDelta,
                                               (current - target) * _softenAnimationFactor);
                }
                else if (current < target - Litho.EPSILON)
                {
                    complete = false;
                    return current + Mathf.Min(animationDelta,
                                               (target - current) * _softenAnimationFactor);
                }
                else
                {
                    _lastAnimationDelta = 0f;
                    complete = true;
                    return target;
                }
            }
            else
            {
                complete = true;
                return target;
            }
        }
        protected float GetNewValue(float target, float current)
        {
            bool complete = false;
            return GetNewValue(target, current, ref complete);
        }

        protected void ResetAnimation()
        {
            _lastAnimationDelta = 0f;
        }
    }

}
