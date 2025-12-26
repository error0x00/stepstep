using UnityEngine;
using System.Collections;

public class MouthWiggler : MonoBehaviour
{
    [Header("Target Mouths")]
    public Transform mouthL;
    public Transform mouthR;

    [Header("Animation Settings")]
    public float biteSpeed = 20f;
    public float returnSpeed = 10f;
    public float biteAngle = 45f;

    private Quaternion defaultRotL;
    private Quaternion defaultRotR;
    private Coroutine biteRoutine;

    private void Awake()
    {
        if (mouthL) defaultRotL = mouthL.localRotation;
        if (mouthR) defaultRotR = mouthR.localRotation;
    }

    // 입 벌리고 닫는 애니메이션 수행
    public void DoBite()
    {
        if (biteRoutine != null) StopCoroutine(biteRoutine);
        biteRoutine = StartCoroutine(BiteRoutine());
    }

    private IEnumerator BiteRoutine()
    {
        if (mouthL == null || mouthR == null) yield break;

        Quaternion targetRotL = defaultRotL * Quaternion.Euler(0, 0, biteAngle);
        Quaternion targetRotR = defaultRotR * Quaternion.Euler(0, 0, -biteAngle);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * biteSpeed;
            mouthL.localRotation = Quaternion.Lerp(defaultRotL, targetRotL, t);
            mouthR.localRotation = Quaternion.Lerp(defaultRotR, targetRotR, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            mouthL.localRotation = Quaternion.Lerp(targetRotL, defaultRotL, t);
            mouthR.localRotation = Quaternion.Lerp(targetRotR, defaultRotR, t);
            yield return null;
        }
        
        mouthL.localRotation = defaultRotL;
        mouthR.localRotation = defaultRotR;
    }
}