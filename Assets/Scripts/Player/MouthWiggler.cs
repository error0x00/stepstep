using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MouthWiggler : MonoBehaviour
{
    [Header("Target Mouths")]
    public Transform mouthL;
    public Transform mouthR;

    [Header("Animation Settings")]
    public float biteSpeed = 20f;
    public float returnSpeed = 10f;
    public float biteAngle = 45f;

    [Header("Eat Settings")]
    public float eatRadius = 0.5f;
    public Vector2 eatOffset = new Vector2(0.5f, 0f);
    public int hitsToDestroy = 3;

    private Quaternion defaultRotL;
    private Quaternion defaultRotR;
    private Coroutine biteRoutine;
    private PlayerMotion playerMotion; // 성장을 위해 모션 참조 추가

    private Dictionary<GameObject, int> leafHitCounts = new Dictionary<GameObject, int>();

    private void Awake()
    {
        if (mouthL) defaultRotL = mouthL.localRotation;
        if (mouthR) defaultRotR = mouthR.localRotation;
        
        // 머리(부모)에서 PlayerMotion 찾기
        playerMotion = GetComponentInParent<PlayerMotion>();
    }

    public void DoBite()
    {
        if (biteRoutine != null) StopCoroutine(biteRoutine);
        biteRoutine = StartCoroutine(BiteRoutine());
        
        CheckForFood();
    }

    private void CheckForFood()
    {
        Vector2 checkPos = (Vector2)transform.position + (Vector2)(transform.right * eatOffset.x);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(checkPos, eatRadius);
        
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Leaf"))
            {
                GameObject leaf = hit.gameObject;

                if (!leafHitCounts.ContainsKey(leaf))
                {
                    leafHitCounts.Add(leaf, 0);
                }

                leafHitCounts[leaf]++;

                if (leafHitCounts[leaf] >= hitsToDestroy)
                {
                    leafHitCounts.Remove(leaf);
                    Destroy(leaf);
                    
                    // 나뭇잎이 사라질 때 성장 시도
                    if (playerMotion != null) playerMotion.AddSegment();
                }
                break; 
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 checkPos = (Vector2)transform.position + (Vector2)(transform.right * eatOffset.x);
        Gizmos.DrawWireSphere(checkPos, eatRadius);
    }
}