/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Simulates requesting/receiving next set of results for query
     * Waits for QUERY_STATE_NEXT
     * Returns message that contains list of results in form ("key" => "key Value")*
     */
    public class QueryNextStep : QueryResultStep
    {

        /**
         * Initiate step
         * @param hasNext is response message QueryResponse hasMore field set
         * @param vals list of keys to generate ("key" => "key Value") pairs
         */
        public QueryNextStep(bool hasNext, params string[] vals) : base(hasNext,vals)
        {
        }

    

        public override bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.QueryStateNext;
        }
    }
}
