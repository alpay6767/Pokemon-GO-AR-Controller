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
    /// When attached to an object, represents that this object may be selected by a Manipulator
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Selectable", -9774)]
    public class Selectable : Grabbable
    {
        private enum SelectionMode
        {
            HoldToSelect = 0,
            TapToToggle
        }

        [SerializeField]
        [Tooltip("Whether to only select this object whilst it is grabbed, or to require a " +
                 "second grab event to deselect it")]
        private SelectionMode _selectionMode = SelectionMode.TapToToggle;

        [Tooltip("Occurs when this object gets selected")]
        public ManipulationUnityEvent OnSelected = new ManipulationUnityEvent();

        [Tooltip("Occurs when this object gets deselected")]
        public ManipulationUnityEvent OnDeselected = new ManipulationUnityEvent();

        [Tooltip("Occurs when the selection state of this object changes")]
        public ManipulationUnityEvent OnSelectionStateChanged = new ManipulationUnityEvent();

        public bool IsSelected { get; private set; }


        protected override void Awake()
        {
            base.Awake();

            OnManipulatorNoExitRelease.AddListener(HandleManipulatorNoExitRelease);
        }

        protected override void OnDestroy()
        {
            OnManipulatorNoExitRelease.RemoveListener(HandleManipulatorNoExitRelease);
        }

        public override void InitializeManipulation(Manipulation manipulation)
        {
            base.InitializeManipulation(manipulation);

            if (_selectionMode == SelectionMode.HoldToSelect)
            {
                SetSelected(true, manipulation);
            }
        }

        public override void FinalizeManipulation(Manipulation manipulation)
        {
            base.FinalizeManipulation(manipulation);

            if (_selectionMode == SelectionMode.HoldToSelect)
            {
                SetSelected(false, manipulation);
            }
        }

        private void HandleManipulatorNoExitRelease(Manipulation manipulation)
        {
            if (_selectionMode == SelectionMode.TapToToggle)
            {
                SetSelected(!IsSelected, manipulation);
            }
        }

        public void SetSelected(bool shouldBeSelected, Manipulation manipulation)
        {
            bool wasSelected = IsSelected;
            IsSelected = shouldBeSelected;
            if (IsSelected != wasSelected)
            {
                OnSelectionStateChanged.Invoke(manipulation);
                if (IsSelected)
                {
                    OnSelected.Invoke(manipulation);
                }
                else
                {
                    OnDeselected.Invoke(manipulation);
                }
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Implements a custom editor that shows whether a Manipulable is hovered and/ or activated,
    /// using a label in the Scene view and Inspector window
    /// </summary>
    [CustomEditor(typeof(Selectable))]
    [CanEditMultipleObjects]
    public class SelectableTargetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (((Selectable)serializedObject.targetObject).IsSelected)
            {
                EditorGUILayout.LabelField("Selected");
            }
            else
            {
                EditorGUILayout.LabelField("Not Selected");
            }
        }
    }
#endif

}
