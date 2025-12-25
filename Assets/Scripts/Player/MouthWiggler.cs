using UnityEngine;
using System.Collections;

public class MouthWiggler : MonoBehaviour
{
    [Header("Target Mouths")]
    public Transform mouthL;
    public Transform mouthR;

    [Header("Animation Settings")]
    public float biteSpeed = 20f;   // 입 벌리는 속도
    public float returnSpeed = 10f; // 제자리로 돌아오는 속도
    public float biteAngle = 45f;   // 입 벌어지는 각도

    private Quaternion defaultRotL;
    private Quaternion defaultRotR;
    
    // 코루틴 중복 실행 방지
    private Coroutine biteRoutine;

    private void Awake()
    {
        // 원래 각도 저장 (기준점)
        if (mouthL) defaultRotL = mouthL.localRotation;
        if (mouthR) defaultRotR = mouthR.localRotation;
    }

    // PlayerMotion에서 호출할 함수
    public void DoBite()
    {
        if (biteRoutine != null) StopCoroutine(biteRoutine);
        biteRoutine = StartCoroutine(BiteRoutine());
    }

    private IEnumerator BiteRoutine()
    {
        if (mouthL == null || mouthR == null) yield break;

        // 1. 목표 각도 계산
        Quaternion targetRotL = defaultRotL * Quaternion.Euler(0, 0, biteAngle);
        Quaternion targetRotR = defaultRotR * Quaternion.Euler(0, 0, -biteAngle);
        float t = 0f;

        // 2. 팍! 하고 벌리기 (Lerp)
        while (t < 1f)
        {
            t += Time.deltaTime * biteSpeed;
            mouthL.localRotation = Quaternion.Lerp(defaultRotL, targetRotL, t);
            mouthR.localRotation = Quaternion.Lerp(defaultRotR, targetRotR, t);
            yield return null;
        }

        // 3. 스르륵 돌아오기
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            mouthL.localRotation = Quaternion.Lerp(targetRotL, defaultRotL, t);
            mouthR.localRotation = Quaternion.Lerp(targetRotR, defaultRotR, t);
            yield return null;
        }
        
        // 4. 확실하게 원위치 고정
        mouthL.localRotation = defaultRotL;
        mouthR.localRotation = defaultRotR;
    }
}