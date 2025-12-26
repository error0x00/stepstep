using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TestVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public Image imageA; 
    public Image imageD; 

    [Header("Settings")]
    public Color normalColor = Color.white; 
    public Color pressedColor = new Color(1f, 0.5f, 0.5f); 

    // 왼쪽 입력 상태에 따라 UI 색상을 변경함
    public void OnStepLeft(InputAction.CallbackContext context)
    {
        if (imageA == null) return;
        imageA.color = context.ReadValueAsButton() ? pressedColor : normalColor;
    }

    // 오른쪽 입력 상태에 따라 UI 색상을 변경함
    public void OnStepRight(InputAction.CallbackContext context)
    {
        if (imageD == null) return;
        imageD.color = context.ReadValueAsButton() ? pressedColor : normalColor;
    }
}