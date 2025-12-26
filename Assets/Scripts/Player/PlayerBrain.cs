using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    private PlayerMover mover;
    private PlayerBody body;
    private PlayerGrower grower;
    private List<LegWiggler> wigglers = new List<LegWiggler>();
    private List<MouthWiggler> mouths = new List<MouthWiggler>();
    private Camera mainCam;

    private void Awake()
    {
        mover = GetComponent<PlayerMover>();
        body = GetComponent<PlayerBody>();
        grower = GetComponent<PlayerGrower>();
        mainCam = Camera.main;
        
        RefreshComponents();
    }

    // 자식 오브젝트들로부터 다리(Wiggler)와 입(Mouth) 컴포넌트를 찾아 리스트를 최신화함
    public void RefreshComponents()
    {
        wigglers.Clear();
        mouths.Clear();
        wigglers.AddRange(GetComponentsInChildren<LegWiggler>());
        mouths.AddRange(GetComponentsInChildren<MouthWiggler>());
    }

    private void Update()
    {
        // 마우스 월드 좌표를 계산하여 머리가 바라보게 함
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        body.LookAt(mousePos);

        // 1. 리듬 상태 업데이트
        mover.UpdateRhythm();
        
        // 2. 몸통 회전 및 꼬리 고정 (원본 로직 순서)
        body.RefreshBody(mover.IsRhythmActive);
    }

    // Input Action: StepLeft[/Keyboard/a]와 연결됨
    public void OnStepLeft(InputValue value)
    {
        if (value.isPressed) 
        {
            mover.TryStep(StepType.Left);
            body.ExecuteLegStep(StepType.Left, wigglers);
        }
    }

    // Input Action: StepRight[/Keyboard/d]와 연결됨
    public void OnStepRight(InputValue value)
    {
        if (value.isPressed) 
        {
            mover.TryStep(StepType.Right);
            body.ExecuteLegStep(StepType.Right, wigglers);
        }
    }

    // Input Action: Bite[/Mouse/leftButton]와 연결됨
    public void OnBite(InputValue value)
    {
        if (value.isPressed)
        {
            foreach (var mouth in mouths) mouth.DoBite();
            body.CheckForFood(() => Grow());
        }
    }

    // MouthWiggler에서 나뭇잎을 먹었을 때 호출되어 성장을 수행함
    public void Grow()
    {
        grower.AddSegment(body);
        RefreshComponents();
    }
}