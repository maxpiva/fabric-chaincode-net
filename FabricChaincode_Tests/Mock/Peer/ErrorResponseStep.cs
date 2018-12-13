/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Error message from chaincode side, no response sent
     */
    public class ErrorResponseStep : ScenarioStep {


    public bool Expected(ChaincodeMessage msg)
    {
        return msg.Type == ChaincodeMessage.Types.Type.Error;
    }


    public List<ChaincodeMessage> Next()
    {
        return new List<ChaincodeMessage>();
    }
    }
}
