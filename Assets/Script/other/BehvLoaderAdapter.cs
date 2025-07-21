using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using System;

public class BehvLoaderAdapter : CrossBindingAdaptor
{
    public override Type BaseCLRType => typeof(BehvLoader); // 主项目中的基类
    public override Type AdaptorType => typeof(Adaptor);
    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adaptor(appdomain, instance);
    }

    class Adaptor : BehvLoader, CrossBindingAdaptorType
    {
        ILTypeInstance instance;
        ILRuntime.Runtime.Enviorment.AppDomain appdomain;

        public Adaptor() { }

        public Adaptor(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
        {
            this.appdomain = appdomain;
            this.instance = instance;
        }

        public ILTypeInstance ILInstance => instance;

        // override 基类方法，调用热更实现
        public override void Start()
        {
            var method = instance.Type.GetMethod("SomeVirtualMethod", 0);
            if (method != null)
                appdomain.Invoke(method, instance, null);
            else
                base.Start();
        }
        public override void Update()
        {
            var method = instance.Type.GetMethod("SomeVirtualMethod", 0);
            if (method != null)
                appdomain.Invoke(method, instance, null);
            else
                base.Update();
        }
        public override void Awake()
        {
            var method = instance.Type.GetMethod("SomeVirtualMethod", 0);
            if (method != null)
                appdomain.Invoke(method, instance, null);
            else
                base.Awake();
        }
        public override void OnDestroy()
        {
            var method = instance.Type.GetMethod("SomeVirtualMethod", 0);
            if (method != null)
                appdomain.Invoke(method, instance, null);
            else
                base.OnDestroy();
        }
    }
}
