using IPGModAPI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static SMath;

public class Player : MonoBehaviour
{
    public Vector3 dir = new();
    public Vector3 inp = new();

    public Entity ent;
    public GameObject plane;
    private bool detDIr = false;

    public static List<Entity> entitiesAround = new();

    private void Start()
    {
        ent = GetComponent<Entity>();
        dPGM a = () =>
        {
            if (Page.IsPage("main"))
            {
                ent.speed = Ct.curWd.playerSpeed;
                detDIr = true;
            }
        };

        Ct.act.Main.direction.performed += c => a.Invoke();
        Ct.ct.joystick.OnInputIn += a;

        transform.position = Ct.curWd.plyPos;
        GetComponent<SphereCollider>().radius = Ct.curWd.approacherDistance;
    }

    public void Update()
    {
        inp = GetDir();
        if (inp == Vector3.zero)
        {
            detDIr = false;
            dir = new();
            ent.speed = 0f;
        }
        if (detDIr)
        {
            dir = -DirectionAdjustment();
            ent.direction = dir;

            Ct.evn.IWhenPlayerMoving();
        }

        Ct.cam.Following(transform.position);
        plane.transform.position = transform.position;
    }

    public Vector3 GetDir()
    {
        if (Ct.ct.joystick.isInputing)
            return WorldGenerator.To3DPos(Ct.ct.joystick.InputVector);
        else
            return Ct.act.Main.direction.ReadValue<Vector3>().normalized;
    }
    public Coroutine cor;
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("drop"))
        {
            Drops d = other.GetComponent<Drops>();
            d.toApproach = true;
            cor = Ct.ct.CT(d.Approaching());
        }
        else if (other.gameObject.CompareTag("entity"))
        {
            entitiesAround.Add(other.GetComponent<Entity>()); ;
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("entity"))
        {
            entitiesAround.Remove(other.GetComponent<Entity>()); ;
        }
    }
    private void OnDisable()
    {
        if(cor != null)
        {
            Ct.ct.Cta(cor);
            cor = null;
        }
    }
    public Vector3 DirectionAdjustment()
    {
        float a = Ct.curWd.camAngle;
        float b = SMath.Angle(inp);
        float r = b - 90 + a;

        return new Vector3(SMath.CosA(r), 0, SMath.SinA(r)) * inp.magnitude;
    }
}
