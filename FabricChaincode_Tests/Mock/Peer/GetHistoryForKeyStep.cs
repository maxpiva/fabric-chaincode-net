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
    public class GetHistoryForKeyStep : ScenarioStep
    {
        private readonly bool hasNext;
        private ChaincodeMessage orgMsg;
        private readonly string[] values;

        /**
     * Initiate step
     * @param hasNext is response message QueryResponse hasMore field set
     * @param vals list of keys to generate ("key" => "key Value") pairs
     */
        public GetHistoryForKeyStep(bool hasNext, params string[] vals)
        {
            values = vals;
            this.hasNext = hasNext;
        }


        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.GetHistoryForKey;
        }


        public List<ChaincodeMessage> Next()
        {
            List<KeyModification> keyModifications = values.Select(a => new KeyModification {TxId = a, Value = ByteString.CopyFromUtf8(a + " Value")}).ToList();
            QueryResponse qr = new QueryResponse {HasMore = hasNext};
            qr.Results.Add(keyModifications.Select(a => new QueryResultBytes {ResultBytes = a.ToByteString()}));
            ByteString historyPayload = qr.ToByteString();
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid, Payload = historyPayload});
            return list;
        }
    }
}