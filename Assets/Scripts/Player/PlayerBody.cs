using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class PlayerBody : MonoBehaviour
{
    [BoxGroup("Head Settings")]
    [LabelText("머리 회전 속도")] public float headRotationSpeed = 20f;

    [BoxGroup("Body Settings")]
    [LabelText("몸통 관절 리스트")] public List<HingeJoint2D> bodyJoints = new List<HingeJoint2D>();
    [BoxGroup("Body Settings")]
    [LabelText("관절 굴절 속도")] public float bodyRotationSpeed = 5f;
    [BoxGroup("Body Settings")]
    [LabelText("당기는 유격 강도")] public float pullStrength = 0.15f;

    [BoxGroup("Body Settings")]
    [Button("관절 자동 찾기")]
    public void AutoFindJoints()
    {
        bodyJoints.Clear();
        bodyJoints.AddRange(GetComponentsInChildren<HingeJoint2D>());
    }

    [BoxGroup("Eat Settings")]
    [LabelText("먹기 인식 범위")] public float eatRadius = 0.5f;
    [BoxGroup("Eat Settings")]
    [LabelText("먹기 인식 오프셋")] public Vector2 eatOffset = new Vector2(0.5f, 0f);
    [BoxGroup("Eat Settings")]
    [LabelText("파괴(성장)에 필요한 타격수")] public int hitsToDestroy = 3;

    private Rigidbody2D headRb;
    private Rigidbody2D tailRb;
    private float[] currentTargetAngles;
    private Dictionary<GameObject, int> leafHitCounts = new Dictionary<GameObject, int>();

    private void Awake()
    {
        headRb = GetComponentInChildren<Rigidbody2D>();
        UpdateTailReference();
    }

    // 머리가 특정 목표 지점을 바라보도록 부드럽게 회전시킴
    public void LookAt(Vector2 targetPos)
    {
        Vector2 diff = targetPos - (Vector2)headRb.transform.position;
        float targetAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        headRb.MoveRotation(Mathf.LerpAngle(headRb.rotation, targetAngle, Time.deltaTime * headRotationSpeed));
    }

    // 전방의 나뭇잎(StageLeaf)을 체크하고 타격 횟수에 따라 이미지를 갱신하거나 성장을 실행함
    public void CheckForFood(System.Action onEatSuccess)
    {
        Vector2 checkPos = (Vector2)headRb.transform.position + (Vector2)(headRb.transform.right * eatOffset.x);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(checkPos, eatRadius);
        
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Leaf"))
            {
                GameObject leafObj = hit.gameObject;
                StageLeaf stageLeaf = leafObj.GetComponent<StageLeaf>();
                
                if (stageLeaf == null) continue;

                if (!leafHitCounts.ContainsKey(leafObj)) leafHitCounts.Add(leafObj, 0);

                // 나뭇잎 타격 횟수 증가 및 단계별 이미지 업데이트 요청
                leafHitCounts[leafObj]++;
                stageLeaf.UpdateLeafVisual(leafHitCounts[leafObj]);

                // 설정된 타격 횟수에 도달하면 데이터를 정리하고 성장 로직 호출
                if (leafHitCounts[leafObj] >= hitsToDestroy)
                {
                    leafHitCounts.Remove(leafObj);
                    onEatSuccess?.Invoke();
                }
                break; 
            }
        }
    }

    // 모든 다리 컴포넌트에 발차기 애니메이션 신호 전달
    public void ExecuteLegStep(StepType step, List<LegWiggler> wigglers)
    {
        foreach (var wiggler in wigglers)
        {
            if (wiggler != null) wiggler.DoStep(step);
        }
    }

    // 현재 몸통 마디 리스트 중 가장 마지막 마디를 꼬리로 지정하고 리지드바디 참조를 갱신함
    public void UpdateTailReference()
    {
        if (bodyJoints.Count > 0)
        {
            if (tailRb != null) tailRb.constraints = RigidbodyConstraints2D.None;
            tailRb = bodyJoints[bodyJoints.Count - 1].attachedRigidbody;
            currentTargetAngles = new float[bodyJoints.Count];
        }
    }

    // 리듬 상태와 회전 방향에 따라 몸통 마디들을 순차적으로 굴절시켜 물결치는 움직임을 생성함
    public void RefreshBody(bool isSpeedMet)
    {
        if (bodyJoints.Count == 0) return;

        float headAngle = Mathf.DeltaAngle(0, headRb.transform.eulerAngles.z);
        float direction = Mathf.Abs(headAngle) > 15f ? Mathf.Sign(headAngle) : 0f;

        for (int i = 0; i < bodyJoints.Count; i++)
        {
            HingeJoint2D joint = bodyJoints[i];
            if (joint == null) continue;

            float limitAngle = direction > 0 ? joint.limits.max : joint.limits.min;

            if (isSpeedMet && direction != 0f && i < bodyJoints.Count - 1)
            {
                bool isParentReady = (i == 0) || (Mathf.Abs(currentTargetAngles[i - 1] - (direction > 0 ? bodyJoints[i-1].limits.max : bodyJoints[i-1].limits.min)) < 1f);
                if (isParentReady)
                    currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], limitAngle, bodyRotationSpeed * 10f * Time.deltaTime);
            }
            else
            {
                currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], 0f, bodyRotationSpeed * 20f * Time.deltaTime);
            }

            float finalTarget = currentTargetAngles[i];
            if (i == 0) finalTarget += headAngle * pullStrength;

            if (joint.attachedRigidbody != null)
                joint.attachedRigidbody.MoveRotation(finalTarget);
        }

        if (tailRb != null)
        {
            if (isSpeedMet) tailRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            else tailRb.constraints = RigidbodyConstraints2D.None;
        }
    }

    // 하이어라키에서 오브젝트 선택 시 먹기 인식 범위를 빨간색 원으로 표시함
    private void OnDrawGizmosSelected()
    {
        if (headRb == null) headRb = GetComponentInChildren<Rigidbody2D>();
        if (headRb == null) return;

        Gizmos.color = Color.red;
        Vector2 checkPos = (Vector2)headRb.transform.position + (Vector2)(headRb.transform.right * eatOffset.x);
        Gizmos.DrawWireSphere(checkPos, eatRadius);
    }
}