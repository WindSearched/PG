using System.Collections;
using UnityEngine;

public class Drops : MonoBehaviour
{
    public static Transform parent;
    public Rigidbody body;

    public int amt = 0;
    public string item = "n";
    public int dur = -1;

    public bool toApproach = true;
    private void Awake()
    {
        parent = GameObject.Find("Drops").transform;
    }
    private void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        {
            if (other.CompareTag("drop"))
            {
                Drops d = other.GetComponent<Drops>();
                if (item == d.item && dur == -1 && d.dur == -1)
                {
                    item += d.item;
                    Destroy(d.gameObject);
                }
            }
        }
    }
    private void OnDestroy()
    {
        toApproach = false;
    }
    public IEnumerator Approaching()
    {
        if (body == null) body = GetComponent<Rigidbody>();
        while (toApproach)
        {
            if (!Ct.curWd.inventory.HasFreeItemGrid(item) && Ct.curWd.inventory.full)
            {
                yield return null;
                toApproach = false;
                continue;
            }
            else
            {
                Vector3 toward = Ct.ct.player.transform.position - transform.position;
                float legth = toward.magnitude;

                if (legth > Ct.curWd.approacherDistance)
                {
                    body.velocity = Vector3.zero;
                    toApproach = false;
                    yield break;
                }
                body.AddForce(Ct.curWd.dropsApporachSpeed * Time.deltaTime * toward);
                if (legth < Ct.curWd.absorbDistance)
                {
                    Ct.curWd.inventory.Add(item, amt, out int full);
                    if (full > 0)
                    {
                        amt = full;
                        yield break;
                    }
                    toApproach = false;
                    Destroy(gameObject);
                    yield break;
                }
            }
            yield return null;
        }
    }
    public static GameObject Load(string item, int amt, Vector3 position, int dur = -1)
    {
        GameObject o = Instantiate(Resources.Load("drop") as GameObject, parent);
        o.GetComponent<SpriteRenderer>().sprite = Item.GetSprite(item);
        o.transform.position = position;

        Drops d = o.GetComponent<Drops>();
        d.dur = dur;
        d.item = item;
        d.amt = amt;

        return o;
    }
}
