/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Implementation;
using Hyperledger.Fabric.Shim.Tests.Chaincode;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Implementation
{
	[TestClass]
    public class HandlerTest
    {
        [TestMethod]
        public void TestHandlerStates()
        {
            ChaincodeBase cb = new EmptyChaincode();

            ChaincodeID chaincodeId = new ChaincodeID {Name = "mycc"};
            Handler handler = new Handler(chaincodeId, cb);

            ChaincodeMessage msgReg = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Register};
            // Correct message
            handler.OnChaincodeMessage(msgReg);
            Assert.AreEqual(CCState.ESTABLISHED, handler.State, "Not correct handler state");

            ChaincodeMessage msgReady = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Ready};
            // Correct message
            handler.OnChaincodeMessage(msgReady);
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            handler = new Handler(chaincodeId, cb);
            // Incorrect message
            handler.OnChaincodeMessage(msgReady);
            Assert.AreEqual(CCState.CREATED, handler.State, "Not correct handler state");
            // Correct message
            handler.OnChaincodeMessage(msgReg);
            Assert.AreEqual(CCState.ESTABLISHED, handler.State, "Not correct handler state");
            // Incorrect message
            handler.OnChaincodeMessage(msgReg);
            Assert.AreEqual(CCState.ESTABLISHED, handler.State, "Not correct handler state");
            handler.OnChaincodeMessage(msgReady);
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            // Unrelated message, do nothing
            ChaincodeMessage unkonwnMessage = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.PutState, ChannelId = "mychannel", Txid = "q", Payload = ByteString.CopyFromUtf8("")};

            handler.OnChaincodeMessage(unkonwnMessage);
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            // KEEPALIVE message, do nothing
            ChaincodeMessage keepAliveMessage = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Keepalive, ChannelId = "mychannel", Txid = "q", Payload = ByteString.CopyFromUtf8("")};
            handler.OnChaincodeMessage(keepAliveMessage);
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            ChaincodeMessage errorMsg = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Error, ChannelId = "mychannel", Txid = "q", Payload = ByteString.CopyFromUtf8("")};

            // Error message, except exception, no open communication
            try
            {
                handler.OnChaincodeMessage(errorMsg);
                Assert.Fail("Expecting InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                //Ignore
            }

            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");
        }
    }
}