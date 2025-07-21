using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SButton : Button
{
    [Header("Pointer Events")]
    public UnityEvent OnButtonDown;
    public UnityEvent OnButtonUp;
    public override void OnSubmit(BaseEventData eventData)
    {
        // ÆÁ±Î Enter
        // ²»µ÷ÓÃ base.OnSubmit(eventData);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        Debug.Log("down"); 
        OnButtonDown.Invoke();
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        OnButtonUp.Invoke();
    }

    public void BreakOn()
    {
        Interactions.itc.left = Interactions.State.hold;
        Interactions.itc.breakCor = Ct.ct.CT(Interactions.itc.Breaking());
    }
    public void BreakOff()
    {
        Interactions.itc.left = Interactions.State.relase;
    }
    public void EnterInv()
    {
        Ct.ct.invp.ChangeToInvPage();
    }
}
