/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Ext.Sbe.Implementation;
using Hyperledger.Fabric.Shim.Implementation;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     *  * Simulates Handler.putStateMetadata() invocation from chaincode side
     *  * Waits for PUT_STATE_METADATA message from chaincode, including metadata entry with validation metadata and sends back response with empty payload
     */
    public class PutStateMetadataStep : IScenarioStep
    {
        private ChaincodeMessage orgMsg;
        private readonly StateBasedEndorsement val;

        public PutStateMetadataStep(StateBasedEndorsement sbe)
        {
            val = sbe;
        }

        /**
         * Check incoming message
         * If message type is PUT_STATE_METADATA and payload match to passed in constructor
         * @param msg message from chaincode
         * @return
         */
        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            PutStateMetadata psm;
            try
            {
                psm = PutStateMetadata.Parser.ParseFrom(msg.Payload);
            }
            catch (InvalidProtocolBufferException)
            {
                return false;
            }

            StateBasedEndorsement msgSbe = new StateBasedEndorsement(psm.Metadata.Value.ToByteArray());
            return msg.Type == ChaincodeMessage.Types.Type.PutStateMetadata && ChaincodeStub.VALIDATION_PARAMETER.Equals(psm.Metadata.Metakey) && msgSbe.ListOrgs().Count == val.ListOrgs().Count;
        }

        public List<ChaincodeMessage> Next()
        {
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid});
            return list;
        }
    }
}