using System.Collections;
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
        Ct.ct.indicatorstick.OnInteractTap += () =>
        {
            var os = Ct.mcanvas.GetUIObjectsAt(Ct.ct.indicator.position);
            foreach (var v in os)
            {
                GameObject o = v.gameObject;
                if (o == null)
                    LeftTap();
                else
                {
                    Debug.Log(o.name);
                    if (o.GetComponent<SButton>() != null)
                    {
                        o.GetComponent<SButton>().onClick.Invoke();
                    }
                }
            }
        };
        Ct.ct.indicatorstick.OnInteractHold += () => RightTap();

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
            if (Ct.curWd.maxDistanceOfInteraction < Ct.dmp)
                return;
            if (left)
            {
                LeftTap();
            }
            else
            {
                RightTap();
            }
        }
        else if (cxt.interaction is HoldInteraction)
        {
            if (left)
            {
                LeftHold();
            }
            else
            {
                RiightHold();
            }
        }
    }


    public void LeftTap()
    {
        ItemData d = Item.GetData(Ct.mouseSelected.select.item);
        this.left = State.tap;
        Item.Interact(Item.InteractionType.lefttap, d);
    }
    public void RightTap()
    {
        ItemData d = Item.GetData(Ct.mouseSelected.select.item);
        right = State.tap;
        Item.Interact(Item.InteractionType.righttap, d);
        if (Ct.mouseSelected.select.item == "n" || d.itc[1] == null)
        {
            if (Ct.ct.casted.CompareTag("entity"))
            {
                Entity en = Ct.ct.casted.GetComponent<Entity>();
                Entity.interactions[en.name]?.Invoke(en);
            }
            else if (Ct.ct.casted.CompareTag("actor"))
            {
                var act = Ct.ct.casted.GetComponent<Actor>();
                Actor.interactions[act.type]?.Invoke(act.dat, act);
            }
            else if (Ct.ct.casted.CompareTag("Object"))
            {
                Obj o = Ct.ct.casted.GetComponent<Obj>();
                ObjData obj = Obj.data[o.index];
                if (obj.itc != null)
                    Obj.interactions[obj.itc]?.Invoke(obj, o);
            }
        }
    }
    public void LeftHold()
    {
        ItemData d = Item.GetData(Ct.mouseSelected.select.item);
        this.left = State.hold;
        if (Ct.curWd.maxDistanceOfInteraction < Ct.dmp)
        {
            Item.Interact(Item.InteractionType.leftpress, d);
        }
        breakCor = Ct.ct.CT(Breaking());
    }
    public void RiightHold()
    {
        ItemData d = Item.GetData(Ct.mouseSelected.select.item);
        if (Ct.curWd.maxDistanceOfInteraction < Ct.dmp)
        {

            right = State.hold;
            Item.Interact(Item.InteractionType.rightpress, d);
        }
    }


    public Coroutine breakCor;
    public IEnumerator Breaking()
    {
        float curMar = 0.5f;
        GameObject o = null;
        Material m = null;
        ObjData d = null;


        o = Ct.ct.casted;
        if (o.CompareTag("Object"))
        {
            d = Obj.data[o.GetComponent<Obj>().index];
            m = o.GetComponentInChildren<SpriteRenderer>().material;
            curMar = 0.5f;
        }

        while (left != State.relase)
        {
            yield return null;
            if (Ct.curWd.maxDistanceOfInteraction >= Ct.dmp)
            {
                if (o == Ct.ct.casted && o != null && o.CompareTag("Object"))
                {
                    Ct.ct.indicator.Indicate = "breaking";
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
                    o = Ct.ct.casted;
                    if (o != null && o.CompareTag("Object"))
                    {
                        d = Obj.data[o.GetComponent<Obj>().index];
                        m = o.GetComponentInChildren<SpriteRenderer>().material;
                        curMar = 0.5f;
                    }
                }
                yield return null;
            }
        }
        if (o != null && o.CompareTag("Object"))
        {
            m.SetFloat("_lineWidth", 0.5f);
        }

        Ct.ct.indicator.Indicate = "";
    }

    public enum State
    {
        tap,
        hold,
        relase
    }
}
