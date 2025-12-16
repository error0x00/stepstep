using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Head–Body1–BodyMid…–Tail 체인을 관리한다.
/// 런타임 시작 시 씬에 이미 존재하는 조인트 체인을 따라가며 중간 세그먼트를 자동으로 수집한다.
/// 기존 조인트를 파괴하거나 재생성하지 않는다.
/// </summary>
public class BodyChainController : MonoBehaviour
{
    [Header("Chain References")]
    [SerializeField] private Rigidbody2D body1;
    [SerializeField] private Rigidbody2D tail;

    [Header("Segment Prefab")]
    [SerializeField] private GameObject bodySegmentPrefab;

    [Header("Joint Settings")]
    [SerializeField] private bool enableCollisionBetweenConnected = false;
    [SerializeField] private bool useJointLimits = true;
    [SerializeField] private float minLimit = -90f;
    [SerializeField] private float maxLimit = 90f;

    [Header("Auto Physics")]
    [SerializeField] private bool applyAutoPhysics = true;

    [SerializeField] private float body1Mass = 1.5f;
    [SerializeField] private float body1LinearDamping = 0.5f;
    [SerializeField] private float body1AngularDamping = 0.5f;

    [SerializeField] private float tailMass = 5f;
    [SerializeField] private float tailLinearDamping = 3.5f;
    [SerializeField] private float tailAngularDamping = 3.5f;

    [Header("Runtime State")]
    [SerializeField] private List<Rigidbody2D> midSegments = new List<Rigidbody2D>();

    private void Awake()
    {
        RebuildMidSegmentsFromExistingJoints();

        if (!IsTailReachableFromBody1())
            ConnectLastBodyToTail();

        if (applyAutoPhysics)
            ApplyAutoPhysics();
    }

    /// <summary>
    /// Body1에서 시작하여 connectedBody 체인을 따라가며 Tail 전까지를 midSegments로 구성한다.
    /// </summary>
    private void RebuildMidSegmentsFromExistingJoints()
    {
        midSegments.Clear();

        if (body1 == null || tail == null)
            return;

        HashSet<Rigidbody2D> visited = new HashSet<Rigidbody2D>();

        Rigidbody2D current = body1;
        int guard = 0;

        while (current != null && current != tail && guard++ < 128)
        {
            if (!visited.Add(current))
                break;

            Joint2D nextJoint = FindNextJoint(current);
            if (nextJoint == null || nextJoint.connectedBody == null)
                break;

            Rigidbody2D next = nextJoint.connectedBody;

            if (next == tail)
                break;

            midSegments.Add(next);
            current = next;
        }
    }

    /// <summary>
    /// 현재 바디에서 다음 바디로 이어지는 Joint2D를 찾는다.
    /// 대부분의 세그먼트는 다음 세그먼트로 연결된 Joint2D를 하나만 가진다.
    /// </summary>
    private Joint2D FindNextJoint(Rigidbody2D from)
    {
        Joint2D[] joints = from.GetComponents<Joint2D>();
        for (int i = 0; i < joints.Length; i++)
        {
            Joint2D j = joints[i];
            if (j == null)
                continue;

            if (j.connectedBody == null)
                continue;

            return j;
        }

        return null;
    }

    /// <summary>
    /// Body1에서 Tail까지 조인트 체인이 실제로 이어져 있는지 확인한다.
    /// </summary>
    private bool IsTailReachableFromBody1()
    {
        if (body1 == null || tail == null)
            return false;

        HashSet<Rigidbody2D> visited = new HashSet<Rigidbody2D>();

        Rigidbody2D current = body1;
        int guard = 0;

        while (current != null && guard++ < 128)
        {
            if (current == tail)
                return true;

            if (!visited.Add(current))
                return false;

            Joint2D nextJoint = FindNextJoint(current);
            if (nextJoint == null || nextJoint.connectedBody == null)
                return false;

            current = nextJoint.connectedBody;
        }

        return false;
    }

    /// <summary>
    /// Tail이 체인에서 끊겨 있을 때만, 마지막 Body에서 Tail로 조인트를 추가한다.
    /// 기존 조인트는 제거하지 않는다.
    /// </summary>
    private void ConnectLastBodyToTail()
    {
        if (body1 == null || tail == null)
            return;

        Rigidbody2D lastBody = GetLastBody();

        if (lastBody == null || lastBody == tail)
            return;

        if (HasJointOnBodyConnectedTo(lastBody, tail))
            return;

        CreateHinge(lastBody, tail);
    }

