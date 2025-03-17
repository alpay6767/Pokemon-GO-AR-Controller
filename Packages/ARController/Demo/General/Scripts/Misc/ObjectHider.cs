/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.Events;

namespace LITHO
{

    /// <summary>
    /// Handles showing/ hiding of this object when the Show/ Hide methods are called
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Object Hider", -9078)]
    public class ObjectHider : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether to start in the hidden state")]
        protected bool _startHidden = true;

        [SerializeField]
        [Tooltip("Time period over which to scale between hidden and shown states")]
        protected float _animationPeriod = 0.25f;

        protected Vector3 _originalScale;

        protected bool _isHiding = false;
        protected bool _isAnimating = false;
        protected float _hiddenness;

        [SerializeField]
        [Tooltip("Occurs when the 'hide' process is started")]
        public UnityEvent OnStartHiding = new UnityEvent();

        [SerializeField]
        [Tooltip("Occurs once the 'hide' process is complete")]
        public UnityEvent OnHidden = new UnityEvent();

        [SerializeField]
        [Tooltip("Occurs once the 'show' process is started")]
        public UnityEvent OnStartShowing = new UnityEvent();

        [SerializeField]
        [Tooltip("Occurs once the 'show' process is complete")]
        public UnityEvent OnShown = new UnityEvent();


        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void Start()
        {
            if (_startHidden)
            {
                Hide(true);
            }
        }

        private void Update()
        {
            if (_isAnimating && _animationPeriod > 0f)
            {
                Animate();
            }
        }

        public virtual void Show(bool immediate = false)
        {
            _isAnimating = true;
            _isHiding = false;
            HandleStartShowing();
            if (immediate)
            {
                _hiddenness = 0f;
                transform.localScale = _originalScale;
                HandleFinishShowing();
            }
        }

        public virtual void Hide(bool immediate = false)
        {
            _isAnimating = true;
            _isHiding = true;
            HandleStartHiding();
            if (immediate)
            {
                _hiddenness = 1f;
                transform.localScale = Vector3.zero;
                HandleFinishHiding();
            }
        }

        public void Toggle(bool immediate = false)
        {
            if (_isHiding)
            {
                Show(immediate);
            }
            else
            {
                Hide(immediate);
            }
        }

        protected void HandleStartHiding()
        {
            // Disable any Manipulators contained by the target object
            foreach (Manipulator manipulator in GetComponentsInChildren<Manipulator>())
            {
                manipulator.enabled = false;
            }
            OnStartHiding?.Invoke();
        }

        protected void HandleFinishHiding()
        {
            OnHidden?.Invoke();
        }

        protected void HandleStartShowing()
        {
            OnStartShowing?.Invoke();
        }

        protected void HandleFinishShowing()
        {
            // Re-enable any Manipulators contained by the target object
            foreach (Manipulator manipulator in GetComponentsInChildren<Manipulator>())
            {
                manipulator.enabled = true;
            }
            OnShown?.Invoke();
        }

        private void Animate()
        {
            if (_isHiding && _hiddenness < 1f)
            {
                _hiddenness += Time.deltaTime / _animationPeriod;
                if (_hiddenness > 1f)
                {
                    _hiddenness = 1f;
                    _isAnimating = false;
                    HandleFinishHiding();
                }
            }
            else if (_hiddenness > 0f)
            {
                _hiddenness -= Time.deltaTime / _animationPeriod;
                if (_hiddenness < 0f)
                {
                    _hiddenness = 0f;
                    _isAnimating = false;
                    HandleFinishShowing();
                }
            }
            transform.localScale = _originalScale * (1f - _hiddenness);
        }
    }

}
