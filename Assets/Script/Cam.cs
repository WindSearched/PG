using IPGModAPI;
using System;
using UnityEngine;

public class Cam : MonoBehaviour
{
    public Vector3 cen = new();
    public Vector3 to = new();
    public float follwingSpeed;
    
    public float time = 0;
    public float preangle;
    public float prelen;
    GameObject ply;
    private void Start()
    {
        Ct.act.Main.q.started +=
            c =>
            {
                Ct.visionRotate = true;
                Ct.ct.specifyInter = true;
                Ct.evn.WhenVisionRotating += VisionRotating;
            };//if the key q down, detect the rotation
        Ct.act.Main.q.canceled +=
            c =>
            {
                Ct.visionRotate = false;
                Ct.ct.specifyInter = false;
                Ct.evn.WhenVisionRotating -= VisionRotating;
            };//if key up, cancle detection
        Ct.act.Main.e.started +=
            c =>
            {
                Ct.visionElevate = true;
                Ct.ct.specifyInter = true;
                Ct.evn.WhenVisionElevate += VisionElevate;
            };
        Ct.act.Main.e.canceled +=
           c =>
           {
               Ct.visionElevate = false;
               Ct.ct.specifyInter = false;
               Ct.evn.WhenVisionElevate -= VisionElevate;
           };

        ply = Ct.ct.player;
        cen = ply.transform.position;

        CamPosition();
        //develop part
        //


        Ct.dePa.Regist(9, () => Ct.ct.casted, "rayed");

        WorldGenerator.generators.Add(WorldGenerator.Bioming);
        Ct.evn.IWhenPlayerMoving();

        Ct.dePa.Regist(0, () => Ct.curWd.camAngle, "angle");

        Ct.dePa.Regist(2, () => Ct.cp, "chunk");
        Ct.dePa.Regist(3, () => Ct.ppw, "ppw");
        Ct.dePa.Regist(4, () => Ct.wmp, "moouse projection");
        Ct.dePa.Regist(5, () => Page.curPage, "current page");
        Ct.dePa.Regist(7, () => Ct.ct.joystick.InputVector, "joystick");
    }
    private void Update()
    {
        cen = ply.transform.position;


        //Following();
    }

    /// <summary>
    /// detecte the changing of rotation, when down key(q)
    /// </summary>
    public void VisionRotating()
    {
        if (Ct.ct.ocped == MouseState.hold)
        {
            FreeViosionRotate();
        }
        else if (Ct.ct.ocped == MouseState.tap2)
        {
            SmoothVisionRotate(out bool finished);
            if (finished)
            {
                Ct.ct.ChangeState(MouseState.relased);
            }
        }
        CamPosition();
    }
    public void VisionElevate()
    {
        if (Ct.ct.ocped == MouseState.hold)
        {
            FreeVisionElevate();
        }
        else if (Ct.ct.ocped == MouseState.tap2)
        {
            SmoothVisionElevate(out bool finished);
            if(finished)
            {
                Ct.ct.ChangeState(MouseState.relased);
            }
        }
        CamPosition();
    }
    public void FreeViosionRotate(float rotateAngle = 80)
    {
        Ct.curWd.camAngle += -rotateAngle * Time.deltaTime * Ct.ct.toward;
    }
    public void SmoothVisionRotate(out bool finished, float rotateAngle = 90f, float maxTime = 0.5f)
    {
        if (time == 0)
            preangle = Ct.curWd.camAngle;

        time += Time.deltaTime;
        if (time >= maxTime)
        {
            Ct.curWd.camAngle = preangle + rotateAngle * Ct.ct.toward;
            finished = true;
            time = 0;
        }
        else
        {
            Ct.curWd.camAngle = preangle + SMath.Smooth(maxTime, time) * rotateAngle * Ct.ct.toward;
            finished = false;
        }
    }
    public void Watch()
    {
        transform.LookAt(ply.transform);
        Obj.facing = transform.rotation;
    }
    public void FreeVisionElevate()
    {
        Ct.curWd.camDist += Ct.curWd.camElevatepower * Ct.ct.toward * Time.deltaTime;
    }
    public void SmoothVisionElevate(out bool finished, float maxTime = 0.5f)
    {
        if (time == 0)
        {
            prelen = Ct.curWd.camDist;
            dist = Ct.curWd.camDeafDist - prelen;
        }

        time += Time.deltaTime;
        if (time >= maxTime)
        {
            Ct.curWd.camDist = prelen + dist;
            finished = true;
            time = 0;
            dist = 0;
        }
        else
        {
            Ct.curWd.camDist = prelen + SMath.Smooth(maxTime,time) * dist;
            finished = false;
        }
    }
    public void Following(Vector3 to)
    {
        to += new Vector3(Ct.curWd.CamPos.x, Ct.curWd.CamPos.y, Ct.curWd.CamPos.z);
        Vector3 dis = (to - transform.position) / follwingSpeed;
        transform.position += dis;
        
        //transform.position = to;
    }
    public void CamPosition()
    {
        Ct.curWd.CamPos.y = SMath.Parabola(Ct.curWd.camDist, 2) * Ct.curWd.camYp;//.camYp即 y 的 power

        Vector3 ar = SMath.V3.ParaAround(Vector3.zero, Ct.curWd.camAngle, Ct.curWd.camDist);//水平方面平行
        Ct.curWd.CamPos.x = ar.x;
        Ct.curWd.CamPos.z = ar.z;
        transform.position = cen + new Vector3(Ct.curWd.CamPos.x,Ct.curWd.CamPos.y,Ct.curWd.CamPos.z);

        Watch();
    }
    public float dist;
}