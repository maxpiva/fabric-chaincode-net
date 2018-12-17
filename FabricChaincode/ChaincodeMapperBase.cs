using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hyperledger.Fabric.Shim
{
    public class ChaincodeMapperBase : ChaincodeBase
    {
        private Dictionary<string, MethodInfo> methodInfos;
        public ChaincodeMapperBase()
        {
            List<MethodInfo> methods = GetType().GetMethods(BindingFlags.Public).Where(a => a.GetParameters().Length == 1 && typeof(IChaincodeStub).IsAssignableFrom(a.GetParameters()[0].ParameterType) && typeof(Response).IsAssignableFrom(a.ReturnType)).ToList();
            methodInfos=new Dictionary<string, MethodInfo>();
            foreach (MethodInfo m in methods)
            {
                if (m.Name=="Invoke" || m.Name=="Init")
                    continue;
                FunctionName f= m.GetCustomAttribute(typeof(FunctionName), true) as FunctionName;
                methodInfos.Add(f != null ? f.Name.ToLowerInvariant() : m.Name.ToLowerInvariant(), m);
            }
        }
        public override Response Init(IChaincodeStub stub)
        {
            return NewSuccessResponse();
        }

        public override Response Invoke(IChaincodeStub stub)
        {
            try
            {
                string function = stub.Function.ToLowerInvariant();
                if (methodInfos.ContainsKey(function))
                {
                    return (Response)methodInfos[function].Invoke(this, new object[] { stub });
                }
                return NewErrorResponse("Unknown function " + function);
            }
            catch (Exception e)
            {
                return NewErrorResponse(e.Message);
            }
        }
    }

}
