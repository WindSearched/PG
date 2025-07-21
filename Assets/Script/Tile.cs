using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public MeshRenderer mesh;
    public string type;

    private void Start()
    {
        mesh = GetComponent<MeshRenderer>();

        InitMesh();
    }
    public void InitMesh()
    {
        int i = 5;
        while (i-- > 0)
        {
            mesh.materials[i] = Ct.ct.blendTile;
        }

        var tex = GetData(type);
        AddTexture(TileFace.down, tex.down, 0);
        AddTexture(TileFace.up, tex.up, 0);
        AddTexture(TileFace.right, tex.right, 0);
        AddTexture(TileFace.right, tex.right, 0);
        AddTexture(TileFace.front, tex.front, 0);
    }
    /// <summary>
    /// fixed a texture of material; if cancel the texture,just set null
    /// </summary>
    /// <param name="tileFace">face of tile</param>
    /// <param name="tex">fix tex</param>
    /// <param name="index">max is 8 </param>
    public void AddTexture(TileFace tileFace, Texture2D tex, int index)
    {
        mesh.materials[(int)tileFace].SetTexture("Tex" + index, tex);
    }

    public static Dictionary<string, TileTexture> tileTextures = new();
    /// <summary>
    /// add a tile texture to the list
    /// </summary>
    /// <param name="name"></param>
    /// <param name="tileTexture"></param>
    public static void Add(string name, TileTexture tileTexture)
    {
        if (!tileTextures.ContainsKey(name))
            tileTextures.Add(name, tileTexture);
        else
            Debug.LogWarning("TileTexture " + name + " already exists!");
    }
    public static TileTexture GetData(string name)
    {
        if (tileTextures.ContainsKey(name))
        {
            return tileTextures[name];
        }
        else
            return null;
    }

    public static void Load(string type, Vector3 position)
    {
        if (tileTextures.ContainsKey(type)) return;
        var o = Instantiate(Ct.ct.tilePrefab, Ct.ct.tilesParent);
        o.transform.position = position;    
        var t = o.GetComponent<Tile>();
        t.type = type;
    }

    public enum TileFace
    {
        down, up, left, right, front
    }
}
public class TileTexture
{
    public Texture2D right, up, left, down, front;
    public Texture2D edgRight, edgUp, edgLeft, edgDown, edgRightUp, edgUpLeft, edgLeftDown, edgDownRight;

    /// <summary>
    /// based redner
    /// </summary>
    /// <param name="mesh"></param>
    public void LoadTo(MeshRenderer mesh)
    {
        mesh.materials[0].mainTexture = down;
        mesh.materials[1].mainTexture = up;
        mesh.materials[2].mainTexture = left;
        mesh.materials[3].mainTexture = right;
        mesh.materials[4].mainTexture = front;
    }

    [Serializable]
    public class TData
    {
        public bool sameWalls = true;
        public int power = 1;
        public TRect sameWallsPos = new(64, 0, 32, 32);

        public TRect right = new(96, 32, 32, 32), up = new(64, 32, 32, 32), left = new(64, 0, 32, 32), down = new(96, 0, 32, 32), front = new(16, 16, 32, 32);
        public TRect edgLeft = new(0, 16, 16), edgUp = new(16, 48, height: 16), edgRight = new(48, 16, 16), edgDown = new(16, 0, height: 16), 
            edgRightUp = new(48,48,16,16), edgUpLeft = new(0,48,16,16), edgLeftDown = new(0,0,16,16), edgDownRight = new(48,0,16,16);

        public string path;

        public TileTexture Load()
        {
            var origin = Mod.LoadTexture(path);
            TileTexture ttex = new();
            ttex.front = front.GetTexture(origin);
            if (sameWalls)
            {
                ttex.right = ttex.up = ttex.left = ttex.down = sameWallsPos.GetTexture(origin, power);
            }
            else
            {
                ttex.right = right.GetTexture(origin, power);
                ttex.up = up.GetTexture(origin, power);
                ttex.left = left.GetTexture(origin, power);
                ttex.down = down.GetTexture(origin, power);
            }
            ttex.edgRight = SMath.Px.Fill(edgRight.GetTexture(origin, power),new(16,0,16));
            ttex.edgUp = SMath.Px.Fill(edgUp.GetTexture(origin, power), new(0, 16, height:16));
            ttex.edgLeft = SMath.Px.Fill(edgLeft.GetTexture(origin, power), new(-16, 0, 16));
            ttex.edgDown = SMath.Px.Fill(edgDown.GetTexture(origin, power), new(0, -16, height: 16));

            ttex.edgRightUp = SMath.Px.Fill(edgRightUp.GetTexture(origin, power), new(0,0));
            ttex.edgUpLeft = SMath.Px.Fill(edgUpLeft.GetTexture(origin, power), new(-16, 0));
            ttex.edgLeftDown = SMath.Px.Fill(edgLeftDown.GetTexture(origin, power), new(-16, -16));
            ttex.edgDownRight = SMath.Px.Fill(edgDownRight.GetTexture(origin, power), new(0, -16));

            return ttex;
        }
    }
}
public struct TRect
{
    public int x, y;
    public int width, height;
    public Texture2D GetTexture(Texture2D origin, int power = 1)
    {
        return SMath.Px.GetSubTexture(origin, x, y, width, height);
    }
    public TRect(int x, int y, int width = 32, int height = 32)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
}