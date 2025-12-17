using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public enum StepType { None, Left, Right }

public class PlayerMotion : MonoBehaviour
{
    #region Movement Settings
    [BoxGroup("이동 설정")]
    [LabelText("발차기 주 추진력")]
    public float stepPower = 8f;
    
    [BoxGroup("이동 설정")]
    [LabelText("전진 수평 보조 힘")]
    public float forwardAssist = 3f;
    
    [BoxGroup("이동 설정")]
    [LabelText("방향 초기화 시간"), Tooltip("이 시간 이상 걸음을 안 떼면 방향이 리셋됩니다")]
    public float resetTime = 0.5f;
    
    [BoxGroup("이동 설정")]
    [LabelText("최고 속도 판정 시간"), Tooltip("이 시간 내에 걸으면 최고 속도로 판정")]
    public float maxSpeed = 0.2f;
    
    [BoxGroup("이동 설정")]
    [LabelText("같은 발 연속 페널티"), Tooltip("같은 발로 연속으로 걸을 때 힘 감소 비율")]
    public float penaltyRatio = 0.3f;
    #endregion

    #region Head Control
    [BoxGroup("머리 조작")]
    [LabelText("마우스 감도")]
    public float sensitivity = 0.1f;
    
    [BoxGroup("머리 조작")]
    [LabelText("고개 복귀 속도"), Tooltip("자동으로 정면을 바라보는 속도")]
    public float returnSpeed = 5.0f;
    #endregion

    #region Body Lift
    [BoxGroup("몸 일으키기")]
    [LabelText("토크 강도")]
    [InfoBox("값이 클수록 빠르게 일어나지만 너무 크면 뒤로 넘어갈 수 있습니다.", InfoMessageType.Warning)]
    public float liftTorqueStrength = 15.0f;
    
    [BoxGroup("몸 일으키기")]
    [LabelText("상승 힘 배율")]
    public float liftForceMultiplier = 2.0f;
    
    [BoxGroup("몸 일으키기")]
    [LabelText("몸 들기 시작 각도")]
    public float minAngleForLift = 15f;
    #endregion

    #region Floor Detection
    [BoxGroup("바닥 감지")]
    [LabelText("바닥 레이어")]
    public LayerMask floorLayer;
    #endregion

    #region Status (Read Only)
    [TitleGroup("실시간 상태", "현재 플레이어의 상태를 보여줍니다")]
    [HorizontalGroup("실시간 상태/Row1")]
    [LabelText("현재 속도"), DisplayAsString]
    public float CurrentSpeed { get; private set; }
    
    [HorizontalGroup("실시간 상태/Row1")]
    [LabelText("이동 방향"), DisplayAsString]
    private string MoveDirectionDisplay => moveDirection == 0 ? "정지" : moveDirection > 0 ? "전진" : "후진";
    
    [HorizontalGroup("실시간 상태/Row2")]
    [LabelText("마지막 발"), DisplayAsString]
    private string LastStepDisplay => lastStep == StepType.None ? "-" : lastStep == StepType.Left ? "왼발" : "오른발";
    
    [HorizontalGroup("실시간 상태/Row2")]
    [LabelText("현재 머리 각도"), DisplayAsString]
    private string CurrentHeadAngleDisplay => $"{currentHeadAngle:F1}°";
    #endregion

    #region Private Variables
    [HideInInspector] public List<LegWiggler> allWigglers = new List<LegWiggler>();
    
    private Rigidbody2D rb;
    private Collider2D headCollider;
    private StepType lastStep = StepType.None; 
    private float lastStepTime;
    private float moveDirection = 0f;
    private float currentHeadAngle = 0f;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        headCollider = GetComponentInChildren<Collider2D>();
        
