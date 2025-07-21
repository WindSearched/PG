using System;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using IPGModAPI; // 替换成你的命名空间

public class IPGMAdapter : CrossBindingAdaptor
{
    public override Type BaseCLRType => typeof(IPGM); // 主工程接口类型

    public override Type AdaptorType => typeof(Adapter);

    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adapter(appdomain, instance);
    }

    public class Adapter : CrossBindingAdaptorType, IPGM
    {
        private IMethod mOnLoad;
        private IMethod mOnStart;
        private readonly ILTypeInstance instance;
        private readonly ILRuntime.Runtime.Enviorment.AppDomain appdomain;

        public Adapter(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
        {
            this.appdomain = appdomain;
            this.instance = instance;
        }

        public ILTypeInstance ILInstance => instance;

        public void OnLoad()
        {
            if (mOnLoad == null)
                mOnLoad = instance.Type.GetMethod("OnLoad", 0);
            if (mOnLoad != null)
                appdomain.Invoke(mOnLoad, instance, null);
        }

        public void OnStart()
        {
            if (mOnStart == null)
                mOnStart = instance.Type.GetMethod("OnStart", 0);
            if (mOnStart != null)
                appdomain.Invoke(mOnStart, instance, null);
        }
    }
}
