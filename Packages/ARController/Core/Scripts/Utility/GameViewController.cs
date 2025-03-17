/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

namespace LITHO.Utility
{

    /// <summary>
    /// Uses screen touches in the Unity Editor to simulate camera motion and Litho interaction
    /// </summary>
    [AddComponentMenu("LITHO/Utilities/Game View Controller", -9200)]
    [RequireComponent(typeof(Litho))]
    [RequireComponent(typeof(Pointer))]
    public class GameViewController : MonoBehaviour
    {
#if UNITY_EDITOR

        [SerializeField]
        [Tooltip("Whether the controller is locked in position ('L' key to toggle)")]
        private bool _locked;

        [SerializeField]
        [Tooltip("Maximum movement speed of the camera (metres per second)")]
        private float _maxCameraSpeed = 1.5f;

        [SerializeField]
        [Tooltip("Sensitivity of camera rotation to mouse movement (degrees per pixel)")]
        private float _cameraRotationSpeed = 0.05f;

        [SerializeField]
        [Tooltip("Sensitivity of touch position to WASD key presses (units per second held)")]
        private float _touchMoveSensitivity = 0.5f;

        [SerializeField]
        [Tooltip("Sensitivity of Litho twist (z-rotation) to scrolling (degrees per unit")]
        private float _twistScrollSensitivity = 1f;

        [SerializeField]
        private bool _invertY = false;

        private bool _leftMouseIsDown;
        private bool _rightMouseIsDown;
        private Vector3 _lastMousePosition;

        private Vector3 _cameraVelocity;

        private float _boostFactor;

        private Vector2 _touchPosition;

        private float _scrollAngle;

        private Litho litho;


        private readonly Vector2 TOUCH_START_POSITION = Vector2.one * 0.5f;

        private const float MAX_BOOST_FACTOR = 4f;


        private void Awake()
        {
            litho = FindObjectOfType<Litho>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                _locked = !_locked;
            }

            if (!Litho.IsConnected && !_locked)
            {

                Ray screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Physics.Raycast(screenRay, out hit, LaserManipulator.MAX_RAYCAST_DISTANCE,
                                ~(1 << LayerMask.NameToLayer("UI")));

                _scrollAngle
                    = (_scrollAngle + _twistScrollSensitivity * Input.mouseScrollDelta.y) % 360f;

                // If the left mouse button was just pressed
                if (Input.GetMouseButtonDown(0))
                {
                    // Record the mouse state to support locking controller state
                    _leftMouseIsDown = true;
                    // Reset the touch position
                    _touchPosition = TOUCH_START_POSITION;
                    // Fake a touch down event
                    if (litho != null)
                    {
                        litho.InvokeTouchStart(_touchPosition, _touchPosition);
                    }
                }
                // If the left mouse button was released
                else if (Input.GetMouseButtonUp(0))
                {
                    // Record the mouse state to support locking controller state
                    _leftMouseIsDown = false;
                    // Fake a touch up event
                    if (litho != null)
                    {
                        litho.InvokeTouchEnd(_touchPosition, _touchPosition);
                    }
                }

                bool hasAimed = false;
                // If the left mouse button is being held
                if (_leftMouseIsDown)
                {
                    // If not moving the camera
                    if (!Input.GetMouseButton(1))
                    {
                        // Update the touch position
                        _touchPosition += (
                            (Input.GetKey(KeyCode.W) ? Vector2.up : Vector2.zero) +
                            (Input.GetKey(KeyCode.S) ? Vector2.down : Vector2.zero) +
                            (Input.GetKey(KeyCode.A) ? Vector2.left : Vector2.zero) +
                            (Input.GetKey(KeyCode.D) ? Vector2.right : Vector2.zero)
                        ) * _touchMoveSensitivity * _boostFactor * Time.unscaledDeltaTime;
                        _touchPosition = new Vector2(Mathf.Clamp01(_touchPosition.x),
                                                     Mathf.Clamp01(_touchPosition.y));
                    }
                    Pointer pointer = GetComponent<Pointer>();
                    if (pointer != null && pointer.IsInteracting)
                    {
                        float depth = (pointer.TargetGrabPosition
                                       - Camera.main.transform.position).magnitude;
                        transform.LookAt(screenRay.origin + screenRay.direction * depth,
                                         transform.up);
                        hasAimed = true;
                    }

                    if (litho != null)
                    {
                        litho.InvokeTouchHold(_touchPosition, _touchPosition);
                    }
                    // Draw debug lines
                    Debug.DrawLine(screenRay.origin, hit.point, Color.red, Time.unscaledDeltaTime);
                }

                if (!hasAimed)
                {
                    if (hit.transform != null)
                    {
                        transform.LookAt(hit.point);
                    }
                    else
                    {
                        transform.LookAt(screenRay.origin + screenRay.direction
                            * LaserManipulator.MAX_RAYCAST_DISTANCE, transform.up);
                    }
                }

                transform.eulerAngles = new Vector3(
                    transform.eulerAngles.x,
                    transform.eulerAngles.y,
                    _scrollAngle);
            }
            else if (Litho.IsConnected && Input.GetKeyDown(KeyCode.C))
            {
                Litho.Calibrate();
            }

