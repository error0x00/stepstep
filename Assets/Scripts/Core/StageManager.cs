using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Stage List")]
    public GameObject[] stages;
    
    private int currentIndex = 0;
    private GameObject currentStageObject;

    private void Awake()
    {
        // 어디서든 접근 가능하도록 인스턴스 설정
        Instance = this;
    }

    private void Start()
    {
        // 게임 시작 시 첫 번째 스테이지 로드
        LoadStage(0);
    }

    public void LoadStage(int index)
    {
        if (index >= stages.Length)
        {
            Debug.Log("모든 여정을 마쳤습니다.");
            return;
        }

        // 기존 스테이지 삭제
        if (currentStageObject != null)
        {
            Destroy(currentStageObject);
        }

        // 새 스테이지 생성
        currentIndex = index;
        currentStageObject = Instantiate(stages[currentIndex], Vector3.zero, Quaternion.identity);
    }

    public void NextStage()
    {
        // 다음 스테이지로 진행
        LoadStage(currentIndex + 1);
    }
}