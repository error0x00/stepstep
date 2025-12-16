using UnityEngine;

public class LegWiggler : MonoBehaviour
{
    [Header("Target Legs")]
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Wiggle Settings")]
    public float wiggleSpeed = 20f;  // 다리가 흔들리는 속도
    public float wiggleAngle = 30f;  // 다리가 흔들리는 최대 각도
    public float moveThreshold = 0.1f; // 움직임으로 간주할 최소 속도

    private Rigidbody2D rb;
    private Quaternion defaultRotL;
    private Quaternion defaultRotR;

    private void Awake()
    {
        // 자신의 물리 컴포넌트 참조 가져오기
        rb = GetComponent<Rigidbody2D>(); 
        
        // 다리가 할당되어 있다면, 멈췄을 때 돌아갈 초기 각도를 저장
        if (leftLeg) defaultRotL = leftLeg.localRotation;
        if (rightLeg) defaultRotR = rightLeg.localRotation;
    }

    private void Update()
    {
        if (rb == null) return;

        // 현재 이동 속도가 설정한 임계값보다 빠른지 확인
        if (rb.linearVelocity.magnitude > moveThreshold) 
        {
            // 절대값(Abs)을 사용하여 0 ~ 1 사이의 값만 나오도록 제한 (안쪽으로 넘어가지 않음)
            float wave = Mathf.Abs(Mathf.Sin(Time.time * wiggleSpeed));
            
            // 왼쪽 다리: 펼쳐진 상태(Default)에서 0도(일자) 방향으로만 왕복
            if (leftLeg)
            {
                leftLeg.localRotation = defaultRotL * Quaternion.Euler(0, 0, wave * wiggleAngle);
            }
            
            // 오른쪽 다리: 펼쳐진 상태(Default)에서 0도(일자) 방향으로만 왕복 (대칭)
            if (rightLeg)
            {
                rightLeg.localRotation = defaultRotR * Quaternion.Euler(0, 0, -wave * wiggleAngle);
            }
        }
        else
        {
            // 이동이 멈췄을 때는 원래 각도로 부드럽게 복귀
            if (leftLeg) leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, defaultRotL, Time.deltaTime * 10f);
            if (rightLeg) rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, defaultRotR, Time.deltaTime * 10f);
        }
    }
}