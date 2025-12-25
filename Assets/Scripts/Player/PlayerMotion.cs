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
    [LabelText("몸통 프리팹")] public GameObject bodyPrefab;
    [BoxGroup("Body Settings")]
    [LabelText("몸통 마디 간격")] public float segmentOffset = 0.6f; 
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
    private List<MouthWiggler> allMouths = new List<MouthWiggler>();
    private Rigidbody2D tailRb;

    // 리듬 활성화 여부를 외부(다리 등)에서 참조하기 위한 프로퍼티
    public bool IsRhythmActive => isSpeedMet;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 설정된 관절이 없을 경우 자동으로 하위 관절들을 수집
        if (bodyJoints.Count == 0) AutoFindJoints();
        currentTargetAngles = new float[bodyJoints.Count];

        RefreshComponents();
        UpdateTailReference();
    }

    // 자식 오브젝트들로부터 다리(Wiggler)와 입(Mouth) 컴포넌트를 찾아 리스트를 최신화함
    private void RefreshComponents()
    {
        allWigglers.Clear();
        allMouths.Clear();
        if (transform.parent != null)
        {
            allWigglers.AddRange(transform.parent.GetComponentsInChildren<LegWiggler>());
            allMouths.AddRange(transform.parent.GetComponentsInChildren<MouthWiggler>());
        }
    }

    // 현재 몸통 마디 리스트 중 가장 마지막 마디를 꼬리로 지정하고 리지드바디를 참조함
    private void UpdateTailReference()
    {
        if (bodyJoints.Count > 0)
        {
            // 새 마디가 추가될 경우 기존 꼬리의 고정을 해제
            if (tailRb != null) tailRb.constraints = RigidbodyConstraints2D.None;
            tailRb = bodyJoints[bodyJoints.Count - 1].attachedRigidbody;
        }
    }

    private void Update()
    {
        // 마지막 입력으로부터 입력 유지 시간이 지났는지 체크하여 리듬 상태 결정
        isSpeedMet = (Time.time - lastStepTime) <= resetTime;

        // 리듬이 끊기면 이동 방향과 마지막 입력 타입을 초기화하여 입력 오류 방지
        if (!isSpeedMet)
        {
            moveDirection = 0f;
            lastStepType = StepType.None;
        }

        // 마디 개수가 변했을 경우 타겟 각도 배열 크기 재조정
        if (currentTargetAngles.Length != bodyJoints.Count)
            currentTargetAngles = new float[bodyJoints.Count];

        UpdateBodyRotation();
        ApplyTailConstraints();
    }

    // 나뭇잎을 먹었을 때 호출되어 새로운 몸통 마디를 꼬리 뒤에 생성하고 연결함
    public void AddSegment()
    {
        if (bodyPrefab == null || bodyJoints.Count == 0) return;

        HingeJoint2D lastJoint = bodyJoints[bodyJoints.Count - 1];
        Transform lastSegmentTransform = lastJoint.transform;
        
        // 마지막 마디의 뒤쪽 방향으로 설정된 간격만큼 떨어진 생성 위치 계산
        Vector3 spawnDirection = -lastSegmentTransform.right;
        Vector3 spawnPos = lastSegmentTransform.position + (spawnDirection * segmentOffset);

        // 새 마디 생성 및 위치/회전 초기화 (부모의 스케일 영향을 최소화하기 위해 월드 좌표 설정)
        GameObject newSegment = Instantiate(bodyPrefab, transform.parent);
        newSegment.transform.position = spawnPos;
        newSegment.transform.rotation = lastSegmentTransform.rotation;
        
        // 생성된 마디 이름을 순번에 맞게 지정 (예: Body4)
        int nextBodyNumber = bodyJoints.Count + 1;
        newSegment.name = "Body" + nextBodyNumber;

        // 새 마디의 관절을 이전 꼬리 마디의 리지드바디에 물리적으로 연결
        HingeJoint2D newJoint = newSegment.GetComponentInChildren<HingeJoint2D>();
        if (newJoint != null)
        {
            newJoint.connectedBody = lastJoint.attachedRigidbody;
            bodyJoints.Add(newJoint);
        }

        // 마디 추가 후 다리 리스트와 꼬리 참조를 최신화
        RefreshComponents();
        UpdateTailReference();
    }

    // 머리가 특정 목표 지점을 바라보도록 부드럽게 회전시킴
    public void LookAt(Vector2 targetPos)
    {
        Vector2 diff = targetPos - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, Time.deltaTime * headRotationSpeed));
    }

    // 리듬 상태에 따라 몸통 마디들을 순차적으로 굴절시켜 물결치는 움직임을 만듦
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
                // 앞 마디가 충분히 꺾였을 때 다음 마디가 꺾이도록 순차적 로직 적용
                bool isParentReady = (i == 0) || (Mathf.Abs(currentTargetAngles[i - 1] - (direction > 0 ? bodyJoints[i-1].limits.max : bodyJoints[i-1].limits.min)) < 1f);
                
                if (isParentReady)
                    currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], limitAngle, bodyRotationSpeed * 10f * Time.deltaTime);
            }
            else
            {
                // 리듬이 없거나 정면일 경우 관절을 일자로 펴줌
                currentTargetAngles[i] = Mathf.MoveTowards(currentTargetAngles[i], 0f, bodyRotationSpeed * 20f * Time.deltaTime);
            }

            finalTarget = currentTargetAngles[i];
            // 머리의 회전 각도를 몸통 첫 마디에 일부 전달하여 유연함 표현
            if (i == 0) finalTarget += headAngle * pullStrength;

            if (joint.attachedRigidbody != null)
                joint.attachedRigidbody.MoveRotation(finalTarget);
        }
    }

    // 리듬이 유지되는 동안 마지막 마디(꼬리)를 지면에 고정하여 추진력을 얻을 지지대를 만듦
    private void ApplyTailConstraints()
    {
        if (tailRb == null) return;

        if (isSpeedMet)
            tailRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        else
            tailRb.constraints = RigidbodyConstraints2D.None;
    }

    // PlayerBrain으로부터 발차기 신호를 받아 방향을 결정하고 물리적인 힘을 가함
    public void TryStep(StepType step)
    {
        // 방향이 결정되지 않은 상태에서 A->D 혹은 D->A 교대 입력 시 이동 방향(앞/뒤) 확정
        if (moveDirection == 0f && lastStepType != StepType.None)
        {
            if (lastStepType == StepType.Left && step == StepType.Right) moveDirection = 1f;
            else if (lastStepType == StepType.Right && step == StepType.Left) moveDirection = -1f;
        }

        // 모든 다리에 발차기 애니메이션 실행 신호 전달
        foreach (var wiggler in allWigglers) if (wiggler != null) wiggler.DoStep(step);
        
        // 확정된 방향과 추진력을 바탕으로 머리에 순간적인 힘(Impulse)을 가함
        if (moveDirection != 0f)
        {
            float power = (lastStepType == step) ? stepPower * 0.3f : stepPower;
            Vector2 force = (Vector2)transform.right * moveDirection * power + Vector2.right * moveDirection * forwardAssist;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        lastStepType = step;
        lastStepTime = Time.time;
    }

    // 입(Mouth) 컴포넌트들에게 물기 동작을 수행하도록 명령함
    public void Bite()
    {
        for (int i = 0; i < allMouths.Count; i++)
        {
            if (allMouths[i] != null) allMouths[i].DoBite();
        }
    }

    // 에디터에서 버튼 클릭 시 하위의 모든 HingeJoint2D를 자동으로 수집함
    [Button("관절 자동 찾기")]
    public void AutoFindJoints()
    {
        bodyJoints.Clear();
        HingeJoint2D[] joints = GetComponentsInChildren<HingeJoint2D>();
        bodyJoints.AddRange(joints);
    }
}