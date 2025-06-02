using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryPage : MonoBehaviour
{
    List<GameObject> grids = new();
    public Transform gridsParent;
    public Transform fastInvs;
    public GameObject craftgrid;
    public Transform craftlist;
    public List<string> list = new();
    public static bool crafting = false;
    private void Start()
    {
        for (int i = 0; i < gridsParent.childCount; i++)
        {
            grids.Add(gridsParent.GetChild(i).gameObject);
        }
        Ct.AddScalable(gridsParent.GetComponent<RectTransform>());
        Ct.AddScalable(transform.Find("crafted").GetComponent<RectTransform>());


        Ct.curWd.inventory.WhenInvChange += WhenUpdate;
        Ct.curWd.inventory.Invchange();

    }
    public void Binding()
    {
        string inv = "inventory";
        Page.Add(inv, () => gameObject.SetActive(true), () => gameObject.SetActive(false));
        gameObject.SetActive(false);
    }
    public void WhenUpdate(Inventory inv)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            Inventory.Grid g = Ct.curWd.inventory.GetGrid(i);
            grids[i].transform.GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(g.item);
            grids[i].GetComponentInChildren<TextMeshProUGUI>().text = g.amt.ToString();
            if(i < fastInvs.childCount)
            {
                fastInvs.GetChild(i).GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(g.item);
                fastInvs.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = g.amt.ToString();
            }
        }
    }
    public static void OnUpdate(Inventory inv)
    {
        for(int i = 0; i < Ct.ct.chestView.childCount; i++)
        {
            Transform grid = Ct.ct.chestView.GetChild(i);
            Inventory.Grid g = inv.GetGrid(i);
            grid.GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(g.item);
            grid.GetChild(1).GetComponent<TextMeshProUGUI>().text = g.amt.ToString();
        }
    }
    /// <summary>
    /// when button enter 
    /// </summary>
    /// <param name="o"></param>
    public void AddToList(GameObject o)
    {
        int index = int.Parse(o.name);
        string item = Ct.curWd.inventory.GetGrid(index).item;

        if (Ct.shiftPressing)
        {
            if (!Item.IsTool(item))
            {
                Ct.curWd.inventory.Add(index, -1, out int full);
                if (full < 0)
                    return;
            }

            GameObject g = Instantiate(craftgrid, craftlist);
            RectTransform r = g.GetComponent<RectTransform>();
            r.anchoredPosition = new(0, -49 * list.Count);
            g.transform.GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(item);

            list.Add(item);
        }
        else
        {
            Ct.curWd.inventory.Switch(index, Ct.mouseSelected.select, out Inventory.Grid g);
            Ct.mouseSelected.select = g;
            Ct.mouseSelected.WhenSwitch();
        }
    }
    public void FastInv(GameObject o)
    {
        if (!Page.IsPage("main"))
            return;

        int index = int.Parse(o.name);

        Ct.curWd.inventory.Switch(index, Ct.mouseSelected.select, out Inventory.Grid g);
        Ct.mouseSelected.select = g;
        Ct.mouseSelected.WhenSwitch();
    }
}
