using UnityEngine;

public class PreloadObj : Obj
{
    public ItemData itd;
    private bool init = true;
    public override void Start()
    {
        if (init)
        {
            init = false;
            Ct.po = this;
            ld.name = oTy[0];
            spriteR.color = Ct.set.objPlaceable;
            base.Start();
            Active(false);
            return;
        }
        index = oTy.IndexOf(ld.name);
        base.Start();

        if (itd.placeable == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ld.name = itd.name;



    }
    private void Update()
    {
        transform.position = Ct.wmp;
    }
    public bool Placeable
    {
        get => canPlace;
        set
        {
            canPlace = value;
            if (value)
                spriteR.color = Ct.set.objPlaceable;
            else
                spriteR.color = Ct.set.objCannotPlace;
        }
    }
    private bool canPlace = true;

    public void Active(bool active = true)
    {
        if (active)
        {
            gameObject.SetActive(active);
            itd = Item.GetData(Ct.mouseSelected.select.item);
            spriteR.color = itd.placeable.condition == null ? Ct.set.objPlaceable : Ct.set.objCannotPlace;
            Start();
        }
        else
            gameObject.SetActive(active);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Object"))
        {
            return;
        }
        ObjData od = other.gameObject.GetComponent<Obj>().dt;
        Placeable = itd.placeable.condition == od.name;

        Ct.ct.inTrigger = true;
        Ct.ct.ray = other.gameObject;
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Object"))
        {
            return;
        }
        ObjData od = other.gameObject.GetComponent<Obj>().dt;
        Placeable = itd.placeable.condition == null;

        Ct.ct.inTrigger = false;
        Ct.ct.ray = null;
    }
    private void OnDisable()
    {
        Ct.ct.inTrigger = false;
    }
}
