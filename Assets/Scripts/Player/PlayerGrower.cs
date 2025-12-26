using UnityEngine;
using Sirenix.OdinInspector;

public class PlayerGrower : MonoBehaviour
{
    [BoxGroup("Grow Settings")]
    [LabelText("몸통 프리팹")] public GameObject bodyPrefab;
    [BoxGroup("Grow Settings")]
    [LabelText("몸통 마디 간격")] public float segmentOffset = 0.6f;

    // 나뭇잎을 먹었을 때 호출되어 새로운 몸통 마디를 꼬리 뒤에 생성하고 연결함
    public void AddSegment(PlayerBody bodyController)
    {
        if (bodyPrefab == null || bodyController.bodyJoints.Count == 0) return;

        HingeJoint2D lastJoint = bodyController.bodyJoints[bodyController.bodyJoints.Count - 1];
        Transform lastSeg = lastJoint.transform;
        
        // 마지막 마디의 뒤쪽 방향으로 설정된 간격만큼 떨어진 생성 위치 계산
        Vector3 spawnPos = lastSeg.position + (-lastSeg.right * segmentOffset);

        // 새 마디 생성 및 위치/회전 초기화
        GameObject newSeg = Instantiate(bodyPrefab, transform);
        newSeg.transform.position = spawnPos;
        newSeg.transform.rotation = lastSeg.rotation;
        
        // 생성된 마디 이름을 순번에 맞게 지정 (예: Body4)
        newSeg.name = "Body" + (bodyController.bodyJoints.Count + 1);

        // 새 마디의 관절을 이전 꼬리 마디의 리지드바디에 물리적으로 연결
        HingeJoint2D newJoint = newSeg.GetComponentInChildren<HingeJoint2D>();
        if (newJoint != null)
        {
            newJoint.connectedBody = lastJoint.attachedRigidbody;
            bodyController.bodyJoints.Add(newJoint);
            
            // 마디 추가 후 꼬리 참조를 최신화
            bodyController.UpdateTailReference();
        }
    }
}