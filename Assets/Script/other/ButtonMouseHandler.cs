using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class ButtonMouseHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static List<ButtonMouseHandler> MouseHandlers = new();

    [Header("Pointer Events")]
    public UnityEvent onPointerEnter;
    public UnityEvent onPointerExit;

    private RectTransform rectTransform;
    private bool isPointerInside = false;

    private Vector2 pointerPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        MouseHandlers.Add(this);
    }

    private void OnEnable()
    {
        // ×¢²áÊäÈëÊÂ¼þ
        InputSystemUIInputModule inputModule = FindObjectOfType<InputSystemUIInputModule>();

        if (inputModule != null)
        {
            inputModule.point.action.performed += OnPointerMove;
        }
    }

    private void OnDisable()
    {
        InputSystemUIInputModule inputModule = FindObjectOfType<InputSystemUIInputModule>();

        if (inputModule != null)
        {
            inputModule.point.action.performed -= OnPointerMove;
        }
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = Ct.ct.indicator.position;
        CheckPointer(pointerPosition);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit?.Invoke();
    }


    public void CheckPointer(Vector2 screenPosition)
    {
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);

        if (isInside && !isPointerInside)
        {
            onPointerEnter?.Invoke();
            isPointerInside = true;
        }
        else if (!isInside && isPointerInside)
        {
            onPointerExit?.Invoke();
            isPointerInside = false;
        }
    }
}