/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using Hyperledger.Fabric.Shim.Implementation;

namespace Hyperledger.Fabric.Shim.Tests.Chaincode
{
    public class EmptyChaincode : ChaincodeBase
    {
    
        public override Response Init(IChaincodeStub stub)
        {
            return NewSuccessResponse();
        }

        public override Response Invoke(IChaincodeStub stub)
        {
            return NewSuccessResponse();
        }
    }
}
