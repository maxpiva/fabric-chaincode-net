/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Simulates delState() invocation in chaincode
     * Waits for DEL_STATE message from chaincode and sends back response with empty payload
     */
    public class DelValueStep : IScenarioStep
    {
        private ChaincodeMessage orgMsg;

        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.DelState;
        }


        public List<ChaincodeMessage> Next()
        {
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid});
            return list;
        }
    }
}