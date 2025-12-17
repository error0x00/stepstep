using UnityEngine;
using System.Collections;

public class LegWiggler : MonoBehaviour
{
    [Header("Target Legs")]
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Animation Settings")]
    public float kickSpeed = 20f;   // 다리 뻗는 속도
    public float returnSpeed = 10f; // 제자리로 돌아오는 속도
    public float kickAngle = 45f;   // 다리가 움직이는 각도 (크기)

    private Quaternion defaultRotL;
    private Quaternion defaultRotR;
    
    // 코루틴 중복 실행 방지
    private Coroutine leftRoutine;
    private Coroutine rightRoutine;

    private void Awake()
    {
        // 원래 각도 저장 (기준점)
        if (leftLeg) defaultRotL = leftLeg.localRotation;
        if (rightLeg) defaultRotR = rightLeg.localRotation;
    }

    // PlayerMotion에서 호출할 함수 (입력 들어왔을 때만 실행)
    public void DoStep(StepType step)
    {
        if (step == StepType.Left && leftLeg != null)
        {
            if (leftRoutine != null) StopCoroutine(leftRoutine);
            leftRoutine = StartCoroutine(KickRoutine(leftLeg, defaultRotL, kickAngle));
        }
        else if (step == StepType.Right && rightLeg != null)
        {
            if (rightRoutine != null) StopCoroutine(rightRoutine);
            // 오른쪽 다리는 각도를 반대로(-kickAngle) 줘야 대칭이 맞을 수 있음 (상황에 따라 조절)
            rightRoutine = StartCoroutine(KickRoutine(rightLeg, defaultRotR, -kickAngle));
        }
    }

    private IEnumerator KickRoutine(Transform leg, Quaternion defaultRot, float angle)
    {
        // 1. 목표 각도 계산
        Quaternion targetRot = defaultRot * Quaternion.Euler(0, 0, angle);
        float t = 0f;

        // 2. 팍! 하고 차기 (Lerp)
        while (t < 1f)
        {
            t += Time.deltaTime * kickSpeed;
            leg.localRotation = Quaternion.Lerp(defaultRot, targetRot, t);
            yield return null;
        }

        // 3. 스르륵 돌아오기
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            leg.localRotation = Quaternion.Lerp(targetRot, defaultRot, t);
            yield return null;
        }
        
        // 4. 확실하게 원위치 고정
        leg.localRotation = defaultRot;
    }
}