            _boostFactor = Input.GetKey(KeyCode.LeftShift) ?
                _boostFactor * 0.8f + MAX_BOOST_FACTOR * 0.2f :
                _boostFactor * 0.8f + 1f * 0.2f;

            // Check for attempted camera movement
            if (Input.GetMouseButton(1) && Camera.main != null)
            {
                Vector3 delta = (Input.mousePosition - _lastMousePosition) * _cameraRotationSpeed;

                // Rotate the camera in response to the mouse movement
                Camera.main.transform.eulerAngles = new Vector3(
                    Camera.main.transform.eulerAngles.x + delta.y * (_invertY ? 1f : -1f),
                    Camera.main.transform.eulerAngles.y + delta.x,
                    Camera.main.transform.eulerAngles.z);

                // Check whether the camera should have its velocity altered
                if (Input.GetKey(KeyCode.W))
                {
                    _cameraVelocity.z = _cameraVelocity.z * 0.9f
                        + _maxCameraSpeed * _boostFactor * 0.1f;

                }
                else if (Input.GetKey(KeyCode.S))
                {
                    _cameraVelocity.z = _cameraVelocity.z * 0.9f
                        + -_maxCameraSpeed * _boostFactor * 0.1f;
                }
                else
                {
                    _cameraVelocity.z *= 0.8f;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    _cameraVelocity.x = _cameraVelocity.x * 0.9f
                        + _maxCameraSpeed * _boostFactor * 0.1f;
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    _cameraVelocity.x = _cameraVelocity.x * 0.9f
                        + -_maxCameraSpeed * _boostFactor * 0.1f;
                }
                else
                {
                    _cameraVelocity.x *= 0.8f;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    _cameraVelocity.y = _cameraVelocity.y * 0.9f
                        + _maxCameraSpeed * _boostFactor * 0.1f;
                }
                else if (Input.GetKey(KeyCode.Q))
                {
                    _cameraVelocity.y = _cameraVelocity.y * 0.9f
                        + -_maxCameraSpeed * _boostFactor * 0.1f;
                }
                else
                {
                    _cameraVelocity.y *= 0.8f;
                }
                // Apply camera velocity to camera position
                Camera.main.transform.position +=
                    Camera.main.transform.TransformDirection(_cameraVelocity)
                        * Time.unscaledDeltaTime;
            }
            _lastMousePosition = Input.mousePosition;
        }
#else
        private void Awake()
        {
            Destroy(this);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GameViewController))]
    public class GameViewControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.HelpBox("This component allows you to control your camera and Litho " +
                                    "conveniently in the Game view using your computer mouse; " +
                                    "feel free to delete this GameViewController component if " +
                                    "you do not find these features helpful." +
                                    "\nIn the Game view (whilst in Play mode):" +
                                    "\n- Left-click initially to engage the controller" +
                                    "\n- Point to aim Litho" +
                                    "\n- Left-click to simulate Litho touchpad 'tap' events" +
                                    "\n- Left-click and hold to simulate Litho touchpad 'hold' " +
                                    "and 'long hold' events" +
                                    "\n- Left-click and use W, A, S, D keys to move touch position"
                                    + " (when in Clutch grip, this allows depth manipulation)" +
                                    "\n- Left-click and use scroll wheel to twist Litho"
                                    + " (when in Point grip, this allows depth manipulation)" +
                                    "\n- Right-click and drag to rotate camera " +
                                    "\n- Right-click and use W, A, S, D keys to move camera" +
                                    "\n- Press the L key to toggle locking the controller state",
                                    MessageType.Info);
        }
    }
#endif

}
