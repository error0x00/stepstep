using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using QFSW.QC;

public enum StepType { None, Left, Right }

public class PlayerMotion : MonoBehaviour
{
    [BoxGroup("Move Settings")]
    [LabelText("발차기 추진력")] public float stepPower = 12f;
    [BoxGroup("Move Settings")]
    [LabelText("보조 전진 힘")] public float forwardAssist = 5f;
    [BoxGroup("Move Settings")]
    [LabelText("입력 유지 시간")] public float resetTime = 0.5f;

    [BoxGroup("Head Settings")]
    [LabelText("머리 회전 속도")] public float headRotationSpeed = 20f;

    [BoxGroup("Body Settings")]
    [LabelText("몸통 관절 리스트"), InfoBox("자동으로 수집됩니다. 수동 연결 불필요.")]
    public List<HingeJoint2D> bodyJoints = new List<HingeJoint2D>();
    [BoxGroup("Body Settings")]
    [LabelText("관절 굴절 속도")] public float bodyRotationSpeed = 5f;
    [BoxGroup("Body Settings")]
    [LabelText("당기는 유격 강도")] public float pullStrength = 0.15f;

    [BoxGroup("Legs")]
    [ReadOnly] public List<LegWiggler> allWigglers = new List<LegWiggler>();

    private Rigidbody2D rb;
    private float lastStepTime;
    private float moveDirection = 0f;
    private float[] currentTargetAngles;
    private bool isSpeedMet = false;

    private void Reset()
    {
        // 에디터에서 컴포넌트 추가 시 관절 자동 수집
        AutoFindJoints();
    }

    [Button("관절 자동 찾기"), GUIColor(0, 1, 0)]
    public void AutoFindJoints()
    {
        // 자식 오브젝트의 모든 힌지 조인트를 찾아 리스트에 등록
        bodyJoints.Clear();
        HingeJoint2D[] joints = GetComponentsInChildren<HingeJoint2D>();
        bodyJoints.AddRange(joints);
    }

    private void Awake()
    {
        // 물리 제어를 위한 리지드바디 참조 및 다리 스크립트 수집
        rb = GetComponent<Rigidbody2D>();
        if (bodyJoints.Count == 0) AutoFindJoints();
        currentTargetAngles = new float[bodyJoints.Count];

        if (transform.parent != null)
            allWigglers.AddRange(transform.parent.GetComponentsInChildren<LegWiggler>());
    }

    private void Update()
    {
        // 입력 간격에 따른 속도 조건 및 관절 배열 크기 확인
        isSpeedMet = (Time.time - lastStepTime) <= resetTime;

        if (currentTargetAngles.Length != bodyJoints.Count)
            currentTargetAngles = new float[bodyJoints.Count];

        UpdateBodyRotation();
    }

    public void LookAt(Vector2 targetPos)
    {
        // 머리의 독립적인 회전 및 방향 업데이트
        Vector2 diff = targetPos - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, Time.deltaTime * headRotationSpeed));
    }

    private void UpdateBodyRotation()
    {
        // 머리 각도에 따른 몸통 굴절 방향 및 순차 제어
        float headAngle = Mathf.DeltaAngle(0, transform.eulerAngles.z);
        float direction = Mathf.Abs(headAngle) > 15f ? Mathf.Sign(headAngle) : 0f;

        for (int i = 0; i < bodyJoints.Count; i++)
        {
            HingeJoint2D joint = bodyJoints[i];
            if (joint == null) continue;

            float limitAngle = direction > 0 ? joint.limits.max : joint.limits.min;
            float finalTarget = 0f;

            if (isSpeedMet && direction != 0f && i < bodyJoints.Count - 1)
            {
                // 앞 마디가 목표치에 도달했을 때만 다음 마디 굴절 시작
                bool isParentReady = (i == 0) || (Mathf.Abs(currentTargetAngles[i - 1] - (direction > 0 ? bodyJoints[i-1].limits.max : bodyJoints[i-1].limits.min)) < 1f);
                
                if (isParentReady)
                    currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], limitAngle, bodyRotationSpeed * 10f * Time.deltaTime);
            }
            else
            {
                // 조건 미충족 시 바닥으로 각도 복원
                currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], 0f, bodyRotationSpeed * 20f * Time.deltaTime);
            }

            finalTarget = currentTargetAngles[i];
            if (i == 0) finalTarget += headAngle * pullStrength;

            if (joint.attachedRigidbody != null)
                joint.attachedRigidbody.MoveRotation(finalTarget);
        }
    }

    public void TryStep(StepType step)
    {
        // 입력 리듬에 따른 이동 방향 설정 및 다리 동작 실행
        if (Time.time - lastStepTime > resetTime) moveDirection = 0f;
        if (moveDirection == 0f)
        {
            if (step == StepType.Left) moveDirection = 1f;
            else if (step == StepType.Right) moveDirection = -1f;
        }

        foreach (var wiggler in allWigglers) if (wiggler != null) wiggler.DoStep(step);
        
        Move(stepPower, forwardAssist);
        lastStepTime = Time.time;
    }

    private void Move(float power, float assist)
    {
        // Vector3인 transform.right를 Vector2로 변환하여 물리 연산 오류 방지
        Vector2 forward = transform.right;
        Vector2 force = forward * moveDirection * power + Vector2.right * moveDirection * assist;
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}