/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Hyperledger.Fabric.Protos.Peer;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Waits for COMPLETED message, sends nothing back
     */
    public class CompleteStep : IScenarioStep
    {
        public bool Expected(ChaincodeMessage msg)
        {
            return msg.Type == ChaincodeMessage.Types.Type.Completed;
        }


        public List<ChaincodeMessage> Next()
        {
            return new List<ChaincodeMessage>();
        }
    }
}