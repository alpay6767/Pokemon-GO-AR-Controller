/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

namespace LITHO
{

    /// <summary>
    /// Is generated on a Litho Manipulable to represent it being hovered or grabbed by a
    /// Manipulator; handles transfer of information betweeb the Manipulable and the
    /// Manipulator, such as where the Manipulator is targeting whilst the interaction is ongoing;
    /// handles positioning of Manipulator-specific manipulation indicators
    /// </summary>
    public class Manipulation : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private Manipulator _manipulator;
        public Manipulator Manipulator
        {
            get { return _manipulator; }
            private set { _manipulator = value; }
        }

        [SerializeField, HideInInspector]
        private Manipulable _manipulable;
        public Manipulable Manipulable
        {
            get { return _manipulable; }
            private set { _manipulable = value; }
        }

        public float HoverStartTime { get; private set; }
        public float GrabStartTime { get; private set; }


        public Vector3 InitialLocalGrabPosition { get; private set; }
        public Vector3 InitialGrabPosition
            => Manipulable.ConvertToWorldSpace(InitialLocalGrabPosition);
        public Vector3 InitialManipulablePosition { get; private set; } = Vector3.zero;
        public Quaternion InitialManipulableRotation { get; private set; }

        protected Vector3 _localGrabPosition;
        public Vector3 GrabPosition
        {
            get
            {
                return Manipulable.ConvertToWorldSpace(_localGrabPosition);
            }
            set
            {
                _localGrabPosition = Manipulable.ConvertToLocalSpace(value);
            }
        }

        [SerializeField, HideInInspector]
        private GameObject _indicator;
        public GameObject Indicator { get { return _indicator; } set { _indicator = value; } }
        private Vector3 _indicatorBaseScale = Vector3.one;

        public bool IsGrabbed { get; private set; }
        public bool EndedByExit { get; set; }


        private void LateUpdate()
        {
            if (_indicator != null)
            {
                UpdateIndicatorOnManipulator(_indicator.transform);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_indicator != null)
            {
                Destroy(_indicator);
            }
            if (Manipulable != null)
            {
                Manipulable.OnManipulatorGrab -= HandleManipulatorGrab;
                Manipulable.OnManipulatorRelease -= HandleManipulatorRelease;
                Manipulable.OnManipulatorTap -= HandleManipulatorTap;
            }
        }

        protected virtual void HandleManipulatorGrab(Manipulation manipulation)
        {
            // Only continue if this event was triggered by our Manipulator
            if (manipulation.Manipulator != Manipulator)
            {
                return;
            }

            IsGrabbed = true;

            GrabStartTime = Time.time;
            GrabPosition = Manipulator.GrabPosition;
            InitialLocalGrabPosition = _localGrabPosition;
            InitialManipulablePosition = Manipulable.Target.transform.position;
            InitialManipulableRotation = Manipulable.Target.transform.rotation;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        protected virtual void HandleManipulatorRelease(Manipulation manipulation)
        {
            // Only continue if this event was triggered by our Manipulator
            if (manipulation.Manipulator != Manipulator)
            {
                return;
            }
            IsGrabbed = false;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        protected virtual void HandleManipulatorTap(Manipulation manipulation)
        {
            // Only continue if this event was triggered by our Manipulator
            if (manipulation.Manipulator != Manipulator)
            {
                return;
            }

            // Check for any other Manipulables on the closest Manipulable ancestor
            Manipulable[] manipulables = GetComponents<Manipulable>();
            if (manipulables.Length > 1)
            {
                for (int m = 0; m < manipulables.Length; m++)
                {
                    if (manipulables[m] == Manipulable)
                    {
                        Manipulator.HoverNewManipulable(
                            manipulables[(m + 1) % manipulables.Length]);
                    }
                }
            }
        }

        public virtual void Initialize(Manipulable manipulable,
                                       Manipulator manipulator,
                                       GameObject indicatorPrefab)
        {
            Manipulable = manipulable;
            Manipulator = manipulator;

            if (Manipulable != null)
            {
                Manipulable.OnManipulatorGrab += HandleManipulatorGrab;
                Manipulable.OnManipulatorRelease += HandleManipulatorRelease;
                Manipulable.OnManipulatorTap += HandleManipulatorTap;
            }
            else
            {
                Debug.LogWarning("Manipulation cannot be initialized with a null Manipulable",
                                 this);
            }

            if (indicatorPrefab != null && manipulator.ShowManipulationIndicator)
            {
                _indicator = Instantiate(indicatorPrefab);
                _indicator.name = _indicator.name.Replace("Clone", name);
                _indicatorBaseScale = _indicator.transform.localScale;
            }
        }

        public void UpdateIndicatorOnManipulator(Transform indicator)
        {
            if (indicator != null && indicator.gameObject.activeSelf)
            {
                indicator.SetPositionAndRotation(Manipulator.GetPointInFrontOfTarget(),
                                                 Manipulator.GetIconRotation());
                indicator.localScale = Vector3.Scale(_indicatorBaseScale,
                                                     Manipulator.transform.lossyScale);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Manipulation))]
    [CanEditMultipleObjects]
    public class ManipulationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_manipulator"),
                                          new GUIContent("Manipulator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_manipulable"),
                                          new GUIContent("Manipulable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_indicator"),
                                          new GUIContent("Indicator"));

            Manipulation manipulation = (Manipulation)target;
            EditorGUILayout.Toggle("Grabbing", manipulation.IsGrabbed);
            GUI.enabled = true;
        }
    }
#endif

}
