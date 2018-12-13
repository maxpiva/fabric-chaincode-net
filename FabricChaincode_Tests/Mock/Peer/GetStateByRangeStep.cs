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
     * Simulates getStateByRange
     * Waits for GET_STATE_BY_RANGE message
     * Returns message that contains list of results in form ("key" => "key Value")*
     */
    public class GetStateByRangeStep : QueryResultStep {

    /**
     * Initiate step
     * @param hasNext is response message QueryResponse hasMore field set
     * @param vals list of keys to generate ("key" => "key Value") pairs
     */
    public GetStateByRangeStep(bool hasNext, params string[] vals) : base(hasNext,vals)
    {

    }

    public override bool Expected(ChaincodeMessage msg)
    {
        orgMsg = msg;
        return msg.Type == ChaincodeMessage.Types.Type.GetStateByRange;
    }
    }
}
