using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class AModal : MonoBehaviour
{
    public const float TRANSITION_IN_TIME = 0.3f;
    public const float TRANSITION_OUT_TIME = 0.2f;

    [Header("Modal Content (Required)")]
    [SerializeField, InfoBox("Modal object not set", "ModalObjectIsValid", InfoMessageType = InfoMessageType.Error)] GameObject _modalObject;
    [SerializeField] List<UI_TransitionableComponent> _transitionObjects = new List<UI_TransitionableComponent>();

    [Header("Modal Settings")]
    [SerializeField] bool _autoHideOnAwake = true;

    bool _shouldInitialize = true;
    System.Action _onTransitionOutCb = null;

    public bool IsShowing { get; private set; } // Set manually, as _modalObject itself could be between transition states
    public GameObject ModalObject { get { return _modalObject; } }

#if UNITY_EDITOR
    #region Editor
    bool ModalObjectIsValid()
    {
        return _modalObject != null;
    }
    #endregion
#endif

    #region MonoBehaviour
    void Awake()
    {
        Debug.Assert(_modalObject != null, $"Modal object not set on [{name}]");

        if (_autoHideOnAwake)
            _modalObject.SetActive(false);

        OnAwake();
    }
    #endregion

    #region Base Functions
    // Do transition animations
    public virtual IEnumerator TransitionIn()
    {
        yield return StartCoroutine(DoTransitionAnimations(true));
        OnTransitionIn();
        yield break;
    }

    // Do transition animations
    public virtual IEnumerator TransitionOut()
    {
        yield return StartCoroutine(DoTransitionAnimations(false));
        OnTransitionOut();
        yield break;
    }

    public virtual void OnAwake()
    {

    }

    // Master display on / off function. Runs full transition sequence.
    public virtual void DisplayModal(bool show, bool doInitialize = true, System.Action onTransitionOutCb = null)
    {
        if (!gameObject.activeInHierarchy) // Silently toggle display if inactive in hierarchy
        {
            ToggleContainer(show);
            return;
        }

        if ((IsShowing && show) || (!IsShowing && !show)) // Don't double up on animations
            return;

        IsShowing = show;
        _onTransitionOutCb = onTransitionOutCb;
        _shouldInitialize = doInitialize;

        if (show)
        {
            _modalObject.SetActive(true);
            Debug.Assert(_modalObject.activeSelf);
            Debug.Assert(_modalObject.activeInHierarchy);
             StartCoroutine(TransitionIn());
        }
        else
            StartCoroutine(TransitionOut());
    }

    // On animation complete cb
    public virtual void OnTransitionIn()
    {
        if (_shouldInitialize)
            InitializeModal();
    }

    // Run main init
    public virtual void InitializeModal()
    {

    }

    // On animation complete cb
    public virtual void OnTransitionOut()
    {
        _modalObject.SetActive(false);
        _onTransitionOutCb?.Invoke();
        _onTransitionOutCb = null;
    }
    #endregion

    #region Additional Functionality
    IEnumerator DoTransitionAnimations(bool show)
    {
        if (_transitionObjects.Count == 0)
            yield break;

        if (show)
            HideTransitionItems();

        float duration = show ? TRANSITION_IN_TIME : TRANSITION_OUT_TIME;
        float durationPer = duration / Mathf.Max(1, _transitionObjects.Count);

        for (int i = 0; i < _transitionObjects.Count; i++)
        {
            UI_TransitionableComponent tc = _transitionObjects[i];
            if (tc == null)
                continue;

            tc.DoTransition(show, durationPer);
            yield return new WaitForSecondsRealtime(durationPer / 2f);
        }

        yield break;
    }

    void HideTransitionItems()
    {
        for (int i = 0; i < _transitionObjects.Count; i++)
        {
            UI_TransitionableComponent tc = _transitionObjects[i];
            if (tc == null)
                continue;

            tc.JumpToHiddenState();
        }
    }

    // Show / Hide without triggering transitions
    public void ToggleContainer(bool show)
    {
        _modalObject.SetActive(show);
    }

    public void MaybeToggleClickActions(bool block)
    {
        switch (GameController.GetCurrentPhase())
        {
            case GamePhase.Battle:
                break;

            default:
                StagingUIController.BlockClickActions(block);
                break;
        }
    }
    #endregion
}
