using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    [SerializeField] private PlayerMotion motion;
    private Camera mainCamera;

    private void Awake()
    {
        if (motion == null) 
            motion = GetComponent<PlayerMotion>();
            
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 마우스 화면 좌표를 월드 좌표로 변환하여 시선 처리
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        motion.LookAt(mouseWorldPos);
    }

    // Input Action: StepLeft 연결
    public void OnLeft(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            motion.TryStep(StepType.Left);
        }
    }

    // Input Action: StepRight 연결
    public void OnRight(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            motion.TryStep(StepType.Right);
        }
    }
}