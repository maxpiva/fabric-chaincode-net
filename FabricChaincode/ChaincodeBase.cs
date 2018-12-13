using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hyperledger.Fabric.Shim
{
    public abstract class ChaincodeBase : ChaincodeBaseAsync, IChaincodeSync
    {
        public abstract Response Init(IChaincodeStub stub);

        public abstract Response Invoke(IChaincodeStub stub);

        public sealed override Task<Response> InitAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(Init(stub));
        }


        public sealed override Task<Response> InvokeAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(Invoke(stub));
        }

    }
}
