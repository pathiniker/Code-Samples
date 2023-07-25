using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ChatFeed : MonoBehaviour
{
    const float RUNNING_MESSAGE_ORIGIN_TIMEOUT = 20f; // If most recent message is more than X seconds ago, start new message origin
    
    [Header("Components")]
    [SerializeField] RectTransform _feedContainer;
    [SerializeField] ScrollRect _scrollRect;

    [Header("Prefabs")]
    [SerializeField] UI_UserChatMessage _incomingMessagePrefab;
    [SerializeField] UI_UserChatMessage _outgoingMessagePrefab;
    [SerializeField] UI_SystemChatMessage _systemMessagePrefab;

    List<UI_ChatMessage> _allSpawnedMessages = new List<UI_ChatMessage>();
    UI_ChatMessage _mostRecentMessage;

    bool IsLocalUser(Server.PublicUserData user)
    {
#if UNITY_EDITOR
        if (SuperWindow.RealNoiseSuperWindow.IsInSandboxMode)
            return true;
#endif

        if (user == null)
            return false;

        return user.uuid == GameController.Instance.LocalUser.UUID;
    }

    bool MostRecentMessageIsOverTimeoutThreshold()
    {
        if (_mostRecentMessage == null)
            return true;

        if (_mostRecentMessage.MessageType != ChatMessageType.User)
            return true;

        UI_UserChatMessage userMessage = _mostRecentMessage as UI_UserChatMessage;

        float currentTime = Time.time;
        float mostRecentTime = userMessage.LastUpdatedTimeStamp;

        float timeSinceLast = currentTime - mostRecentTime;
        return timeSinceLast >= RUNNING_MESSAGE_ORIGIN_TIMEOUT;
    }

    bool MostRecentMessageFillsFeedArea()
    {
        if (_mostRecentMessage == null)
            return true;

        if (_mostRecentMessage.MessageType != ChatMessageType.User)
            return true;

        UI_UserChatMessage userMessage = _mostRecentMessage as UI_UserChatMessage;
        float messageHeight = userMessage.CurrentHeight;

        float fadeBuffer = 60f;
        float viewportHeight = _scrollRect.viewport.rect.height - fadeBuffer;
        
        return messageHeight >= viewportHeight;
    }

    bool ShouldSpawnNewUserMessageObject(Server.PublicUserData fromUser, ChatChannelType channel)
    {
        if (_mostRecentMessage == null)
            return true;

        if (_mostRecentMessage.MessageType != ChatMessageType.User)
            return true;

        UI_UserChatMessage userMessage = _mostRecentMessage as UI_UserChatMessage;

        if (userMessage.UserData.uuid != fromUser.uuid)
            return true;

        if (_mostRecentMessage.ChannelType != channel)
            return true;

        if (MostRecentMessageIsOverTimeoutThreshold())
            return true;

        if (MostRecentMessageFillsFeedArea())
            return true;

        return false;
    }

    void EnforceMaxMessagesCount()
    {
        int currentMessagesCount = _allSpawnedMessages.Count;
        int maxCount = ProjectConstants.Online.MAX_CHAT_FEED_MESSAGES;

        int messagesOver = currentMessagesCount - maxCount;

        if (messagesOver <= 0)
            return;

        Debug.Log($"Cull {messagesOver} messages...");

        for (int i = 0; i < messagesOver; i++)
        {
            UI_ChatMessage deleteMsg = _allSpawnedMessages[i];
            _allSpawnedMessages.RemoveAt(i);
            Destroy(deleteMsg.gameObject);
        }
    }

    void OnNewMessageAdded(UI_ChatMessage message)
    {
        if (message == null)
            return;

        _mostRecentMessage = message;
        _allSpawnedMessages.Add(message);

        EnforceMaxMessagesCount();
    }

    public void ClearFeed()
    {
        for (int i = 0; i < _allSpawnedMessages.Count; i++)
        {
            UI_ChatMessage obj = _allSpawnedMessages[i];
            if (obj != null && obj.gameObject != null)
                Destroy(obj.gameObject);
        }

        _allSpawnedMessages.Clear();
    }

    public void Initialize()
    {
        ClearFeed();
    }

    public void RefreshFeed()
    {
        for (int i = 0; i < _allSpawnedMessages.Count; i++)
        {
            UI_ChatMessage msg = _allSpawnedMessages[i];
            if (msg == null)
                continue;

            msg.ForceRecalculateObjectSize();
        }

        _scrollRect.verticalNormalizedPosition = 0f;
    }

    public void AddMessageToFeed(string message, ChatChannelType channel, Server.PublicUserData fromUser)
    {
        if (ShouldSpawnNewUserMessageObject(fromUser, channel))
        {
            bool isLocalUser = IsLocalUser(fromUser);
            UI_UserChatMessage prefab = isLocalUser ? _outgoingMessagePrefab : _incomingMessagePrefab;
            UI_UserChatMessage newMessage = Instantiate(prefab, _feedContainer);
            newMessage.SyncTo(fromUser, channel, isLocalUser);
            OnNewMessageAdded(newMessage);
        }

        UI_UserChatMessage userMessage = _mostRecentMessage as UI_UserChatMessage;
        userMessage.AddMessage(message);
    }

    public void AddSystemMessageToFeed(Server.PublicUserData user, string message, ChatChannelType channel = ChatChannelType.Global)
    {
        string fromUserName = null;

        if (user != null)
            fromUserName = user.displayName;

        AddSystemMessageToFeed(message, channel, fromUserName);
    }

    public void AddSystemMessageToFeed(string message, ChatChannelType channel, string userName = null)
    {
        System.Text.StringBuilder msg = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(userName))
        {
            msg.Append("<b>");
            msg.Append(userName);
            msg.Append("</b> ");
        }

        msg.Append(message);

        UI_SystemChatMessage systemMessage = Instantiate(_systemMessagePrefab, _feedContainer);
        systemMessage.SyncTo(msg.ToString(), channel);
        OnNewMessageAdded(systemMessage);
    }
}
