using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ChatSingleMessage : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _text;
    [SerializeField] ContentSizeFitter _sizeFitter;

    float _initialWidth = 0;

    public float InitialWidth { get { return _initialWidth; } }
    public float RectWidth { get { return _text.rectTransform.rect.width; } }
    public TextMeshProUGUI Text { get { return _text; } }
    
    Vector2 GetMessagePivot(bool outgoing)
    {
        float xPivot = outgoing ? 1 : 0;
        return new Vector2(xPivot, 0.5f);
    }

    void ToggleSizeFitter(float maxWidth)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(_text.rectTransform);

        _initialWidth = RectWidth;

        bool isOverMaxWidth = RectWidth >= maxWidth;
        _sizeFitter.horizontalFit = isOverMaxWidth ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
    }

    public void SyncTo(string text, Color c, bool outgoing, float maxWidth)
    {
        _text.color = c;
        _text.alignment = outgoing ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        _text.rectTransform.pivot = GetMessagePivot(outgoing);
        _text.SetText(text);
        ToggleSizeFitter(maxWidth);
    }
}
