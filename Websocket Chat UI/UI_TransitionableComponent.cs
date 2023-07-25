using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public enum TransitionType
{
    NONE = 0,
    Slide = 10,
    PopScale = 20,
    Fade = 30,
}

public class UI_TransitionableComponent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] TransitionType _transitionType;
    [SerializeField, ShowIf(@"_transitionType", TransitionType.Slide)] SlideDirection _slideInDirection;
    [SerializeField] bool _overrideDuration = false;
    [SerializeField, ShowIf("_overrideDuration")] float _customInDuration = AModal.TRANSITION_IN_TIME;
    [SerializeField, ShowIf("_overrideDuration")] float _customOutDuration = AModal.TRANSITION_OUT_TIME;

    [SerializeField] bool _delayStartTransitionIn = false;
    [SerializeField, ShowIf("_delayStartTransitionIn")] float _startTransitionInDelay;

    [Header("Components")]
    [SerializeField] RectTransform _transitionTransform;

    #region Get Helpers
    Vector3 GetHiddenLocalPosition()
    {
        Vector3 result = Vector3.zero;
        float width = _transitionTransform.rect.width * 1.2f; // Add extra padding
        float height = _transitionTransform.rect.height;

        switch (_slideInDirection)
        {
            case SlideDirection.Left:
                result.x += width;
                break;

            case SlideDirection.Right:
                result.x -= width;
                break;

            case SlideDirection.Up:
                result.y -= height;
                break;

            case SlideDirection.Down:
                result.y += height;
                break;
        }

        return result;
    }
    #endregion

    IEnumerator DoSlideAnim(bool show, float duration, System.Action onCompleteCb = null)
    {
        _transitionTransform.DOKill();
        Vector3 showPosition = Vector3.zero;
        Vector3 hiddenPosition = GetHiddenLocalPosition();

        if (show && _delayStartTransitionIn)
            yield return new WaitForSecondsRealtime(_startTransitionInDelay);

        Vector3 startPos = show ? hiddenPosition : showPosition;
        Vector3 endPos = show ? showPosition : hiddenPosition;
        _transitionTransform.localPosition = startPos;
        _transitionTransform.DOLocalMove(endPos, duration).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration);

        onCompleteCb?.Invoke();
        yield break;
    }

    IEnumerator DoPopAnimation(bool show, float duration, System.Action onCompleteCb = null)
    {
        Vector3 startScale = show ? Vector3.zero : Vector3.one;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;
        float popAmount = 1.15f;
        Vector3 popToScale = new Vector3(popAmount, popAmount, popAmount);

        _transitionTransform.localScale = startScale;

        if (show)
        {
            if (_delayStartTransitionIn)
                yield return new WaitForSecondsRealtime(_startTransitionInDelay);

            float popDuration = duration * 0.8f;
            float finishDuration = duration - popDuration;

            _transitionTransform.DOScale(popToScale, popDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(popDuration);
            _transitionTransform.DOScale(endScale, finishDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(finishDuration);
        } else
        {
            // Scale out, no pop
            _transitionTransform.DOScale(endScale, duration).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(duration);
        onCompleteCb?.Invoke();
        yield break;
    }

    public void DoTransition(bool show, float duration, bool handleObjectToggle = false)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (_overrideDuration)
            duration = show ? _customInDuration : _customOutDuration;

        if (handleObjectToggle && show)
            _transitionTransform.gameObject.SetActive(true);

        System.Action onCompleteCb = null;

        if (handleObjectToggle && !show)
            onCompleteCb = () => _transitionTransform.gameObject.SetActive(false);

        switch (_transitionType)
        {
            case TransitionType.NONE:
                return;

            case TransitionType.Slide:
                StartCoroutine(DoSlideAnim(show, duration, onCompleteCb));
                break;

            case TransitionType.PopScale:
                StartCoroutine(DoPopAnimation(show, duration, onCompleteCb));
                break;

            default:
                Debug.LogWarning($"Transition type not handled: {_transitionType}");
                return;
        }
    }

    public void JumpToHiddenState()
    {
        switch (_transitionType)
        {
            case TransitionType.Slide:
                _transitionTransform.localPosition = GetHiddenLocalPosition();
                break;

            case TransitionType.PopScale:
                _transitionTransform.localScale = Vector3.zero;
                break;
        }
    }
}
