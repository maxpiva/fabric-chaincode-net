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
/*
 * Base class for multi result query steps/messages
 */
    public abstract class QueryResultStep : ScenarioStep
    {
        private readonly bool hasNext;
        internal ChaincodeMessage orgMsg;
        private readonly string[] values;

        /**
         * Initiate step
         * @param hasNext is response message QueryResponse hasMore field set
         * @param vals list of keys to generate ("key" => "key Value") pairs
         */
        internal QueryResultStep(bool hasNext, params string[] vals)
        {
            values = vals;
            this.hasNext = hasNext;
        }


        /**
         * Generate response message that list of (key => value) pairs
         * @return
         */

        public abstract bool Expected(ChaincodeMessage msg);

        public List<ChaincodeMessage> Next()
        {
            List<KV> keyValues = values.Select(a => new KV {Key = a, Value = ByteString.CopyFromUtf8(a + " Value")}).ToList();

            QueryResponse qr = new QueryResponse {HasMore = hasNext};
            qr.Results.AddRange(keyValues.Select(a => new QueryResultBytes {ResultBytes = a.ToByteString()}));
            ByteString rangePayload = qr.ToByteString();

            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid, Payload = rangePayload});
            return list;
        }
    }
}