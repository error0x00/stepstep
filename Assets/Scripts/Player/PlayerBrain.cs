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

    // 자식 오브젝트로부터 다리 및 입 컴포넌트 수집
    public void RefreshComponents()
    {
        wigglers.Clear();
        mouths.Clear();
        wigglers.AddRange(GetComponentsInChildren<LegWiggler>());
        mouths.AddRange(GetComponentsInChildren<MouthWiggler>());
    }

    private void Update()
    {
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        body.LookAt(mousePos);

        mover.UpdateRhythm();
        body.RefreshBody(mover.IsRhythmActive);
    }

    // 왼쪽 발차기 실행
    public void OnStepLeft(InputAction.CallbackContext context)
    {
        if (context.started) 
        {
            mover.TryStep(StepType.Left);
            body.ExecuteLegStep(StepType.Left, wigglers);
        }
    }

    // 오른쪽 발차기 실행
    public void OnStepRight(InputAction.CallbackContext context)
    {
        if (context.started) 
        {
            mover.TryStep(StepType.Right);
            body.ExecuteLegStep(StepType.Right, wigglers);
        }
    }

    // 물기 및 먹기 실행
    public void OnBite(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            foreach (var mouth in mouths) mouth.DoBite();
            body.CheckForFood(() => Grow());
        }
    }

    // 마디 추가 및 참조 갱신
    public void Grow()
    {
        grower.AddSegment(body);
        RefreshComponents();
    }
}