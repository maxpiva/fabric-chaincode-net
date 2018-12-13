/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Utils
{
    public class MessageUtil
    {
        /**
     * Generate chaincode messages
     *
     * @param type
     * @param channelId
     * @param txId
     * @param payload
     * @param event
     * @return
     */
        public static ChaincodeMessage NewEventMessage(ChaincodeMessage.Types.Type type, string channelId, string txId, ByteString payload, ChaincodeEvent evnt)
        {
            ChaincodeMessage msg = new ChaincodeMessage {Type = type, ChannelId = channelId, Txid = txId, Payload = payload};
            if (evnt != null)
                msg.ChaincodeEvent = evnt;
            return msg;
        }
    }
}