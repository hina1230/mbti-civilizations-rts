using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MBTICivilizations.Camera
{
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float moveSpeed = 30f;
        [SerializeField] private float edgeScrollSpeed = 25f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float smoothTime = 0.1f;

        [Header("Camera Limits")]
        [SerializeField] private float minHeight = 10f;
        [SerializeField] private float maxHeight = 80f;
        [SerializeField] private float mapBorderX = 200f;
        [SerializeField] private float mapBorderZ = 200f;

        [Header("Edge Scrolling")]
        [SerializeField] private bool enableEdgeScrolling = true;
        [SerializeField] private float edgeBorderThickness = 10f;

        [Header("Camera Angles")]
        [SerializeField] private float cameraAngle = 45f;
        [SerializeField] private float minCameraAngle = 30f;
        [SerializeField] private float maxCameraAngle = 80f;

        private UnityEngine.Camera cam;
        private Transform cameraTransform;
        private Vector3 targetPosition;
        private float targetZoom;
        private float currentZoom;
        private Vector3 velocity = Vector3.zero;
        private float zoomVelocity = 0f;

        private Vector3 lastMousePosition;
        private bool isRotating = false;
        private bool isPanning = false;

        private void Awake()
        {
            cam = GetComponentInChildren<UnityEngine.Camera>();
            if (cam == null)
            {
                GameObject camObject = new GameObject("RTSCamera");
                camObject.transform.SetParent(transform);
                cam = camObject.AddComponent<UnityEngine.Camera>();
            }
            cameraTransform = cam.transform;
        }

        private void Start()
        {
            SetupCamera();
            targetPosition = transform.position;
            currentZoom = transform.position.y;
            targetZoom = currentZoom;
        }

        private void SetupCamera()
        {
            cameraTransform.localPosition = new Vector3(0, 0, -10);
            cameraTransform.localRotation = Quaternion.Euler(cameraAngle, 0, 0);
            
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseInput();
            HandleEdgeScrolling();
            HandleZoom();
            HandleCameraRotation();
            HandleMiddleMousePanning();
            
            UpdateCameraPosition();
        }

        private void HandleKeyboardInput()
        {
            Vector3 inputDirection = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                inputDirection.z += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                inputDirection.z -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                inputDirection.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                inputDirection.x += 1;

            if (inputDirection != Vector3.zero)
            {
                inputDirection = Quaternion.Euler(0, transform.eulerAngles.y, 0) * inputDirection;
                inputDirection.Normalize();
                targetPosition += inputDirection * moveSpeed * Time.deltaTime;
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(2))
            {
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                isPanning = false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                isRotating = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isRotating = false;
            }
        }

        private void HandleEdgeScrolling()
        {
            if (!enableEdgeScrolling || isPanning || isRotating)
                return;

            Vector3 mousePos = Input.mousePosition;
            Vector3 moveDirection = Vector3.zero;

            if (mousePos.x < edgeBorderThickness)
                moveDirection.x = -1;
            else if (mousePos.x > Screen.width - edgeBorderThickness)
                moveDirection.x = 1;

            if (mousePos.y < edgeBorderThickness)
                moveDirection.z = -1;
            else if (mousePos.y > Screen.height - edgeBorderThickness)
                moveDirection.z = 1;

            if (moveDirection != Vector3.zero)
            {
                moveDirection = Quaternion.Euler(0, transform.eulerAngles.y, 0) * moveDirection;
                targetPosition += moveDirection * edgeScrollSpeed * Time.deltaTime;
            }
        }

        private void HandleZoom()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                targetZoom -= scrollInput * zoomSpeed * 10f;
                targetZoom = Mathf.Clamp(targetZoom, minHeight, maxHeight);
            }

            if (Input.GetKey(KeyCode.PageUp))
                targetZoom -= zoomSpeed * Time.deltaTime * 10f;
            if (Input.GetKey(KeyCode.PageDown))
                targetZoom += zoomSpeed * Time.deltaTime * 10f;

            targetZoom = Mathf.Clamp(targetZoom, minHeight, maxHeight);
        }

        private void HandleCameraRotation()
        {
            if (isRotating)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                transform.Rotate(Vector3.up, mouseDelta.x * rotationSpeed * Time.deltaTime, Space.World);
                
                cameraAngle -= mouseDelta.y * rotationSpeed * Time.deltaTime * 0.5f;
                cameraAngle = Mathf.Clamp(cameraAngle, minCameraAngle, maxCameraAngle);
                cameraTransform.localRotation = Quaternion.Euler(cameraAngle, 0, 0);
                
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetKey(KeyCode.Q))
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
            if (Input.GetKey(KeyCode.E))
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void HandleMiddleMousePanning()
        {
            if (isPanning)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                Vector3 move = new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * moveSpeed * 0.01f;
                move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * move;
                targetPosition += move;
                lastMousePosition = Input.mousePosition;
            }
        }

        private void UpdateCameraPosition()
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -mapBorderX, mapBorderX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -mapBorderZ, mapBorderZ);

            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, smoothTime);
            
            smoothedPosition.y = currentZoom;
            transform.position = smoothedPosition;

            float zoomFactor = (currentZoom - minHeight) / (maxHeight - minHeight);
            float adjustedAngle = Mathf.Lerp(minCameraAngle, maxCameraAngle, zoomFactor);
            cameraTransform.localRotation = Quaternion.Euler(adjustedAngle, 0, 0);
        }

        public void FocusOnPosition(Vector3 position)
        {
            targetPosition = new Vector3(position.x, targetPosition.y, position.z);
        }

        public void SetCameraLimits(float borderX, float borderZ)
        {
            mapBorderX = borderX;
            mapBorderZ = borderZ;
        }

        public Vector3 GetMouseWorldPosition()
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
            {
                return hit.point;
            }
            
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }

        public void ResetCamera()
        {
            targetPosition = Vector3.zero;
            targetZoom = (minHeight + maxHeight) / 2f;
            transform.rotation = Quaternion.identity;
            cameraAngle = 45f;
        }
    }
}