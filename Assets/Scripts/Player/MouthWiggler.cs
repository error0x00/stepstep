using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MouthWiggler : MonoBehaviour
{
    [Header("Target Mouths")]
    public Transform mouthL;
    public Transform mouthR;

    [Header("Animation Settings")]
    public float biteSpeed = 20f;   // 입 벌리는 속도
    public float returnSpeed = 10f; // 제자리로 돌아오는 속도
    public float biteAngle = 45f;   // 입 벌어지는 각도

    [Header("Eat Settings")]
    public float eatRadius = 0.5f;  // 먹기 인식 반지름 (동그란 범위)
    public Vector2 eatOffset = new Vector2(0.5f, 0f); // 입 앞쪽으로의 거리 조절
    public int hitsToDestroy = 3;   // 사라지기까지 필요한 클릭 횟수

    private Quaternion defaultRotL;
    private Quaternion defaultRotR;
    private Coroutine biteRoutine;

    // 나뭇잎별 클릭 횟수 기록
    private Dictionary<GameObject, int> leafHitCounts = new Dictionary<GameObject, int>();

    private void Awake()
    {
        if (mouthL) defaultRotL = mouthL.localRotation;
        if (mouthR) defaultRotR = mouthR.localRotation;
    }

    public void DoBite()
    {
        if (biteRoutine != null) StopCoroutine(biteRoutine);
        biteRoutine = StartCoroutine(BiteRoutine());
        
        // 무는 순간 주변에 나뭇잎이 있는지 확인
        CheckForFood();
    }

    private void CheckForFood()
    {
        // 입 앞쪽 위치 계산
        Vector2 checkPos = (Vector2)transform.position + (Vector2)(transform.right * eatOffset.x);
        
        // 동그란 범위 안에 있는 모든 물체 감지
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
                Debug.Log($"나뭇잎을 물었습니다! ({leafHitCounts[leaf]}/{hitsToDestroy})");

                if (leafHitCounts[leaf] >= hitsToDestroy)
                {
                    leafHitCounts.Remove(leaf);
                    Destroy(leaf);
                    // 여기에 나중에 성장(마디 추가) 함수를 연결할 예정입니다.
                }
                
                // 한 번에 나뭇잎 하나만 물도록 처리
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

    // 에디터 화면에서 감지 범위를 빨간 원으로 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 checkPos = (Vector2)transform.position + (Vector2)(transform.right * eatOffset.x);
        Gizmos.DrawWireSphere(checkPos, eatRadius);
    }
}