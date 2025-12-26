using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

public class PlayerGrower : MonoBehaviour
{
    [BoxGroup("Grow Settings")]
    [LabelText("몸통 프리팹")] public GameObject bodyPrefab;
    [BoxGroup("Grow Settings")]
    [LabelText("몸통 마디 간격")] public float segmentOffset = 0.6f;

    [BoxGroup("Color Settings")]
    [LabelText("홀수 마디 색상")] public Color oddColor = Color.white;
    [BoxGroup("Color Settings")]
    [LabelText("짝수 마디 색상")] public Color evenColor = Color.gray;
    [BoxGroup("Color Settings")]
    [LabelText("왼쪽 다리 어둡기 비중")] public float leftLegDarkness = 0.8f;

    // 나뭇잎을 먹었을 때 호출되어 새로운 몸통 마디를 꼬리 뒤에 생성하고 연결함
    public void AddSegment(PlayerBody bodyController)
    {
        if (bodyPrefab == null || bodyController.bodyJoints.Count == 0) return;

        HingeJoint2D lastJoint = bodyController.bodyJoints[bodyController.bodyJoints.Count - 1];
        Transform lastSeg = lastJoint.transform;
        
        Vector3 spawnPos = lastSeg.position + (-lastSeg.right * segmentOffset);

        GameObject newSeg = Instantiate(bodyPrefab, transform);
        newSeg.transform.position = spawnPos;
        newSeg.transform.rotation = lastSeg.rotation;
        
        int segmentIndex = bodyController.bodyJoints.Count + 1;
        newSeg.name = "Body" + segmentIndex;

        SortingGroup lastGroup = lastSeg.GetComponentInParent<SortingGroup>();
        SortingGroup newGroup = newSeg.GetComponent<SortingGroup>();
        if (lastGroup != null && newGroup != null)
        {
            newGroup.sortingOrder = lastGroup.sortingOrder - 1;
        }

        HingeJoint2D newJoint = newSeg.GetComponentInChildren<HingeJoint2D>();
        if (newJoint != null)
        {
            newJoint.connectedBody = lastJoint.attachedRigidbody;
            bodyController.bodyJoints.Add(newJoint);
            
            // 모든 마디의 색상을 갱신하여 새 마디와 기존 마디의 통일성을 맞춤
            RefreshAllSegmentColors(bodyController);
            
            bodyController.UpdateTailReference();
        }
    }

    // 모든 마디를 순회하며 홀수/짝수 색상 및 왼쪽 다리의 명암을 적용함
    [Button("모든 마디 색상 갱신")]
    public void RefreshAllSegmentColors(PlayerBody bodyController)
    {
        for (int i = 0; i < bodyController.bodyJoints.Count; i++)
        {
            int segmentIndex = i + 1;
            GameObject segObj = bodyController.bodyJoints[i].gameObject;
            
            Color targetBaseColor = (segmentIndex % 2 != 0) ? oddColor : evenColor;
            SpriteRenderer[] renderers = segObj.GetComponentsInChildren<SpriteRenderer>();

            foreach (var sr in renderers)
            {
                // 오브젝트 이름에 'legL'이 포함되어 있는지 대소문자 구분 없이 확인하여 색상 적용
                if (sr.name.ToLower().Contains("legl"))
                {
                    Color darkColor = targetBaseColor * leftLegDarkness;
                    darkColor.a = targetBaseColor.a; // 알파값은 원본 유지
                    sr.color = darkColor;
                }
                else
                {
                    sr.color = targetBaseColor;
                }
            }
        }
    }
}