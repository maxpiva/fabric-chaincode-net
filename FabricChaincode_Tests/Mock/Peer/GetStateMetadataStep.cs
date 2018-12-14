using System.Collections.Generic;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Ext.Sbe.Implementation;
using Hyperledger.Fabric.Shim.Implementation;
/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

/**
 * simulates Handler.getStateMetadata
 * Waits for GET_STATE_METADATA message
 * Returns response message with stored metadata
 */
namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    public class GetStateMetadataStep : IScenarioStep
    {
        private ChaincodeMessage orgMsg;
        private readonly byte[] val;

        /**
         * @param sbe StateBasedEndosement to return as one and only one metadata entry
         */
        public GetStateMetadataStep(StateBasedEndorsement sbe)
        {
            val = sbe.Policy();
        }

        public bool Expected(ChaincodeMessage msg)
        {
            orgMsg = msg;
            return msg.Type == ChaincodeMessage.Types.Type.GetStateMetadata;
        }

        public List<ChaincodeMessage> Next()
        {
            List<StateMetadata> entriesList = new List<StateMetadata>();
            StateMetadata validationValue = new StateMetadata {Metakey = ChaincodeStub.VALIDATION_PARAMETER, Value = ByteString.CopyFrom(val)};
            entriesList.Add(validationValue);
            StateMetadataResult stateMetadataResult = new StateMetadataResult();
            stateMetadataResult.Entries.AddRange(entriesList);
            List<ChaincodeMessage> list = new List<ChaincodeMessage>();
            list.Add(new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Response, ChannelId = orgMsg.ChannelId, Txid = orgMsg.Txid, Payload = stateMetadataResult.ToByteString()});
            return list;
        }
    }
}