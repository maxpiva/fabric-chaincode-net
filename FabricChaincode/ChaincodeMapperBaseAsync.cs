using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hyperledger.Fabric.Shim
{
    public class ChaincodeMapperBaseAsync : ChaincodeBaseAsync
    {
        private Dictionary<string, (bool,MethodInfo)> methodInfos;
        public ChaincodeMapperBaseAsync()
        {
            List<MethodInfo> methods = GetType().GetMethods(BindingFlags.Public).Where(a => 
                a.GetParameters().Length ==1 || 
                (a.GetParameters().Length==2 && typeof(CancellationToken).IsAssignableFrom(a.GetParameters()[1].ParameterType)) && 
                typeof(IChaincodeStub).IsAssignableFrom(a.GetParameters()[0].ParameterType) && 
                typeof(Task<Response>).IsAssignableFrom(a.ReturnType)).ToList();
            methodInfos = new Dictionary<string, (bool,MethodInfo)>();
            foreach (MethodInfo m in methods)
            {
                if (m.Name == "InvokeAsync" || m.Name == "InitAsync")
                    continue;
                bool useToken = m.GetParameters().Length == 2 && typeof(CancellationToken).IsAssignableFrom(m.GetParameters()[1].ParameterType);
                FunctionName f = m.GetCustomAttribute(typeof(FunctionName), true) as FunctionName;
                if (f != null)
                {
                    methodInfos.Add(f.Name.ToLowerInvariant(),(useToken,m));
                }
                else
                {
                    string name = m.Name.ToLowerInvariant();
                    if (name.EndsWith("async"))
                        name = name.Substring(0, name.Length - 5);
                    methodInfos.Add(name, (useToken,m));

                }
            }
        }
        public override Task<Response> InitAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(NewSuccessResponse());
        }

        public override async Task<Response> InvokeAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken))
        {
            try
            {
                string function = stub.Function.ToLowerInvariant();
                if (methodInfos.ContainsKey(function))
                {
                    (bool useToken, MethodInfo method) = methodInfos[function];
                    if (useToken)
                        return await (Task<Response>)method.Invoke(this, new object[] { stub, token });
                    return await (Task<Response>)method.Invoke(this, new object[] { stub });
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
