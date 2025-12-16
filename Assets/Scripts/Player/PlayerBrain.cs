using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    public PlayerMotion motion;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        if (!motion) motion = GetComponent<PlayerMotion>();
    }

    private void Update()
    {
        // 마우스 조준
        if (Mouse.current != null && motion.aimPivot != null)
        {
            Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 dir = mousePos - (Vector2)motion.aimPivot.position;
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, -60, 60); 
            
            motion.aimPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // Input System 이벤트 연결
    public void OnStepLeft(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) 
        {
            motion.OnStep(-1f);
        }
    }

    public void OnStepRight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) 
        {
            motion.OnStep(1f);
        }
    }
}