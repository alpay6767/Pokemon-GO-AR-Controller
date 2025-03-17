/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO.Demo
{

    /// <summary>
    /// Sets up a Line Renderer to point at the given target position
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Label : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Transform at which the label should point")]
        private Transform _target = null;

        private TextMesh _text, _outlineText;

        private Vector3 _lastTargetPosition, _lastTextPosition;
        private Vector3 _lastTextRotation;


        private void Awake()
        {
            if (_target == null)
            {
                _target = transform;
            }
            _text = GetComponentInChildren<TextMesh>();
            if (_text != null)
            {
                foreach (TextMesh textMesh in _text.GetComponentsInChildren<TextMesh>())
                {
                    if (textMesh != _text)
                    {
                        _outlineText = textMesh;
                    }
                }
            }
            SetLine();
        }

        private void Update()
        {
            if (_text != null
                && (Vector3.Distance(_lastTextPosition, _text.transform.position) > Litho.EPSILON
                    || Vector3.Distance(_lastTargetPosition, _target.position) > Litho.EPSILON
                    || Vector3.Distance(_lastTextRotation, _text.transform.eulerAngles)
                        > Litho.EPSILON))
            {
                SetLine();

                if (_text != null && _outlineText != null
                   && _text.text != _outlineText.text)
                {
                    _outlineText.text = _text.text;
                }
            }
        }

        private void SetLine()
        {
            LineRenderer lr = GetComponent<LineRenderer>();
            if (lr != null && _text != null)
            {
                BoxCollider textBox = _text.gameObject.AddComponent<BoxCollider>();
                Vector3 sourcePosition = textBox.ClosestPoint(_target.position);
                Destroy(textBox);

                if (Vector3.Distance(sourcePosition, _target.position) > Litho.EPSILON)
                {
                    lr.useWorldSpace = true;
                    Vector3 displacement = _target.position - sourcePosition;
                    // Set up the curve to taper out and in at the start and end
                    float taperDistance = Mathf.Min(
                        0.5f, 0.1f * lr.widthMultiplier / displacement.magnitude);
                    lr.positionCount = 5;
                    lr.SetPosition(0, sourcePosition);
                    lr.SetPosition(1, sourcePosition + displacement * (taperDistance - 0.0001f));
                    lr.SetPosition(2, sourcePosition + displacement * (taperDistance + 0.0001f));
                    lr.SetPosition(3, _target.position - displacement * taperDistance);
                    lr.SetPosition(4, _target.position);
                    lr.widthCurve = new AnimationCurve(new Keyframe[] {
                        new Keyframe(0f, 1f, 0f, 0f, 0f, 0f),
                        new Keyframe(taperDistance - 0.0001f, 1f, 0f, 0f, 0f, 0f),
                        new Keyframe(taperDistance + 0.0001f, 0.2f, 0f, 0f, 0f, 0f),
                        new Keyframe(1f - taperDistance * 0.5f, 0.2f, 0f, 0f, 0f, 0f),
                        new Keyframe(1f, 0f, 0f, 0f, 0f, 0f)
                    });
                }
                _lastTargetPosition = _target.position;
                _lastTextPosition = _text.transform.position;
                _lastTextRotation = _text.transform.eulerAngles;
            }
        }

        public void SetText(string newText)
        {
            if (_text != null && _outlineText != null)
            {
                _text.text = newText;
                _outlineText.text = newText;
            }
        }
    }

}
