/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// When attached to an object, exposes Manipulable events as Unity events (otherwise,
    /// Manipulables only expose Manipulator interactions as regular C# events)
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Manipulator Unity Event Forwarder", -9750)]
    [RequireComponent(typeof(Manipulable))]
    public class ManipulatorUnityEventForwarder : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Occurs when a Manipulator start hovering (highlighting) this object")]
        private ManipulationUnityEvent OnManipulatorEnter = new ManipulationUnityEvent();

        [SerializeField]
        [Tooltip("Occurs once per frame for each Manipulator that is hovering this object")]
        private ManipulationUnityEvent OnManipulatorStay = new ManipulationUnityEvent();

        [SerializeField]
        [Tooltip("Occurs when a Manipulator is no longer hovering this object, and is also no " +
                 "longer grabbing this object")]
        private ManipulationUnityEvent OnManipulatorExit = new ManipulationUnityEvent();


        [SerializeField]
        [Tooltip("Occurs when a Manipulator is hovering this object and that Manipulator's " +
                 "'grab' action is triggered (e.g. for a Pointer, when Litho.OnTouchDown occurs)")]
        private ManipulationUnityEvent OnManipulatorGrab = new ManipulationUnityEvent();

        [SerializeField]
        [Tooltip("Occurs once per frame between OnManipulatorGrab and OnManipulatorRelease, for " +
                 "each Manipulator that is interacting with this object")]
        private ManipulationUnityEvent OnManipulatorHold = new ManipulationUnityEvent();

        [SerializeField]
        [Tooltip("Occurs, if this object is being interacted with by a Manipulator, when that " +
                 "Manipulator's 'release' action is triggered (e.g. for a Pointer, when " +
                 "Litho.OnTouchUp occurs)")]
        private ManipulationUnityEvent OnManipulatorRelease = new ManipulationUnityEvent();


        [SerializeField]
        [Tooltip("Occurs when a Manipulator is hovering this object and that Manipulator's " +
                 "'tap' action is triggered (when 'release' is triggered a fraction of a second " +
                 "after 'grab')")]
        private ManipulationUnityEvent OnManipulatorTap = new ManipulationUnityEvent();

        [SerializeField]
        [Tooltip("Occurs when a Manipulator is grabbing this object and that Manipulator's " +
                 "'long hold' action is triggered (when 'hold' is triggered and it has been " +
                 "more than a fraction of a second since 'grab' was triggered)")]
        private ManipulationUnityEvent OnManipulatorLongHold = new ManipulationUnityEvent();

        private Manipulable _manipulable;


        protected void Awake()
        {
            _manipulable = GetComponent<Manipulable>();

            _manipulable.OnManipulatorEnter += HandleManipulatorEnter;
            _manipulable.OnManipulatorExit += HandleManipulatorExit;
            _manipulable.OnManipulatorGrab += HandleManipulatorGrab;
            _manipulable.OnManipulatorHold += HandleManipulatorHold;
            _manipulable.OnManipulatorRelease += HandleManipulatorRelease;
            _manipulable.OnManipulatorTap += HandleManipulatorTap;
            _manipulable.OnManipulatorLongHold += HandleManipulatorLongHold;
        }

        private void OnDestroy()
        {
            _manipulable.OnManipulatorEnter -= HandleManipulatorEnter;
            _manipulable.OnManipulatorExit -= HandleManipulatorExit;
            _manipulable.OnManipulatorGrab -= HandleManipulatorGrab;
            _manipulable.OnManipulatorHold -= HandleManipulatorHold;
            _manipulable.OnManipulatorRelease -= HandleManipulatorRelease;
            _manipulable.OnManipulatorTap -= HandleManipulatorTap;
            _manipulable.OnManipulatorLongHold -= HandleManipulatorLongHold;
        }

        private void HandleManipulatorEnter(Manipulation selection)
        {
            OnManipulatorEnter?.Invoke(selection);
        }

        private void HandleManipulatorExit(Manipulation selection)
        {
            OnManipulatorExit?.Invoke(selection);
        }

        private void HandleManipulatorGrab(Manipulation selection)
        {
            OnManipulatorGrab?.Invoke(selection);
        }

        private void HandleManipulatorHold(Manipulation selection)
        {
            OnManipulatorHold?.Invoke(selection);
        }

        private void HandleManipulatorRelease(Manipulation selection)
        {
            OnManipulatorRelease?.Invoke(selection);
        }

        private void HandleManipulatorTap(Manipulation selection)
        {
            OnManipulatorTap?.Invoke(selection);
        }

        private void HandleManipulatorLongHold(Manipulation selection)
        {
            OnManipulatorLongHold?.Invoke(selection);
        }
    }

}
