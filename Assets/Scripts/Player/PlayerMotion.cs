using UnityEngine;
using System.Collections.Generic;

public class PlayerMotion : MonoBehaviour
{
    [Header("Settings")]
    public float stepForce = 80f;      // 한 걸음의 힘
    public float liftBonus = 0.8f;     // 머리 들었을 때 위로 당기는 힘 비율

    [Header("Pose")]
    public Transform aimPivot;         // 머리 회전 기준점
    public float poseStiffness = 1000f;// 관절이 버티는 힘
    public float bendFactor = 60f;     // 머리 각도에 따라 몸이 굽혀지는 정도

    private List<Rigidbody2D> allRbs = new List<Rigidbody2D>();
    private List<HingeJoint2D> allJoints = new List<HingeJoint2D>();

    private void Awake()
    {
        GetComponentsInChildren(true, allRbs);
        GetComponentsInChildren(true, allJoints);
    }

    private void FixedUpdate()
    {
        UpdatePose();
    }

    // A/D 키 입력 시 호출
    public void OnStep(float directionX)
    {
        if (aimPivot == null) return;

        // 머리 방향 기준 전진 벡터 계산
        Vector2 forwardDir = aimPivot.right; 
        
        // 머리를 든 정도 계산 (0~1)
        float liftAmount = Mathf.Clamp01(Vector2.Dot(forwardDir, Vector2.up));

        // 최종 힘 벡터 계산 (전진 + 위로 들기)
        Vector2 finalForce = (forwardDir * directionX) + (Vector2.up * liftAmount * liftBonus);
        finalForce *= stepForce;

        // 모든 마디에 힘 분산 적용
        foreach (var rb in allRbs)
        {
            rb.AddForce(finalForce, ForceMode2D.Impulse);
        }
    }

    // 머리 각도에 따라 몸통 굽히기
    private void UpdatePose()
    {
        if (aimPivot == null) return;

        // 머리 각도 계산 (-180 ~ 180)
        float angle = aimPivot.localEulerAngles.z;
        if (angle > 180) angle -= 360;

        // 위를 보고 있을 때만 몸을 세움
        bool isLookingUp = angle > 10f; 
        
        foreach (var joint in allJoints)
        {
            var motor = joint.motor;
            motor.maxMotorTorque = poseStiffness;

            if (isLookingUp)
            {
                // 머리 각도에 비례해서 관절 굽힘
                motor.motorSpeed = -angle * (bendFactor / 10f); 
            }
            else
            {
                // 평소에는 펴지도록 설정
                motor.motorSpeed = 0; 
            }
            joint.motor = motor;
        }
    }
}