        if (transform.parent != null)
        {
            allWigglers.AddRange(transform.parent.GetComponentsInChildren<LegWiggler>());
        }
    }

    private void FixedUpdate()
    {
        // 머리가 바닥에 닿았으면 회전 멈추기 (레버 효과 방지)
        bool isTouchingFloor = headCollider != null && headCollider.IsTouchingLayers(floorLayer);
        
        if (isTouchingFloor && Mathf.Abs(rb.angularVelocity) > 0.1f)
        {
            // 회전 속도 감쇄
            rb.angularVelocity *= 0.5f;
        }
    }

    public void LookAt(Vector2 mousePosition)
    {
        if (rb == null) return;

        float mouseDelta = 0f;

        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.y.ReadValue() * sensitivity;
        }

        if (Mathf.Abs(mouseDelta) > 0.01f)
        {
            float directionMult = (moveDirection < 0) ? -1f : 1f; 
            currentHeadAngle += mouseDelta * directionMult;
        }
        else
        {
            float timeSinceStep = Time.time - lastStepTime;
            CurrentSpeed = Mathf.InverseLerp(resetTime, maxSpeed, timeSinceStep);
            float currentPenalty = 1f - CurrentSpeed;
            currentHeadAngle = Mathf.Lerp(currentHeadAngle, 0f, returnSpeed * currentPenalty * Time.deltaTime);
        }

        rb.MoveRotation(currentHeadAngle);
    }

    public void TryStep(StepType step)
    {
        if (rb == null) return;

        if (Time.time - lastStepTime > resetTime)
        {
            moveDirection = 0f;
            lastStep = StepType.None;
        }

        if (moveDirection == 0f)
        {
            if (step == StepType.Left)       moveDirection = 1f;  
            else if (step == StepType.Right) moveDirection = -1f; 
        }

        float currentPower = stepPower;
        float currentAssist = forwardAssist;

        if (lastStep != StepType.None && step == lastStep)
        {
            currentPower *= penaltyRatio;
            currentAssist *= penaltyRatio;
        }

        foreach (var wiggler in allWigglers)
        {
            if (wiggler != null) wiggler.DoStep(step);
        }

        Move(currentPower, currentAssist);

        lastStep = step;
        lastStepTime = Time.time;
    }

    private void Move(float power, float assist)
    {
        // 현재 머리 각도 계산 (월드 각도 기준)
        float headAngle = transform.eulerAngles.z;
        if (headAngle > 180f) headAngle -= 360f;
        
        // 후진 시 각도 반전
        float effectiveAngle = moveDirection > 0 ? headAngle : -headAngle;

        // 1. 머리가 바라보는 방향의 힘
        Vector2 lookForce = transform.right * power * moveDirection;

        // 2. 고개를 들었을 때 추가 상승 힘 적용
        if (effectiveAngle > minAngleForLift)
        {
            float liftRatio = Mathf.Clamp01((effectiveAngle - minAngleForLift) / 45f);
            Vector2 liftForce = Vector2.up * power * liftRatio * liftForceMultiplier;
            lookForce += liftForce;
        }

        // 3. 수평 보조 힘
        float assistMultiplier = Mathf.Clamp01(1f - Mathf.Abs(transform.right.y));
        Vector2 pushForce = Vector2.right * moveDirection * assist * assistMultiplier;

        // 힘 적용
        rb.AddForce(lookForce + pushForce, ForceMode2D.Impulse);

        // 4. 토크 적용 (몸을 회전시키는 힘)
        // 머리가 땅에 닿았을 때는 토크를 적용하지 않음 (레버 효과 방지)
        bool isTouchingFloor = headCollider != null && headCollider.IsTouchingLayers(floorLayer);
        
        if (!isTouchingFloor && Mathf.Abs(effectiveAngle) > 5f)
        {
            float torqueDirection = Mathf.Sign(effectiveAngle);
            float torqueMagnitude = Mathf.Abs(effectiveAngle) / 90f;
            
            rb.AddTorque(torqueDirection * liftTorqueStrength * torqueMagnitude * moveDirection, ForceMode2D.Impulse);
        }
    }
}