using UnityEngine;
using UnityEngine.UI;

public class MusicToggleUI : MonoBehaviour
{
    public Image background;
    public RectTransform handle;

    public Color onColor = new Color(0f, 0.9f, 1f);
    public Color offColor = new Color(0.15f, 0.2f, 0.23f);

    public Vector2 onPosition = new Vector2(18f, 0f);
    public Vector2 offPosition = new Vector2(-18f, 0f);

    public void UpdateVisual(bool isOn)
    {
        background.color = isOn ? onColor : offColor;
        handle.anchoredPosition = isOn ? onPosition : offPosition;
    }
}