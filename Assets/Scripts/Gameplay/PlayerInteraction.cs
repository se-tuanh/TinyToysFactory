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

    private void Update()
    {
        // Check for Left Mouse Click
        if (Input.GetMouseButtonDown(0))
        {
            // 1. Is the mouse over a UI Element? (Block click-through)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Let the UI handle it (Button click, etc.)
                return;
            }

            // 2. Perform 2D Raycast to the world
            Vector2 mousePos2D = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                // Did we hit a Machine?
                Machine machine = hit.collider.GetComponent<Machine>();
                if (machine != null)
                {
                    machine.TryStartBatch();
                }
            }
        }
    }
}
