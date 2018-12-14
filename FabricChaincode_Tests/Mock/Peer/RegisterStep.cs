/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Simulates chaincode registration after start
     * Waits for REGISTER message from chaincode
     * Sends back pair of messages: REGISTERED and READY
     */
    public class RegisterStep : IScenarioStep
    {
        ChaincodeMessage orgMsg;
        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type==ChaincodeMessage.Types.Type.Register;
        }

     
        public List<ChaincodeMessage> Next()
        {
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Registered});
            list.Add(new ChaincodeMessage { Type = ChaincodeMessage.Types.Type.Ready });
                return list;
        }
    }
}
