/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    public interface ScenarioStep
    {
        /** Validate incoming message from chaincode side
         *
         * @param msg message from chaincode
         * @return is incoming message was expected
         */
        bool Expected(ChaincodeMessage msg);

        /**
         * List of messages send from peer to chaincode as response(s)
         * @return
         */
        List<ChaincodeMessage> Next();
    }
}
