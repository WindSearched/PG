using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public bool craft = false;
    public GameObject ao;
    public void SStart()
    {
        for (int i = 0; i < gridsParent.childCount; i++)
        {
            grids.Add(gridsParent.GetChild(i).gameObject);
        }

        Ct.curWd.inventory.WhenInvChange += WhenUpdate;
        Ct.curWd.inventory.Invchange();

        Ct.act.Main.tab.performed += c =>
        {
            ChangeToInvPage();
        };
        Ct.act.Main.shift.started += c =>
        {
            Mode(true);
        };
        Ct.act.Main.shift.canceled += c =>
        {
            Mode(false);
        };
    }
    public void ChangeToInvPage()
    {
        string inv = "inventory";

        if (Page.IsPage(inv))
        {
            Craft();
        }
        else
        {
            if (Page.IsPage("main") && Ct.mouseSelected.select.item != "n")
            {
                Ct.curWd.inventory.Add(Ct.mouseSelected.select, out int full);
                Ct.mouseSelected.Remove();
                if (full == 0)
                    Ct.mouseSelected.select = new();
                else
                    Page.ChangePage(inv);
            }
            else
                Page.ChangePage(inv);
        }
    }
    public void Binding()
    {
        string inv = "inventory";
        Page.Add(inv, () =>
        {
            gameObject.SetActive(true);
#if UNITY_ANDROID
            Ct.ct.joystick.gameObject.SetActive(false);
#endif
        }, () =>
        {
            gameObject.SetActive(false);
#if UNITY_ANDROID
            Ct.ct.joystick.gameObject.SetActive(true);
#endif
        });
        gameObject.SetActive(false);
    }
    public void WhenUpdate(Inventory inv)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            Inventory.Grid g = Ct.curWd.inventory.GetGrid(i);
            grids[i].transform.GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(g.item);
            grids[i].GetComponentInChildren<TextMeshProUGUI>().text = g.amt.ToString();

            var id = Item.GetData(g?.item).tool;
            var im = grids[i].transform.GetChild(2).GetComponent<Image>();
            float fill = 0;
            if (id != null)
                fill = (float)g.durab / id.durability;
            im.fillAmount = fill;
            if (i < fastInvs.childCount)
            {
                fastInvs.GetChild(i).GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(g.item);
                fastInvs.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = g.amt.ToString();
                fastInvs.GetChild(i).GetChild(2).GetComponent<Image>().fillAmount = fill;
            }
        }
    }
    /// <summary>
    /// chest only
    /// </summary>
    /// <param name="inv"></param>
    public static void OnUpdate(Inventory inv)
    {
        for (int i = 0; i < Ct.ct.chestView.childCount; i++)
        {
            Transform grid = Ct.ct.chestView.GetChild(i);
            Inventory.Grid g = inv.GetGrid(i);
            grid.GetChild(0).GetComponent<Image>().sprite = Item.GetSprite(g.item);
            grid.GetChild(1).GetComponent<TextMeshProUGUI>().text = g.amt.ToString();

            var id = Item.GetData(g.item).tool;
            var im = grid.GetChild(2).GetComponent<Image>();
            if (id != null)
                im.fillAmount = g.durab / id.durability;
            else
                im.fillAmount = 0;
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

        if (craft)
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
    private void OnDisable()
    {
        APointed();
    }

    public void Pointed(GameObject obj)
    {
        ItemData data = Item.GetData(Ct.curWd.inventory.GetGrid(int.Parse(obj.name)).item);
        Ct.ct.PointedName = TextManager.Read(true, true, data.name);
        Ct.ct.PointedDescription = TextManager.Read(true, false, data.name);
    }
    public void APointed()
    {
        Ct.ct.PointedName = "";
        Ct.ct.PointedDescription = "";
    }

    public void Mode(bool isCraft)
    {
        craft = isCraft;
        if (craft)
            ao.GetComponentInChildren<TextMeshProUGUI>().text = "in";
        else
            ao.GetComponentInChildren<TextMeshProUGUI>().text = "out";
    }
    public void Craft()
    {
        if (list.Count == 0)
            Page.ChangePage("main");
        else
        {
            Crafting.Craft(list, out bool craft);
            if (craft)
                Debug.Log("[InventotyPage]Crafting completed");
            for (int i = 0; i < craftlist.childCount; i++)
                Destroy(craftlist.GetChild(i).gameObject);
            list.Clear();
        }
    }
    public void Mode() => Mode(!craft);

}