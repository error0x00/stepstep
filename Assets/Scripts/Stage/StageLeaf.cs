using UnityEngine;

public class StageLeaf : MonoBehaviour
{
    [Header("Visual Settings")]
    public SpriteRenderer leafB; // 애벌레 뒤에 위치하는 나뭇잎 레이어
    public SpriteRenderer leafF; // 애벌레 앞을 덮는 나뭇잎 레이어 (구멍 뚫리는 효과)
    public Sprite[] frontSprites; // 앞 레이어(LeafF)용 4단계 스프라이트 배열

    private Collider2D leafCollider;

    private void Awake()
    {
        leafCollider = GetComponent<Collider2D>();
        
        // 초기 상태: 앞 레이어 이미지를 온전한 첫 번째 스프라이트로 설정함
        if (leafF != null && frontSprites.Length > 0)
        {
            leafF.sprite = frontSprites[0];
        }
    }

    // 타격 횟수에 따라 앞 레이어(LeafF) 이미지를 교체하고 마지막 단계에서 콜라이더를 비활성화함
    public void UpdateLeafVisual(int hitCount)
    {
        if (frontSprites == null || frontSprites.Length == 0 || leafF == null) return;

        // 타격 횟수에 맞춰 앞 레이어의 뜯겨나간 이미지 적용
        int spriteIndex = Mathf.Clamp(hitCount, 0, frontSprites.Length - 1);
        leafF.sprite = frontSprites[spriteIndex];

        // 마지막 단계 도달 시 물리 판정을 꺼서 통과 가능하게 함 (LeafB 이미지는 배경으로 남음)
        if (spriteIndex == frontSprites.Length - 1)
        {
            if (leafCollider != null) leafCollider.enabled = false;
        }
    }
}