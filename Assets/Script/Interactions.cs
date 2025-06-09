using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class Interactions : MonoBehaviour
{
    public static float mar = 50;
    public static float maxMarV = 9.5f;

    public State left, right;
    private void Start()
    {
        Ct.act.itc.rightM.performed +=
            c => MouseInt(c, false);


        Ct.act.itc.leftM.performed +=
            c => MouseInt(c, true);
        Ct.act.itc.rightM.canceled += c => right = State.relase;
        Ct.act.itc.leftM.canceled += c => left = State.relase;
    }




    public void MouseInt(InputAction.CallbackContext cxt, bool left)
    {
        if (Ct.ct.specifyInter || !Page.IsPage("main"))
            return;

        ItemData d = Item.GetData(Ct.mouseSelected.select.item);
        if (cxt.interaction is TapInteraction)
        {
            if (left)
            {
                this.left = State.tap;
                Item.Interact(Item.InteractionType.lefttap, d);
            }
            else
            {
                right = State.tap;
                Item.Interact(Item.InteractionType.righttap, d);
                if (Ct.mouseSelected.select.item == "n" || d.itc[1] == null)
                {
                    if (Ct.ct.ray.CompareTag("entity"))
                    {
                        Entity en = Ct.ct.ray.GetComponent<Entity>();
                        Entity.interactions[en.name]?.Invoke(en);
                    }
                    else if (Ct.ct.ray.CompareTag("Object"))
                    {
                        Obj o = Ct.ct.ray.GetComponent<Obj>();
                        ObjData obj = Obj.data[o.index];
                        if (obj.itc != null)
                            Obj.interactions[obj.itc]?.Invoke(obj, o);
                    }
                }
            }
        }
        else if (cxt.interaction is HoldInteraction)
        {
            if (left)
            {
                this.left = State.hold;
                Item.Interact(Item.InteractionType.leftpress, d);
                breakCor = Ct.ct.CT(Breaking());
            }
            else
            {
                right = State.hold;
                Item.Interact(Item.InteractionType.rightpress, d);
            }
        }
    }
    public Coroutine breakCor;
    public IEnumerator Breaking()
    {
        float curMar = 0.5f;
        GameObject o = null;
        Material m = null;
        ObjData d = null;

        o = Ct.ct.ray;
        if (o.CompareTag("Object"))
        {
            d = Obj.data[o.GetComponent<Obj>().index];
            m = o.GetComponentInChildren<SpriteRenderer>().material;
            curMar = 0.5f;
        }

        while (left != State.relase)
        {
            if (o == Ct.ct.ray && o!= null && o.CompareTag("Object"))
            {
                float remMar = mar / d.breaking.hardness * Time.deltaTime;
                curMar += remMar;

                m.SetFloat("_lineWidth", curMar);
                if (curMar >= 10)
                {
                    Obj.GetData(o.GetComponent<Obj>().ld.name).GetDrops(o.transform.position);
                    Destroy(o);
                    yield return new WaitForEndOfFrame();
                    o = null;
                    continue;
                }
            }
            else
            {
                o = Ct.ct.ray;
                if (o != null && o.CompareTag("Object"))
                {
                    d = Obj.data[o.GetComponent<Obj>().index];
                    m = o.GetComponentInChildren<SpriteRenderer>().material;
                    curMar = 0.5f;
                }
            }
            yield return null;
        }
        if(o != null && o.CompareTag("Object"))
        {
            m.SetFloat("_lineWidth", 0.5f);
        }
    }

    public enum State
    {
        tap,
        hold,
        relase
    }
}
