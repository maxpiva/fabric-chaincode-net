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
     * Simulates another chaincode invocation
     * Waits for INVOKE_CHAINCODE
     * Sends back RESPONSE message with chaincode response inside
     */
    public class InvokeChaincodeStep : ScenarioStep
    {
        private ChaincodeMessage orgMsg;


        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.InvokeChaincode;
        }

        /**
     *
     * @return Chaincode response packed as payload inside COMPLETE message packed as payload inside RESPONSE message
     */

        public List<ChaincodeMessage> Next()
        {
            ByteString chaincodeResponse = new Protos.Peer.ProposalResponsePackage.Response {Status = (int) Protos.Common.Status.Success, Message = "OK"}.ToByteString();
            ByteString completePayload = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Completed, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid, Payload = chaincodeResponse}.ToByteString();
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid, Payload = completePayload});
            return list;
        }
    }
}