using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    [SerializeField] private PlayerMotion motion;
    private Camera mainCamera;

    private void Awake()
    {
        // Get PlayerMotion component
        if (motion == null) motion = GetComponent<PlayerMotion>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // Convert mouse position to world space
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        // Update head look direction
        motion.LookAt(worldPos);
    }

    public void OnLeft(InputAction.CallbackContext context)
    {
        // Execute left step logic
        if (context.performed) motion.TryStep(StepType.Left);
    }

    public void OnRight(InputAction.CallbackContext context)
    {
        // Execute right step logic
        if (context.performed) motion.TryStep(StepType.Right);
    }
}