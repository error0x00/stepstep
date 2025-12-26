using UnityEngine;
using Sirenix.OdinInspector;

public class PlayerMover : MonoBehaviour
{
    [BoxGroup("Move Settings")]
    [LabelText("발차기 추진력")] public float stepPower = 12f;
    [BoxGroup("Move Settings")]
    [LabelText("보조 전진 힘")] public float forwardAssist = 5f;
    [BoxGroup("Move Settings")]
    [LabelText("입력 유지 시간")] public float resetTime = 0.5f;

    private Rigidbody2D rb;
    private float lastStepTime;
    private StepType lastStepType = StepType.None;
    private float moveDirection = 0f;
    private bool isSpeedMet;

    public bool IsRhythmActive => isSpeedMet;

    private void Awake()
    {
        // 자식인 Head의 Rigidbody2D를 참조
        rb = GetComponentInChildren<Rigidbody2D>();
    }

    // 마지막 입력으로부터 입력 유지 시간이 지났는지 체크하여 리듬 상태 결정
    public void UpdateRhythm()
    {
        isSpeedMet = (Time.time - lastStepTime) <= resetTime;

        if (!isSpeedMet)
        {
            moveDirection = 0f;
            lastStepType = StepType.None;
        }
    }

    // 발차기 신호를 받아 방향을 결정하고 물리적인 추진력을 가함
    public void TryStep(StepType step)
    {
        if (moveDirection == 0f && lastStepType != StepType.None)
        {
            if (lastStepType == StepType.Left && step == StepType.Right) moveDirection = 1f;
            else if (lastStepType == StepType.Right && step == StepType.Left) moveDirection = -1f;
        }
        
        if (moveDirection != 0f)
        {
            float power = (lastStepType == step) ? stepPower * 0.3f : stepPower;
            Vector2 force = (Vector2)rb.transform.right * moveDirection * power + Vector2.right * moveDirection * forwardAssist;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        lastStepType = step;
        lastStepTime = Time.time;
    }
}