/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Simulates getState
     * Waits for GET_STATE message
     * Returns response message with value as payload
     */
    public class GetValueStep : ScenarioStep
    {
        private ChaincodeMessage orgMsg;
        private readonly string val;

        /**
     *
     * @param val value to return
     */
        public GetValueStep(string val)
        {
            this.val = val;
        }


        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.GetState;
        }


        public List<ChaincodeMessage> Next()
        {
            ByteString getPayload = ByteString.CopyFromUtf8(val);
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid, Payload = getPayload});
            return list;
        }
    }
}