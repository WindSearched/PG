using ILRuntime.CLR.Method;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using System.Reflection;
using UnityEngine;

public class HotfixMonoProxy : MonoBehaviour
{
    private ILTypeInstance instance;
    private AppDomain appDomain;

    private IMethod awakeMethod;
    private IMethod startMethod;
    private IMethod updateMethod;
    private IMethod onDestroyMethod;
    public void Init(ILTypeInstance instance, AppDomain appDomain)
    {
        this.instance = instance;
        this.appDomain = appDomain;

        var type = instance.Type;

        awakeMethod = type.GetMethod("Awake", 0);
        startMethod = type.GetMethod("Start", 0);
        updateMethod = type.GetMethod("Update", 0);
        onDestroyMethod = type.GetMethod("OnDestroy", 0);

        instance.AssignFieldNoClone(type.GetFieldIndex("gameObject"), gameObject);
        instance.AssignFieldNoClone(type.GetFieldIndex("transform"), transform);
    }

    void Awake()
    {
        if (awakeMethod != null)
            appDomain.Invoke(awakeMethod, instance, null);
    }

    void Start()
    {
        if (startMethod != null)
            appDomain.Invoke(startMethod, instance, null);
    }

    void Update()
    {
        if (updateMethod != null)
            appDomain.Invoke(updateMethod, instance, null);
    }

    void OnDestroy()
    {
        if (onDestroyMethod != null)
            appDomain.Invoke(onDestroyMethod, instance, null);
    }
}
