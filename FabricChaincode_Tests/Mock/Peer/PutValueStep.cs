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
     * Simulates putState() invocation in chaincode
     * Waits for PUT_STATE message from chaincode, including value and sends back response with empty payload
     */
    public class PutValueStep : ScenarioStep
    {
        private readonly string val;
        private ChaincodeMessage orgMsg;

        /**
     * Initiate step
     * @param val
     */
        public PutValueStep(string val)
        {
            this.val = val;
        }

        /**
         * Check incoming message
         * If message type is PUT_STATE and payload equal to passed in constructor
         * @param msg message from chaincode
         * @return
         */
        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            PutState putMsg;
            try
            {
                putMsg = PutState.Parser.ParseFrom(msg.Payload);
            }
            catch (InvalidProtocolBufferException)
            {
                return false;
            }

            return val.Equals(putMsg.Value.ToStringUtf8()) && msg.Type == ChaincodeMessage.Types.Type.PutState;
        }


        public List<ChaincodeMessage> Next()
        {
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid});
            return list;
        }
    }
}