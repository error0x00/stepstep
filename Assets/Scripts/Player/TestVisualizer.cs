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

    public void OnLeft(InputAction.CallbackContext context)
    {
        // 키를 누르거나 유지하는 상태면 색상 변경
        if (context.started || context.performed)
        {
            imageA.color = pressedColor;
        }
        // 키를 떼면 원래 색으로 복귀
        else if (context.canceled)
        {
            imageA.color = normalColor;
        }
    }

    public void OnRight(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            imageD.color = pressedColor;
        }
        else if (context.canceled)
        {
            imageD.color = normalColor;
        }
    }
}