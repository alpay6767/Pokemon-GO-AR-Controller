using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using PokemonGO.Global;
using Kynesis.Utilities;
using UnityEngine;
using LITHO; // for Litho events

namespace PokemonGO
{
    public class ThrowerBLE : MonoBehaviour
    {
        [Header("Dragging Settings")]
        [SerializeField] private float _followSpeed = 10f;
        [SerializeField] private float _torqueMultiplier = 0.3f;

        [Header("Throw Settings")]
        [SerializeField] private float _forceMultiplier = 60f;
        [SerializeField] private float _heightMultiplier = 0.5f;
        [SerializeField] private float _curveInfluence = 5f;
        [SerializeField] private float _minimumForce = 0.4f;
        [SerializeField, Range(-1f, 1f)] private float _minimumDot = 0.2f;

        [Header("Help Settings")]
        [SerializeField] private Vector3 _helpInfluence = new Vector3(0f, 0f, 0f);
        [SerializeField] private float _helpRadius = 2f;

        [Header("Bezier")]
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _mid;
        [SerializeField] private Transform _end;
        [SerializeField, Range(1, 10)] private float _extrapolation = 2f;
        [SerializeField, Range(3, 100)] private int _points = 10;

        [Header("Bindings")]
        [SerializeField] private PokeBall _pokeBall;
        [SerializeField] private Transform _pokeBallSlot;
        [SerializeField] private Collider _pointerCollider;
        [SerializeField] private PokeBallFactory _pokeBallFactory;

        // We use our own history of touch delta values
        private Queue<Vector2> _deltaHistory = new Queue<Vector2>();
        [SerializeField] private int _deltaHistorySize = 10;

        // We also store the last received positions
        private Vector2 _lastTouchScreenPos;
        private Vector3 _lastTouchWorldPos;

        private bool _isDragging = false;

        // Compute force using the average delta from our history
        private float Force => GetAverageDelta().magnitude * _forceMultiplier;
        private bool HasPokeBall => _pokeBall != null;

        private void OnEnable()
        {
            // Subscribe to Litho touch events
            Litho.OnTouchStart += HandleTouchStart;
            Litho.OnTouchHold  += HandleTouchHold;
            Litho.OnTouchEnd   += HandleTouchEnd;
        }

        private void OnDisable()
        {
            Litho.OnTouchStart -= HandleTouchStart;
            Litho.OnTouchHold  -= HandleTouchHold;
            Litho.OnTouchEnd   -= HandleTouchEnd;
        }

        private void HandleTouchStart(Vector2 touchScreenPos, Vector2 touchWorldPos)
        {
            // Only react if a Litho device is connected and we have a poke ball
            if (!Litho.IsConnected || !HasPokeBall)
                return;

            StartDragging(touchScreenPos, touchWorldPos);
        }

        private void HandleTouchHold(Vector2 touchScreenPos, Vector2 touchWorldPos)
        {
            if (!_isDragging)
                return;

            // Smoothly update the poke ball’s position toward the current touch world position.
            FollowPointer(touchWorldPos);

            // Record delta (difference in screen position) to compute the throw force later.
            if (_lastTouchScreenPos != Vector2.zero)
            {
                Vector2 delta = touchScreenPos - _lastTouchScreenPos;
                _deltaHistory.Enqueue(delta);
                if (_deltaHistory.Count > _deltaHistorySize)
                    _deltaHistory.Dequeue();
            }
            _lastTouchScreenPos = touchScreenPos;
            _lastTouchWorldPos = ConvertToWorldPosition(touchWorldPos);
        }

        private void HandleTouchEnd(Vector2 touchScreenPos, Vector2 touchWorldPos)
        {
            if (!_isDragging)
                return;

            StopDragging();
        }

       private void StartDragging(Vector2 touchScreenPos, Vector2 touchWorldPos)
       {
           Debug.Log("Pre Position:");
           Debug.Log(_pokeBall.transform.position);

           // Convert the normalized touchScreenPos to world position.
           Vector3 worldPos = ConvertToWorldPosition(touchScreenPos);
           _pokeBall.transform.position = worldPos;
           _pokeBall.transform.rotation = _pokeBallSlot.rotation;

           _pokeBall.DisableGravity();
           _pokeBall.ClearVelocities();

           _isDragging = true;
           _deltaHistory.Clear();
           _lastTouchScreenPos = touchScreenPos;
           _lastTouchWorldPos = worldPos;

           Debug.Log("Post Position:");
           Debug.Log(_pokeBall.transform.position);
       }


        // In this example we assume Litho's touch world position is in a compatible coordinate system.
        // If needed, adjust this conversion (for example, by setting a default z based on your poke ball slot).
        private Vector3 ConvertToWorldPosition(Vector2 normalizedTouchPos)
        {
            // Convert normalized coordinates (0–1, top-left origin) to screen coordinates (bottom-left origin)
            float screenX = normalizedTouchPos.x * Screen.height;
            float screenY = (1f - normalizedTouchPos.y) * Screen.width;

            // Choose a proper depth – for instance, the distance from the camera to the poke ball slot.
            float depth = Vector3.Distance(Camera.main.transform.position, _pokeBallSlot.position);

            Vector3 screenPos = new Vector3(screenY, screenX, depth);
            return Camera.main.ScreenToWorldPoint(screenPos);
        }



