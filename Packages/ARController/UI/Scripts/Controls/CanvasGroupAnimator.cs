/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2019 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.UI
{

    /// <summary>
    /// Animates the alpha value of the attached CanvasGroup
    /// </summary>
    [AddComponentMenu("LITHO/UI/Canvas Group Animator")]
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupAnimator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The alpha value to target")]
        private float _targetAlpha;

        [SerializeField]
        [Tooltip("The exponential rate at which to animate towards the target alpha")]
        private float _animateRate = 0.1f;

        public float TargetAlpha { get { return _targetAlpha; } set { _targetAlpha = value; } }

        private CanvasGroup _canvasGroup;


        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            _canvasGroup.alpha
                = _canvasGroup.alpha * (1f - _animateRate) + TargetAlpha * _animateRate;
        }
    }

}
