using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MouseSelect : MonoBehaviour
{
    public RectTransform rt;
    public Inventory.Grid select;
    private void Start()
    {
        select = new();
        Ct.mouseSelected = this;

        rt = GetComponent<RectTransform>();
        Ct.AddScalable(rt);
    }
    public void WhenSwitch()
    {
        GetComponent<Image>().sprite = Item.GetSprite(select.item);
        if(select.amt == 0)
            transform.GetComponentInChildren<TextMeshProUGUI>().text = "";
        else
            transform.GetComponentInChildren<TextMeshProUGUI>().text = select.amt.ToString();

        ItemData d = Item.GetData(select.item);
        if (d.placeable == null)
        {
            Ct.po.Active(false);
        }
        else
        {
            Ct.po.ld.name = d.placeable.placed;
            Ct.po.Active();
        }
        AttackView();
    }
    public void AttackView()
    {
        ItemData id = Item.GetData(select.item);
        if (id.IsTool() && id.tool.arm)
        {
            if(Ct.attackViewer.positionCount == 0)
            {
                Ct.ct.CT(Attackviewer());
                return;
            }
        }
        Ct.attackViewer.positionCount = 0;
        Ct.attackingMode = false;
    }
    public IEnumerator Attackviewer()
    {
        ItemData id = Item.GetData(select.item);
        Ct.attackingMode = true;

        while (Ct.attackingMode)
        {
            Vector3[] a = id.AttackViewer();
            Ct.attackViewer.positionCount = a.Length;
            Ct.attackViewer.SetPositions(a);
            yield return null;
        }
    }
    public void Fray(int frayed)
    {
        select.Fray(1);
        if(select.durab <= 0)
            WhenSwitch();
    }
    public void Remove(int removed)
    {
        select.Add(-removed, out int full);
        if(full != 0 || select.amt <= 0)
            WhenSwitch();
    }

    public void Remove()
    {
        select = new();
        WhenSwitch();
    }
}