        private void FollowPointer(Vector2 touchWorldPos)
        {
            Vector3 targetPos = ConvertToWorldPosition(touchWorldPos);
            _pokeBall.transform.position = Vector3.Slerp(
                _pokeBall.transform.position,
                targetPos,
                Time.deltaTime * _followSpeed);
        }

        private Vector2 GetAverageDelta()
        {
            Vector2 sum = Vector2.zero;
            foreach (var delta in _deltaHistory)
            {
                sum += delta;
            }
            return _deltaHistory.Count > 0 ? sum / _deltaHistory.Count : Vector2.zero;
        }

        private void StopDragging()
        {
            _isDragging = false;
            Vector2 avgDelta = GetAverageDelta();
            float dot = Vector2.Dot(avgDelta.normalized, Vector2.right);
            float force = avgDelta.magnitude * _forceMultiplier;
            bool shouldThrow = dot > _minimumDot && force > _minimumForce;
            if (shouldThrow) {
            Throw();
            }
            else {
            Reset();
            }

        }


        private void FixedUpdate()
        {
            if (!_isDragging)
                return;
            AddTorque();
        }

        // Similar torque logic to your mouse-based Thrower.
        private void AddTorque()
        {
            Vector3 pokeBallPos = _pokeBall.transform.position;
            Vector3 pointerPos = _lastTouchWorldPos;
            Vector3 deltaDirection = transform.TransformDirection(GetAverageDelta().normalized);
            Vector3 directionToBall = (pokeBallPos - pointerPos).normalized;
            if (deltaDirection.magnitude > 0)
            {
                Vector3 cross = Vector3.Cross(directionToBall, deltaDirection);
                Vector3 torque = cross * _torqueMultiplier * -1f;
                _pokeBall.AddTorque(torque);
            }
        }

        private void Throw()
        {
            // Capture start position
            Vector3 startPosition = _pokeBall.transform.position;
            _start.position = startPosition;

            // Calculate the throw vector based on the average pointer (touch) delta.
            Vector2 avgDelta = GetAverageDelta();
            Vector2 pointerInfluence = new Vector2(0f, 1f);
            Vector3 pointerDirection = avgDelta.normalized;
            Vector3 influencedPointerDirection = Vector3.Scale(pointerDirection, pointerInfluence);
            Vector3 localPointerDirection = transform.TransformDirection(influencedPointerDirection);
            Vector3 throwVector = (transform.forward * (Force * 2f) + localPointerDirection * Force).normalized * Force;
            Vector3 endPosition = startPosition + throwVector;

            // Determine a mid position for the Bezier curve.
            Vector3 midPosition = Vector3.Lerp(startPosition, endPosition, 0.5f);
            midPosition.y += Force * _heightMultiplier * (_pokeBall.IsCharged ? 0.5f : 1f);
            _mid.position = midPosition;

            //Apply a curve if the poke ball is charged.
            if (_pokeBall.IsCharged)
            {
                Vector3 curveDirection = new Vector3(-localPointerDirection.x, 0, 0).normalized;
                endPosition += curveDirection * _curveInfluence;
            }


            // If a Pokemon is nearby, help aim toward it.
            Transform pokemon = GameObject.FindWithTag(Tag.Pokemon).transform;
            bool isOnHelpRange = Vector3.Distance(pokemon.position, endPosition) < _helpRadius;
            if (isOnHelpRange)
            {
                endPosition.x = Mathf.Lerp(endPosition.x, pokemon.position.x, _helpInfluence.x);
                endPosition.y = Mathf.Lerp(endPosition.y, pokemon.position.y, _helpInfluence.y);
                endPosition.z = Mathf.Lerp(endPosition.z, pokemon.position.z, _helpInfluence.z);
                _end.position = endPosition;
            }

            // Generate the extrapolated Bezier path.
            List<Vector3> path = Bezier.GetExtrapolatedPath(
                startPosition, midPosition, endPosition, 0f, _extrapolation, _points);

            // Execute the throw.
            _pokeBall.Throw(path);
            _pokeBall.transform.SetParent(null);
            _pokeBall = null;
            DOVirtual.DelayedCall(1f, SpawnPokeBall);
        }

        private void SpawnPokeBall()
        {
            _pokeBall = _pokeBallFactory.Create(_pokeBallSlot.position, _pokeBallSlot.rotation);
            _pokeBall.transform.SetParent(_pokeBallSlot);
        }

        private void Reset()
        {
            _pokeBall.ClearVelocities();
            _pokeBall.transform.position = _pokeBallSlot.position;
            _pokeBall.transform.rotation = _pokeBallSlot.rotation;
        }
    }
}
