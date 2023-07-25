using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChatMessageType
{
    System = 0,
    User = 10
}

public class UI_ChatMessage : MonoBehaviour
{
    public virtual ChatMessageType MessageType { get { return ChatMessageType.System; } }

    ChatChannelType _channelType;

    public ChatChannelType ChannelType { get { return _channelType; } }

    public Color GetTextColor()
    {
        return UI_SocialMiniHub.Chat.GetChannelColor(ChannelType);
    }

    public void DoBaseSync(ChatChannelType channel)
    {
        _channelType = channel;
    }

    public virtual void ForceRecalculateObjectSize() { }
}
