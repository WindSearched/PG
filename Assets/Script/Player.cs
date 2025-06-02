using System.Collections.Generic;
using UnityEngine;

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
        Ct.act.Main.direction.performed +=
            c =>
            {
                if (Page.IsPage("main"))
                {
                    ent.speed = Ct.curWd.playerSpeed;
                    detDIr = true;
                }
            };

        transform.position = Ct.curWd.plyPos;
        GetComponent<SphereCollider>().radius = Ct.curWd.approacherDistance;
    }

    public void Update()
    {
        inp = Ct.act.Main.direction.ReadValue<Vector3>();
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
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("drop"))
        {
            Drops d = other.GetComponent<Drops>();
            d.toApproach = true;
            Ct.ct.CT(d.Approaching());
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

    public Vector3 DirectionAdjustment()
    {
        float a = Ct.curWd.camAngle;
        float b = SMath.Angle(inp);
        float r = b - 90 + a;

        return new(SMath.CosA(r), 0, SMath.SinA(r));
    }
}