    /// <summary>
    /// 중간 Body 세그먼트를 하나 추가한다.
    /// 마지막 Body와 Tail 사이의 중간 위치에 삽입된다.
    /// </summary>
    public void AddBodySegment()
    {
        if (bodySegmentPrefab == null || body1 == null || tail == null)
            return;

        Rigidbody2D lastBody = GetLastBody();
        if (lastBody == null)
            return;

        Vector2 insertPosition = (lastBody.position + tail.position) * 0.5f;

        GameObject segment = Instantiate(
            bodySegmentPrefab,
            insertPosition,
            Quaternion.identity,
            transform
        );

        Rigidbody2D newRb = segment.GetComponent<Rigidbody2D>();
        if (newRb == null)
        {
            Destroy(segment);
            return;
        }

        newRb.linearVelocity = Vector2.zero;
        newRb.angularVelocity = 0f;

        RemoveJointOnBodyConnectedTo(lastBody, tail);

        CreateHinge(lastBody, newRb);
        CreateHinge(newRb, tail);

        midSegments.Add(newRb);

        if (applyAutoPhysics)
            ApplyAutoPhysics();
    }

    /// <summary>
    /// 현재 체인에서 Tail 바로 앞에 있는 Body를 반환한다.
    /// </summary>
    public Rigidbody2D GetLastBody()
    {
        if (midSegments.Count > 0)
            return midSegments[midSegments.Count - 1];

        return body1;
    }

    /// <summary>
    /// 중간 세그먼트 목록을 반환한다.
    /// </summary>
    public IReadOnlyList<Rigidbody2D> GetMidSegments()
    {
        return midSegments;
    }

    /// <summary>
    /// Body1 Rigidbody2D를 반환한다.
    /// </summary>
    public Rigidbody2D GetBody1()
    {
        return body1;
    }

    /// <summary>
    /// Tail Rigidbody2D를 반환한다.
    /// </summary>
    public Rigidbody2D GetTail()
    {
        return tail;
    }

    /// <summary>
    /// 체인 길이에 따라 질량과 감쇠를 선형 보간으로 배분한다.
    /// Tail로 갈수록 무겁고 둔해진다.
    /// </summary>
    private void ApplyAutoPhysics()
    {
        List<Rigidbody2D> chain = new List<Rigidbody2D>();
        chain.Add(body1);

        for (int i = 0; i < midSegments.Count; i++)
        {
            if (midSegments[i] != null)
                chain.Add(midSegments[i]);
        }

        chain.Add(tail);

        int count = chain.Count;
        if (count < 2)
            return;

        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = chain[i];
            if (rb == null)
                continue;

            float t = (float)i / (count - 1);

            rb.mass = Mathf.Lerp(body1Mass, tailMass, t);
            rb.linearDamping = Mathf.Lerp(body1LinearDamping, tailLinearDamping, t);
            rb.angularDamping = Mathf.Lerp(body1AngularDamping, tailAngularDamping, t);
        }
    }

    /// <summary>
    /// 두 Rigidbody 사이에 힌지 조인트를 생성한다.
    /// </summary>
    private void CreateHinge(Rigidbody2D from, Rigidbody2D to)
    {
        if (from == null || to == null)
            return;

        HingeJoint2D joint = from.gameObject.AddComponent<HingeJoint2D>();
        joint.connectedBody = to;
        joint.enableCollision = enableCollisionBetweenConnected;

        joint.autoConfigureConnectedAnchor = true;
        joint.anchor = Vector2.zero;

        joint.useLimits = useJointLimits;
        if (useJointLimits)
        {
            JointAngleLimits2D limits = joint.limits;
            limits.min = minLimit;
            limits.max = maxLimit;
            joint.limits = limits;
        }
    }

    private bool HasJointOnBodyConnectedTo(Rigidbody2D owner, Rigidbody2D target)
    {
        Joint2D[] joints = owner.GetComponents<Joint2D>();
        for (int i = 0; i < joints.Length; i++)
        {
            Joint2D j = joints[i];
            if (j != null && j.connectedBody == target)
                return true;
        }
        return false;
    }

    private void RemoveJointOnBodyConnectedTo(Rigidbody2D owner, Rigidbody2D target)
    {
        Joint2D[] joints = owner.GetComponents<Joint2D>();
        for (int i = 0; i < joints.Length; i++)
        {
            Joint2D j = joints[i];
            if (j != null && j.connectedBody == target)
                Destroy(j);
        }
    }
}
