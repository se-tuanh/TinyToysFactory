using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PlayerInteraction — Handles global mouse clicks.
/// Pre-checks if the mouse is hovering over a UI canvas element (like Event popups).
/// If not, it shoots a 2D raycast to see if the player clicked a Machine.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private Vector2 _mouseDownPos;
    private Machine _potentialMachine;
    private Machine _draggedMachine;
    private Vector3 _dragOffset;
    private bool    _isDragging;

    private const float DRAG_THRESHOLD = 10f; // pixels

    private void Update()
    {
        // 1. Mouse Down: Find potential target
        if (Input.GetMouseButtonDown(0))
        {
            _mouseDownPos = Input.mousePosition;
            _potentialMachine = null;
            _isDragging = false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 mousePos2D = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null) _potentialMachine = hit.collider.GetComponent<Machine>();
        }

        // 2. Mouse Held: Handle dragging
        if (Input.GetMouseButton(0) && _potentialMachine != null)
        {
            float dist = Vector2.Distance(_mouseDownPos, Input.mousePosition);
            if (!_isDragging && dist > DRAG_THRESHOLD)
            {
                _isDragging = true;
                _draggedMachine = _potentialMachine;
                _dragOffset = _draggedMachine.transform.position - GetMouseWorldPoint();
            }

            if (_isDragging && _draggedMachine != null)
            {
                _draggedMachine.transform.position = GetMouseWorldPoint() + _dragOffset;
            }
        }

        // 3. Mouse Up: End drag or trigger click
        if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging && _draggedMachine != null)
            {
                // Snap to grid for clean layout
                Vector3 pos = _draggedMachine.transform.position;
                pos.x = Mathf.Round(pos.x * 2f) / 2f; 
                pos.y = Mathf.Round(pos.y * 2f) / 2f; 
                pos.z = 0;
                _draggedMachine.transform.position = pos;
            }
            else if (_potentialMachine != null)
            {
                // Simple click event (only if we didn't drag it)
                if (_potentialMachine.CurrentState == Machine.MachineState.Working) _potentialMachine.SpeedUp();
                else _potentialMachine.TryStartBatch();
            }

            _isDragging = false;
            _draggedMachine = null;
            _potentialMachine = null;
        }
    }

    private Vector3 GetMouseWorldPoint()
    {
        Vector3 m = Input.mousePosition;
        m.z = -_mainCamera.transform.position.z;
        return _mainCamera.ScreenToWorldPoint(m);
    }
}
