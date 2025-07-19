using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CommandPage : MonoBehaviour
{
    public TMP_InputField input;
    public static Dictionary<string, Commanded> commands = new();
    public void Starte()
    {
        Ct.command = this;
        commands.Add("summon", tree =>//val => type position(0,0,0)
        {
            //[0]type, [1]position
            if (!Actor.aTy.Contains(tree[0]))
                return;
            Vector3 p = tree.Count >= 2 ? SMath.V3.Parse(tree[1]) : Ct.ppw;
            Actor.Load(tree[0], p);
            NoteManager.Load($"summon at {p}, the {tree[0]}");
        });
        commands.Add("load", (List<string> tree) =>
        {//[0]name/index, [1] position
            string n = int.TryParse(tree[0], out int ind) ? Obj.oTy[ind] : tree[0];
            Vector3 p = tree.Count >= 2 ? SMath.V3.Parse(tree[1]) : Ct.ppw;
            Obj.Load(n, p);
            NoteManager.Load($"load at {p} the {n}");
        });
        commands.Add("give", (List<string> tree) =>
        {//0entity, 1item, 2amount
            Inventory inv = null;
            string n = tree[0];
            if (n == "self")
                inv = Ct.curWd.inventory;

            if (inv == null)
                return;
            else
            {
                string item = tree[1];
                int amt = int.Parse(tree[2]);
                inv.Add(item, amt, out int full);
                if (full > 0)
                    Drops.Load(item, amt, Ct.ppw);
                NoteManager.Load($"give at {n} {amt} {item}");
            }
        });
        commands.Add("text", (List<string> tree) =>//0.key, 1.langue
        {
            string text = TextManager.Read(tree[1], tree[0]);
            NoteManager.Load(text);
            Debug.Log(text);
        });

        Ct.act.Main.enter.performed += c =>
        {
            string com = input.text;
            input.text = "";
            Command(com);
        };
    }
    public void Command(string command)
    {
        if (command == "")
            return;
        try
        {
            List<string> list = command.Split(' ').ToList();
            string c = list[0];
            if (!commands.ContainsKey(c))
                return;
            list.RemoveAt(0);
            commands[c]?.Invoke(list);
        }
        catch (Exception x)
        {
            Debug.LogError(x.Message);
        }
    }
    public void CommandByPath(string path)
    {
        if (!Data.FileExists(path))
            return;

        string lines = Data.LoadFile(path);
        List<string> commands = lines.Split('\n').ToList();
        foreach (var item in commands)
        {
            Command(item.TrimEnd('\n'));
        }
    }
    public delegate void Commanded(List<string> tree);
}

//summon dealeer