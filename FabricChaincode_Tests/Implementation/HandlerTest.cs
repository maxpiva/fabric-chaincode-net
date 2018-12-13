/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Threading;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
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
            CancellationToken token = default(CancellationToken);

            ChaincodeBaseAsync cb = new EmptyChaincode();

            ChaincodeID chaincodeId = new ChaincodeID {Name = "mycc"};
            Handler handler = Handler.CreateAsync(chaincodeId, cb).RunAndUnwrap();

            ChaincodeMessage msgReg = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Registered};
            // Correct message
            handler.OnChaincodeMessageAsync(msgReg,token).RunAndUnwrap();
            Assert.AreEqual(CCState.ESTABLISHED, handler.State, "Not correct handler state");

            ChaincodeMessage msgReady = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Ready};
            // Correct message
            handler.OnChaincodeMessageAsync(msgReady, token).RunAndUnwrap();
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            handler = Handler.CreateAsync(chaincodeId, cb).RunAndUnwrap();
            // Incorrect message
            handler.OnChaincodeMessageAsync(msgReady, token).RunAndUnwrap();
            Assert.AreEqual(CCState.CREATED, handler.State, "Not correct handler state");
            // Correct message
            handler.OnChaincodeMessageAsync(msgReg, token).RunAndUnwrap();
            Assert.AreEqual(CCState.ESTABLISHED, handler.State, "Not correct handler state");
            // Incorrect message
            handler.OnChaincodeMessageAsync(msgReg, token).RunAndUnwrap();
            Assert.AreEqual(CCState.ESTABLISHED, handler.State, "Not correct handler state");
            handler.OnChaincodeMessageAsync(msgReady, token).RunAndUnwrap();
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            // Unrelated message, do nothing
            ChaincodeMessage unkonwnMessage = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.PutState, ChannelId = "mychannel", Txid = "q", Payload = ByteString.CopyFromUtf8("")};

            handler.OnChaincodeMessageAsync(unkonwnMessage, token).RunAndUnwrap();
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            // KEEPALIVE message, do nothing
            ChaincodeMessage keepAliveMessage = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Keepalive, ChannelId = "mychannel", Txid = "q", Payload = ByteString.CopyFromUtf8("")};
            handler.OnChaincodeMessageAsync(keepAliveMessage, token).RunAndUnwrap();
            Assert.AreEqual(CCState.READY, handler.State, "Not correct handler state");

            ChaincodeMessage errorMsg = new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Error, ChannelId = "mychannel", Txid = "q", Payload = ByteString.CopyFromUtf8("")};

            // Error message, except exception, no open communication
            try
            {
                handler.OnChaincodeMessageAsync(errorMsg, token).RunAndUnwrap();
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