using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

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
    [LabelText("몸통 관절 리스트")]
    public List<HingeJoint2D> bodyJoints = new List<HingeJoint2D>();
    [BoxGroup("Body Settings")]
    [LabelText("관절 굴절 속도")] public float bodyRotationSpeed = 5f;
    [BoxGroup("Body Settings")]
    [LabelText("당기는 유격 강도")] public float pullStrength = 0.15f;

    private Rigidbody2D rb;
    private float lastStepTime;
    private StepType lastStepType = StepType.None;
    private float moveDirection = 0f;
    private float[] currentTargetAngles;
    private bool isSpeedMet = false;
    private List<LegWiggler> allWigglers = new List<LegWiggler>();
    private Rigidbody2D tailRb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyJoints.Count == 0) AutoFindJoints();
        currentTargetAngles = new float[bodyJoints.Count];

        if (transform.parent != null)
            allWigglers.AddRange(transform.parent.GetComponentsInChildren<LegWiggler>());

        // 마지막 관절에 연결된 꼬리 리지드바디 수집
        if (bodyJoints.Count > 0)
        {
            tailRb = bodyJoints[bodyJoints.Count - 1].attachedRigidbody;
        }
    }

    private void Update()
    {
        isSpeedMet = (Time.time - lastStepTime) <= resetTime;

        if (currentTargetAngles.Length != bodyJoints.Count)
            currentTargetAngles = new float[bodyJoints.Count];

        UpdateBodyRotation();
        ApplyTailConstraints();
    }

    public void LookAt(Vector2 targetPos)
    {
        Vector2 diff = targetPos - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, Time.deltaTime * headRotationSpeed));
    }

    private void UpdateBodyRotation()
    {
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
                // 순차적 관절 굴절 제어 (앞마디 리밋 도달 확인)
                bool isParentReady = (i == 0) || (Mathf.Abs(currentTargetAngles[i - 1] - (direction > 0 ? bodyJoints[i-1].limits.max : bodyJoints[i-1].limits.min)) < 1f);
                
                if (isParentReady)
                    currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], limitAngle, bodyRotationSpeed * 10f * Time.deltaTime);
            }
            else
            {
                // 각도 복원
                currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], 0f, bodyRotationSpeed * 20f * Time.deltaTime);
            }

            finalTarget = currentTargetAngles[i];
            if (i == 0) finalTarget += headAngle * pullStrength;

            if (joint.attachedRigidbody != null)
                joint.attachedRigidbody.MoveRotation(finalTarget);
        }
    }

    private void ApplyTailConstraints()
    {
        if (tailRb == null) return;

        // 리듬 활성화 시 꼬리 오브젝트 회전 잠금으로 지지력 확보
        if (isSpeedMet)
        {
            tailRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            tailRb.constraints = RigidbodyConstraints2D.None;
        }
    }

    public void TryStep(StepType step)
    {
        if (Time.time - lastStepTime > resetTime) moveDirection = 0f;

        if (moveDirection == 0f && lastStepType != StepType.None)
        {
            // AD 교대 입력에 따른 이동 방향 결정
            if (lastStepType == StepType.Left && step == StepType.Right) moveDirection = 1f;
            else if (lastStepType == StepType.Right && step == StepType.Left) moveDirection = -1f;
        }

        foreach (var wiggler in allWigglers) if (wiggler != null) wiggler.DoStep(step);
        
        if (moveDirection != 0f)
        {
            // 동일 키 연속 입력 페널티 적용
            float power = (lastStepType == step) ? stepPower * 0.3f : stepPower;
            Vector2 forward = transform.right;
            Vector2 force = forward * moveDirection * power + Vector2.right * moveDirection * forwardAssist;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        lastStepType = step;
        lastStepTime = Time.time;
    }

    [Button("관절 자동 찾기")]
    public void AutoFindJoints()
    {
        bodyJoints.Clear();
        HingeJoint2D[] joints = GetComponentsInChildren<HingeJoint2D>();
        bodyJoints.AddRange(joints);
    }
}