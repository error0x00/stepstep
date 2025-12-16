using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    [SerializeField] private PlayerMotion motion;
    private Camera mainCamera; // 마우스 좌표 변환을 위한 카메라 참조

    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (motion == null) 
            motion = GetComponent<PlayerMotion>();
            
        mainCamera = Camera.main; // 메인 카메라 가져오기
    }

    // [New] 매 프레임 실행: 마우스를 바라보게 함
    private void Update()
    {
        // 마우스가 연결되어 있지 않다면 실행하지 않음 (안전장치)
        if (Mouse.current == null) return;

        // 1. 현재 마우스의 화면상 좌표를 가져옴
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        
        // 2. 화면 좌표(Screen) -> 게임 월드 좌표(World)로 변환
        // (카메라가 비추는 실제 게임 세상의 위치로 바꿈)
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // 3. PlayerMotion에게 "저기를 봐!"라고 명령
        motion.LookAt(mouseWorldPos);
    }

    // A키 연결용
    public void OnLeft(InputAction.CallbackContext context)
    {
        // 키를 누르는 순간에만 실행
        if (context.performed)
        {
            motion.TryStep(StepType.Left);
        }
    }

    // D키 연결용
    public void OnRight(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            motion.TryStep(StepType.Right);
        }
    }
}