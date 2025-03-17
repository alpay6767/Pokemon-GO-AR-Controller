/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Responds to hovering and grabbing of any Manipulables attached to this object or any of its
    /// children
    /// </summary>
    public abstract class ManipulationIndicator : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            Manipulable[] manipulables = GetComponentsInChildren<Manipulable>();
            if (manipulables.Length > 0)
            {
                foreach (Manipulable manipulable in manipulables)
                {
                    manipulable.OnManipulatorEnter += HandleManipulatorEnter;
                    manipulable.OnManipulatorExit += HandleManipulatorExit;
                    manipulable.OnManipulatorGrab += HandleManipulatorGrab;
                    manipulable.OnManipulatorRelease += HandleManipulatorRelease;
                }
            }
            else
            {
                Debug.LogError("There are no Manipulables attached to " + this + ", so it cannot" +
                               "indicate the status of Manipulations. Attach a Manipulable to " +
                               this + " or to one of its children, or delete this " +
                               "ManipulationIndicator component to fix this issue.");
            }
        }

        protected virtual void OnDisable()
        {
            foreach (Manipulable manipulable in GetComponentsInChildren<Manipulable>())
            {
                manipulable.OnManipulatorEnter -= HandleManipulatorEnter;
                manipulable.OnManipulatorExit -= HandleManipulatorExit;
                manipulable.OnManipulatorGrab -= HandleManipulatorGrab;
                manipulable.OnManipulatorRelease -= HandleManipulatorRelease;
            }
            ProcessManipulatorRelease(null);
            ProcessManipulatorExit(null);
        }

        private void HandleManipulatorEnter(Manipulation manipulation)
        {
            if (manipulation.Manipulator.ShowManipulationIndicator)
            {
                ProcessManipulatorEnter(manipulation);
            }
        }

        private void HandleManipulatorExit(Manipulation manipulation)
        {
            if (manipulation.Manipulator.ShowManipulationIndicator)
            {
                ProcessManipulatorExit(manipulation);
            }
        }

        private void HandleManipulatorGrab(Manipulation manipulation)
        {
            if (manipulation.Manipulator.ShowManipulationIndicator)
            {
                ProcessManipulatorGrab(manipulation);
            }
        }

        private void HandleManipulatorRelease(Manipulation manipulation)
        {
            if (manipulation.Manipulator.ShowManipulationIndicator)
            {
                ProcessManipulatorRelease(manipulation);
            }
        }

        protected virtual void ProcessManipulatorEnter(Manipulation manipulation) { }

        protected virtual void ProcessManipulatorExit(Manipulation manipulation) { }

        protected virtual void ProcessManipulatorGrab(Manipulation manipulation) { }

        protected virtual void ProcessManipulatorRelease(Manipulation manipulation) { }

    }

}
