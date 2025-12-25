using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    [SerializeField] private PlayerMotion motion;
    private Camera mainCamera;

    private void Awake()
    {
        // PlayerMotion 컴포넌트 가져오기
        if (motion == null) motion = GetComponent<PlayerMotion>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 마우스 위치를 월드 좌표로 변환
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        // 머리 조향 업데이트
        motion.LookAt(worldPos);
    }

    public void OnLeft(InputAction.CallbackContext context)
    {
        // 왼쪽 발자국 로직 실행
        if (context.performed) motion.TryStep(StepType.Left);
    }

    public void OnRight(InputAction.CallbackContext context)
    {
        // 오른쪽 발자국 로직 실행
        if (context.performed) motion.TryStep(StepType.Right);
    }

    public void OnBite(InputAction.CallbackContext context)
    {
        // 무는 동작 실행
        if (context.performed) motion.Bite();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Goal 태그에 닿으면 다음 스테이지로 이동
        if (other.CompareTag("Goal"))
        {
            StageManager.Instance.NextStage();
        }
    }
}