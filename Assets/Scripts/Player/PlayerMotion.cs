using UnityEngine;

// None 추가: 아무 발도 밟지 않은 초기 상태
public enum StepType { None, Left, Right }

public class PlayerMotion : MonoBehaviour
{
    [Header("Settings")]
    // 나중에 리듬 보너스를 곱하기 위한 '기본' 힘
    public float baseStepPower = 5f;

    // 일정 시간 이상 입력이 없으면 초기화 (A -> 한참 쉼 -> D 입력 시 꼬임 방지)
    public float resetTime = 0.2f;

    // 추진을 적용할 체인 컨트롤러 (Body1 + Tail 추진)
    public BodyChainController chain;

    // Body1과 Tail에 힘을 분배하는 비율
    public float body1DriveWeight = 0.6f;
    public float tailDriveWeight = 0.4f;

    [Header("Lift")]
    // 머리를 들었을 때 위로 끌어올리는 비율
    public float liftStrength = 0.6f;

    // 위로 드는 최대 비율 (점프 방지)
    public float maxLift = 0.7f;

    [Header("Aim")]
    // 머리 회전 피봇 (좌측 기준)
    public Transform aimPivot;

    // 조준 각도 제한
    public float minAimAngle = -60f;
    public float maxAimAngle = 60f;

    // 마우스 추종 속도
    public float aimFollowSpeed = 20f;

    [Header("State")]
    public StepType lastStep = StepType.None;
    public float lastStepTime = 0f;

    // 전진: 1, 후진: -1
    public float moveDirection = 0f;

    private Rigidbody2D rb;
    private Rigidbody2D body1Rb;
    private Rigidbody2D tailRb;

    // 현재 조준 각도
    private float aimAngle = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (chain == null)
            chain = GetComponentInParent<BodyChainController>();

        if (chain != null)
        {
            body1Rb = chain.GetBody1();
            tailRb = chain.GetTail();
        }
    }

    /// <summary>
    /// 마우스 위치를 기준으로 머리 방향을 회전시킨다.
    /// 이동 로직에는 영향을 주지 않는다.
    /// </summary>
    public void LookAt(Vector2 worldPosition)
    {
        if (aimPivot == null)
            return;

        Vector2 dir = worldPosition - (Vector2)aimPivot.position;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        targetAngle = Mathf.Clamp(targetAngle, minAimAngle, maxAimAngle);

        float t = 1f - Mathf.Exp(-aimFollowSpeed * Time.deltaTime);
        aimAngle = Mathf.LerpAngle(aimAngle, targetAngle, t);

        aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle);
    }

    public void TryStep(StepType step)
    {
        // 0. 시간 체크: 오래 쉬었으면 방향 초기화
        if (Time.time - lastStepTime > resetTime)
        {
            lastStep = StepType.None;
            moveDirection = 0f;
        }

        // 1. 같은 발 연타 방지 (A-A-A 혹은 D-D-D)
        if (lastStep == step)
        {
            lastStepTime = Time.time;
            return;
        }

        // 2. 방향 결정 로직 (방향이 정해지지 않았을 때만 계산)
        if (moveDirection == 0f && lastStep != StepType.None)
        {
            // A -> D = 전진
            if (lastStep == StepType.Left && step == StepType.Right)
                moveDirection = 1f;
            // D -> A = 후진
            else if (lastStep == StepType.Right && step == StepType.Left)
                moveDirection = -1f;
        }

        // 3. 이동 (방향이 정해진 경우에만)
        if (moveDirection != 0f)
            Move();

        lastStep = step;
        lastStepTime = Time.time;
    }

    private void Move()
    {
        // 기본 전진 방향 (머리 방향 기준)
        Vector2 forward = transform.right.normalized;

        // 머리 각도 기반 위로 드는 성분 계산
        float liftDot = Vector2.Dot(aimPivot.up, Vector2.up);
        float lift = Mathf.Clamp01(liftDot) * liftStrength;
        lift = Mathf.Min(lift, maxLift);

        // 전진 + 위쪽 성분을 결합한 최종 이동 방향
        Vector2 pushDir = (forward + Vector2.up * lift).normalized * moveDirection;

        float bw = Mathf.Max(0f, body1DriveWeight);
        float tw = Mathf.Max(0f, tailDriveWeight);
        float sum = bw + tw;

        if (sum < 0.0001f)
        {
            bw = 0.5f;
            tw = 0.5f;
            sum = 1f;
        }

        if (body1Rb != null)
            body1Rb.AddForce(pushDir * baseStepPower * (bw / sum), ForceMode2D.Impulse);

        if (tailRb != null)
            tailRb.AddForce(pushDir * baseStepPower * (tw / sum), ForceMode2D.Impulse);
    }
}
