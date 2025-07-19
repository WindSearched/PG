using System;
using System.Collections.Generic;
using UnityEngine;

public static class Page
{
    /// <summary>
    /// when enter tha page
    /// </summary>
    public static Dictionary<string, Action> enters = new();
    /// <summary>
    /// when exit the page
    /// </summary>
    public static Dictionary<string, Action> exits = new();
    public static string curPage;

    public static void ChangePage(string page)
    {
        if (page == curPage)
            return;
        exits[curPage]?.Invoke();
        curPage = page;
        enters[page]?.Invoke();
        Debug.Log("[Page.ChangePage] changed to : " + page);
    }
    public static bool IsPage(string page)
    {
        return curPage == page;
    }
    public static void Add(string page, Action enter, Action exit)
    {
        enters.Add(page, enter);
        exits.Add(page, exit);
    }
}
