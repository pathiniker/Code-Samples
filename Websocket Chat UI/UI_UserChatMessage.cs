using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_UserChatMessage : UI_ChatMessage
{
    public override ChatMessageType MessageType => ChatMessageType.User;

    const float LAYOUT_CORRECTION_AMOUNT = -12f;

    [Header("Settings")]
    [SerializeField] Color _localUserNameColor = Color.green;
    [SerializeField] Color _defaultNameColor = Color.white;
    [SerializeField] float _maxWidth = 250f;

    [Header("User Components")]
    [SerializeField] UI_AvatarIcon _avatar;
    [SerializeField] TextMeshProUGUI _displayName;

    [Header("Bubble Components")]
    [SerializeField] RectTransform _bubbleContainerRect;
    [SerializeField] RectTransform _backgroundRect;

    [Header("Components")]
    [SerializeField] RectTransform _messageContainer;
    [SerializeField] LayoutElement _layoutElement;
    [SerializeField] Button _surfaceButton;

    [Header("Prefabs")]
    [SerializeField] UI_ChatSingleMessage _textPrefab;

    bool _isLocalUser;
    Server.PublicUserData _userData;
    List<UI_ChatSingleMessage> _spawnedMessages = new List<UI_ChatSingleMessage>();
    
    bool _isAtMaxWidth = false;
    float _currentMaxMessageWidth = 0;
    float _lastUpdatedTimeStamp = 0;

    public float LastUpdatedTimeStamp { get { return _lastUpdatedTimeStamp; } }
    public float CurrentHeight { get { return _messageContainer.rect.height; } }
    public Server.PublicUserData UserData { get { return _userData; } }
    
    public List<UI_ChatSingleMessage> SpawnedMessages { get { return _spawnedMessages; } }

    bool ShouldAllowClick()
    {
        float y = transform.localPosition.y;
        float threshold = -100f;
        return y < threshold;
    }

    Color GetNameColor()
    {
        if (_isLocalUser)
            return _localUserNameColor;

        return _defaultNameColor;
    }

    void UpdateObjectSize()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(DoSizeUpdate());
    }

    IEnumerator DoSizeUpdate()
    {
        yield return new WaitForEndOfFrame();
        _layoutElement.preferredHeight = _layoutElement.minHeight + _messageContainer.rect.height + LAYOUT_CORRECTION_AMOUNT;
        UpdateBackgroundWidth();
        ToggleButton(ShouldAllowClick());
        yield break;
    }

    public override void ForceRecalculateObjectSize()
    {
        base.ForceRecalculateObjectSize();

        float maxSize = 0;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_messageContainer);

        for (int i = 0; i < _spawnedMessages.Count; i++)
        {
            UI_ChatSingleMessage msg = _spawnedMessages[i];
            if (msg == null)
                continue;

            maxSize = Mathf.Max(maxSize, msg.RectWidth);
        }

        _currentMaxMessageWidth = maxSize;
        _isAtMaxWidth = false;
        UpdateObjectSize();
    }

    void UpdateBackgroundWidth()
    {
        if (_isAtMaxWidth)
            return;

        float minSizeSetEnd = _maxWidth;

        minSizeSetEnd -= (_currentMaxMessageWidth + 15);
        minSizeSetEnd = Mathf.Max(0, minSizeSetEnd);

        if (_isLocalUser)
            _backgroundRect.SetLeft(minSizeSetEnd);
        else
            _backgroundRect.SetRight(minSizeSetEnd);

        _isAtMaxWidth = minSizeSetEnd <= 1;
    }

    public void SyncTo(Server.PublicUserData userData, ChatChannelType channel, bool isLocalUser)
    {
        DoBaseSync(channel);
        _spawnedMessages.Clear();
        _userData = userData;
        _avatar.SyncTo(userData.avatar, isLocalUser ? FaceDirection.Left : FaceDirection.Right);
        _displayName.SetText(userData.displayName);
        _isLocalUser = isLocalUser;
        _displayName.color = GetNameColor();
        _lastUpdatedTimeStamp = Time.time;
    }

    public void AddMessage(string message)
    {
        message = ProfanityHelper.GetCensoredString(message);

        UI_ChatSingleMessage newMessage = Instantiate(_textPrefab, _messageContainer);
        newMessage.SyncTo(message, GetTextColor(), _isLocalUser, _maxWidth);
        _spawnedMessages.Add(newMessage);

        float width = _spawnedMessages.Count == 1 ? newMessage.InitialWidth : newMessage.RectWidth;

        _currentMaxMessageWidth = Mathf.Max(_currentMaxMessageWidth, Mathf.Abs(width));

        UpdateObjectSize();
        _lastUpdatedTimeStamp = Time.time;
    }

    public void ToggleButton(bool interactable)
    {
        if (_isLocalUser)
            interactable = false;

        _surfaceButton.gameObject.SetActive(interactable); // Need to hide whole thing to protect raycasts
    }

    #region UI Callbacks
    public void OnClick_ChatMessage()
    {
        UI_SocialMiniHub.Chat.ViewUserPopup.SyncTo(this);
    }
    #endregion
}