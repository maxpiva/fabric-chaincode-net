/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Simulate last query (close) step. Happens after passing over all query result
     * Waits for QUERY_STATE_CLOSE
     * Sends back response with empty payload
     */
    public class QueryCloseStep : ScenarioStep
    {
        private ChaincodeMessage orgMsg;


        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.QueryStateClose;
        }

        /**
     *
     * @return RESPONSE message with empty payload
     */

        public List<ChaincodeMessage> Next()
        {
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid});
            return list;
        }
    }
}