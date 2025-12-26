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

    // 왼쪽 발차기 입력 상태에 따라 UI 색상 변경
    public void OnStepLeft(InputValue value)
    {
        if (imageA != null)
        {
            imageA.color = value.isPressed ? pressedColor : normalColor;
        }
    }

    // 오른쪽 발차기 입력 상태에 따라 UI 색상 변경
    public void OnStepRight(InputValue value)
    {
        if (imageD != null)
        {
            imageD.color = value.isPressed ? pressedColor : normalColor;
        }
    }
}