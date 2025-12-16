using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Body1–…–Tail 체인의 HingeJoint2D를 모터로 구동하여,
/// 머리를 들면 몸통이 아치 형태로 굽어 올라가도록 자세를 만든다.
/// 이동(AD/DA) 로직과 분리되어 있으며, 자세만 담당한다.
/// </summary>
public class BodyPoseController : MonoBehaviour
{
    [Header("References")]
    // Body 체인을 제공하는 컨트롤러
    public BodyChainController chain;

    // 머리 조준 피봇(좌측 기준). 이 Transform의 회전 각도로 "들기"를 판단한다.
    public Transform aimPivot;

    [Header("Pose Settings")]
    // aimPivot의 Z 회전(도) 중에서 위로 드는 구간만 사용
    public float minAimAngle = -60f;
    public float maxAimAngle = 60f;

    // 몸을 들어올리는 최대 굽힘 각도(첫 관절 기준)
    public float maxBendAngle = 35f;

    // 관절 번호가 뒤로 갈수록 굽힘이 약해지는 감쇠(0~1)
    public float bendFalloff = 0.75f;

    // 고개를 내렸을 때(aim <= 0) 아치를 더 빨리 풀어주는 배율
    public float relaxBoost = 1.5f;

    [Header("Motor Settings")]
    // 오차에 비례해 모터 스피드를 정하는 게인
    public float motorGain = 10f;

    // 모터 속도 제한(도/초)
    public float maxMotorSpeed = 240f;

    // 관절이 버티는 토크(클수록 뻣뻣)
    public float maxMotorTorque = 250f;

    // 고개를 내리면(aim <= 0) 자세를 풀기 위해 쓰는 토크(너무 크면 다시 뜨고, 너무 작으면 안 내려감)
    public float relaxMotorTorque = 120f;

    // aimAngle이 이 값보다 작으면 자세 목표를 0으로 빠르게 복귀시킨다
    public float relaxThreshold = 0.05f;

    [Header("Debug")]
    // 런타임에 체인 조인트를 자동 수집할지 여부
    public bool autoCollectJoints = true;

    private readonly List<HingeJoint2D> joints = new List<HingeJoint2D>();
    private bool initialized;

    private void Awake()
    {
        if (chain == null)
            chain = GetComponentInParent<BodyChainController>();

        if (autoCollectJoints)
            CollectChainJoints();

        ConfigureMotors();
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        if (aimPivot == null || chain == null || joints.Count == 0)
            return;

        AimState aim = GetAimState();
        ApplyPose(aim);
    }

    /// <summary>
    /// Body1에서 시작해 Tail까지 연결된 HingeJoint2D를 순서대로 수집한다.
    /// 각 세그먼트는 "다음 세그먼트로 연결된 HingeJoint2D 하나"를 가진다는 전제다.
    /// </summary>
    private void CollectChainJoints()
    {
        joints.Clear();

        Rigidbody2D current = chain.GetBody1();
        Rigidbody2D tail = chain.GetTail();

        if (current == null || tail == null)
            return;

        int guard = 0;
        while (current != null && current != tail && guard++ < 128)
        {
            HingeJoint2D hj = FindNextHinge(current);
            if (hj == null || hj.connectedBody == null)
                break;

            joints.Add(hj);

            Rigidbody2D next = hj.connectedBody;
            current = next;
        }
    }

    private HingeJoint2D FindNextHinge(Rigidbody2D from)
    {
        HingeJoint2D[] hs = from.GetComponents<HingeJoint2D>();
        for (int i = 0; i < hs.Length; i++)
        {
            if (hs[i] == null)
                continue;

            if (hs[i].connectedBody == null)
                continue;

            return hs[i];
        }
        return null;
    }

    /// <summary>
    /// 수집된 모든 관절에 모터 기본 설정을 적용한다.
    /// </summary>
    private void ConfigureMotors()
    {
        for (int i = 0; i < joints.Count; i++)
        {
            HingeJoint2D hj = joints[i];
            if (hj == null)
                continue;

            hj.useMotor = true;

            JointMotor2D m = hj.motor;
            m.maxMotorTorque = maxMotorTorque;
            m.motorSpeed = 0f;
            hj.motor = m;
        }
    }

    private struct AimState
    {
        public float angleDeg;
        public float up01;
        public bool isUp;
    }

    /// <summary>
    /// aimPivot의 Z 회전각을 읽고, "위로 듦" 정도(0..1)와 상태를 반환한다.
    /// up01은 0도 이상에서만 증가하며, 0도 이하에서는 0이다.
    /// </summary>
    private AimState GetAimState()
    {
        float z = aimPivot.eulerAngles.z;
        if (z > 180f)
            z -= 360f;

        z = Mathf.Clamp(z, minAimAngle, maxAimAngle);

        float up01 = Mathf.InverseLerp(0f, maxAimAngle, z);
        up01 = Mathf.Clamp01(up01);

        AimState s;
        s.angleDeg = z;
        s.up01 = up01;
        s.isUp = z > 0f;
        return s;
    }

    /// <summary>
    /// 목표 굽힘을 관절별로 분배하고 모터로 추종시킨다.
    /// 고개를 들면 아치가 생기고, 고개를 내리면 목표를 0으로 강하게 복귀시킨다.
    /// </summary>
    private void ApplyPose(AimState aim)
    {
        float targetBase;

        if (!aim.isUp || aim.up01 < relaxThreshold)
            targetBase = 0f;
        else
            targetBase = aim.up01 * maxBendAngle;

        float torque = aim.isUp ? maxMotorTorque : relaxMotorTorque;

        float boost = (!aim.isUp || aim.up01 < relaxThreshold) ? relaxBoost : 1f;

        for (int i = 0; i < joints.Count; i++)
        {
            HingeJoint2D hj = joints[i];
            if (hj == null)
                continue;

            if (!hj.useMotor)
                hj.useMotor = true;

            float fall = Mathf.Pow(Mathf.Clamp01(bendFalloff), i);
            float desired = targetBase * fall;

            float current = hj.jointAngle;
            float error = Mathf.DeltaAngle(current, desired);

            float speed = -error * motorGain * boost;
            speed = Mathf.Clamp(speed, -maxMotorSpeed, maxMotorSpeed);

            JointMotor2D m = hj.motor;
            m.maxMotorTorque = torque;
            m.motorSpeed = speed;
            hj.motor = m;
        }
    }
}
