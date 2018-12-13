/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Ledger;
using Hyperledger.Fabric.Shim.Tests.Chaincode;
using Hyperledger.Fabric.Shim.Tests.Mock.Peer;
using Hyperledger.Fabric.Shim.Tests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.FVT
{
	[TestClass]
    public class ChaincodeFVTest
    {
#if DEBUG
        private static int Timeout = 200000;
#else
        private static int Timeout = 5000;
#endif
        private ChaincodeMockPeer server;

        [TestCleanup]
        public void AfterTest()
        {
            server?.Stop();
            server = null;
        }

        [TestMethod]
        public void TestRegister()
        {
            ChaincodeBaseAsync cb = new EmptyChaincode();

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());

            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});

            CheckScenarioStepEnded(server, 1, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Ready);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Register);
        }

        [TestMethod]
        public void TestRegisterAndEmptyInit()
        {
            ChaincodeBaseAsync cb = new EmptyChaincode();
            ChaincodeInput inp = new ChaincodeInput();
            inp.Args.Add(ByteString.CopyFromUtf8(""));
            ByteString payload = inp.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", payload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());

            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);

            server.Send(initMsg);
            CheckScenarioStepEnded(server, 2, Timeout);

            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Init);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
        }

        [TestMethod]
        public void TestInitAndInvoke()
        {
            ChaincodeBase cb = new Cb();
            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("init"), ByteString.CopyFromUtf8("a"), ByteString.CopyFromUtf8("100")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new PutValueStep("100"));
            scenario.Add(new CompleteStep());
            scenario.Add(new GetValueStep("100"));
            scenario.Add(new PutValueStep("120"));
            scenario.Add(new DelValueStep());
            scenario.Add(new CompleteStep());
            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);

            server.Send(initMsg);
            CheckScenarioStepEnded(server, 3, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response1");
            ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("invoke"), ByteString.CopyFromUtf8("a"), ByteString.CopyFromUtf8("10")});
            ByteString invokePayload = ci.ToByteString();
            ChaincodeMessage invokeMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Transaction, "testChannel", "0", invokePayload, null);
            server.Send(invokeMsg);

            CheckScenarioStepEnded(server, 7, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response2");
        }

        [TestMethod]
        public void TestInvokeRangeQ()
        {
            ChaincodeBaseAsync cb = new Cb2();
            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);
            ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("invoke"), ByteString.CopyFromUtf8("a"), ByteString.CopyFromUtf8("b")});
            ByteString invokePayload = ci.ToByteString();
            ChaincodeMessage invokeMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Transaction, "testChannel", "0", invokePayload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());
            scenario.Add(new GetStateByRangeStep(false, "a", "b"));
            scenario.Add(new QueryCloseStep());
            scenario.Add(new CompleteStep());
            scenario.Add(new GetStateByRangeStep(true, "a", "b"));
            scenario.Add(new QueryNextStep(false, "c"));
            scenario.Add(new QueryCloseStep());
            scenario.Add(new CompleteStep());

            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);
            server.Send(initMsg);
            CheckScenarioStepEnded(server, 2, Timeout);
            server.Send(invokeMsg);
            CheckScenarioStepEnded(server, 5, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response2");
            server.Send(invokeMsg);
            CheckScenarioStepEnded(server, 9, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response2");
        }

        [TestMethod]
        public void TestGetQueryResult()
        {
            ChaincodeBaseAsync cb = new Cb3();
            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);
            ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("invoke"), ByteString.CopyFromUtf8("query")});
            ByteString invokePayload = ci.ToByteString();
            ChaincodeMessage invokeMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Transaction, "testChannel", "0", invokePayload, null);


            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());
            scenario.Add(new GetQueryResultStep(false, "a", "b"));
            scenario.Add(new QueryCloseStep());
            scenario.Add(new CompleteStep());
            scenario.Add(new GetQueryResultStep(true, "a", "b"));
            scenario.Add(new QueryNextStep(false, "c"));
            scenario.Add(new QueryCloseStep());
            scenario.Add(new CompleteStep());

            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);
            server.Send(initMsg);
            CheckScenarioStepEnded(server, 2, Timeout);
            server.Send(invokeMsg);
            CheckScenarioStepEnded(server, 5, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response2");
            server.Send(invokeMsg);
            CheckScenarioStepEnded(server, 9, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response2");
        }

        [TestMethod]
        public void TestGetHistoryForKey()
        {
            ChaincodeBase cb = new Cb4();
            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);
            ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("invoke"), ByteString.CopyFromUtf8("key1")});
            ByteString invokePayload = ci.ToByteString();
            ChaincodeMessage invokeMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Transaction, "testChannel", "0", invokePayload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());
            scenario.Add(new GetHistoryForKeyStep(false, "1", "2"));
            scenario.Add(new QueryCloseStep());
            scenario.Add(new CompleteStep());
            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);
            server.Send(initMsg);
            CheckScenarioStepEnded(server, 2, Timeout);
            server.Send(invokeMsg);
            CheckScenarioStepEnded(server, 5, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
            Assert.AreEqual(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(server.LastMessageRcvd.Payload).Message, "OK response2");
        }

        [TestMethod]
        public void TestInvokeChaincode()
        {
            ChaincodeBase cb = new Cb5();
            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);
            ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("invoke")});
            ByteString invokePayload = ci.ToByteString();
            ChaincodeMessage invokeMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Transaction, "testChannel", "0", invokePayload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());
            scenario.Add(new InvokeChaincodeStep());
            scenario.Add(new CompleteStep());

            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);
            server.Send(initMsg);
            CheckScenarioStepEnded(server, 2, Timeout);
            server.Send(invokeMsg);
            CheckScenarioStepEnded(server, 4, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Response);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Completed);
        }

        [TestMethod]
        public void TestErrorInitInvoke()
        {
            ChaincodeBase cb = new Cb6();

            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new ErrorResponseStep());
            scenario.Add(new ErrorResponseStep());

            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);

            server.Send(initMsg);
            CheckScenarioStepEnded(server, 2, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Init);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Error);
            Assert.AreEqual(server.LastMessageRcvd.Payload.ToStringUtf8(), "Wrong response1");
            ci = new ChaincodeInput();
            ByteString invokePayload = ci.ToByteString();
            ChaincodeMessage invokeMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Transaction, "testChannel", "0", invokePayload, null);


            server.Send(invokeMsg);

            CheckScenarioStepEnded(server, 3, Timeout);
            Assert.AreEqual(server.LastMessageSend.Type, ChaincodeMessage.Types.Type.Transaction);
            Assert.AreEqual(server.LastMessageRcvd.Type, ChaincodeMessage.Types.Type.Error);
            Assert.AreEqual(server.LastMessageRcvd.Payload.ToStringUtf8(), "Wrong response2");
        }

        [TestMethod]
        public void TestStreamShutdown()
        {
            ChaincodeBase cb = new Cb7();
            ChaincodeInput ci = new ChaincodeInput();
            ci.Args.AddRange(new[] {ByteString.CopyFromUtf8("")});
            ByteString initPayload = ci.ToByteString();
            ChaincodeMessage initMsg = MessageUtil.NewEventMessage(ChaincodeMessage.Types.Type.Init, "testChannel", "0", initPayload, null);

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());
            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});
            CheckScenarioStepEnded(server, 1, Timeout);

            server.Send(initMsg);
            server.Stop();
            server = null;
        }

        [TestMethod]
        [Ignore] //Not Supported yet
        public void TestChaincodeLogLevel()
        {
            ChaincodeBase cb = new EmptyChaincode();

            List<ScenarioStep> scenario = new List<ScenarioStep>();
            scenario.Add(new RegisterStep());
            scenario.Add(new CompleteStep());
            server = ChaincodeMockPeer.StartServer(scenario);

            cb.Start(new string[] {"-a", "127.0.0.1:7052", "-i", "testId"});

            //assertEquals("Wrong debug level for " + cb.getClass().getPackage().getName(), Level.FINEST, Logger.getLogger(cb.getClass().getPackage().getName()).getLevel());
        }

        public static void CheckScenarioStepEnded(ChaincodeMockPeer s, int step, int timeout)
        {
            try
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (s.LastExecutedStep == step)
                            return;
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                }).TimeoutAsync(TimeSpan.FromMilliseconds(timeout), default(CancellationToken)).RunAndUnwrap();
            }
            catch (TimeoutException)
            {
                Assert.Fail("Got timeout, first step not finished");
            }
        }

        public class Cb : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                Assert.AreEqual(stub.Function, "init");
                Assert.AreEqual(stub.Args.Count, 3);
                stub.PutState("a", ByteString.CopyFromUtf8("100").ToByteArray());
                return NewSuccessResponse("OK response1");
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                Assert.AreEqual(stub.Function, "invoke");
                Assert.AreEqual(stub.Args.Count, 3);
                string aKey = stub.StringArgs[1];
                Assert.AreEqual(aKey, "a");
                string aVal = stub.GetStringState(aKey);
                stub.PutState(aKey, ByteString.CopyFromUtf8("120").ToByteArray());
                stub.DelState("delKey");
                return NewSuccessResponse("OK response2");
            }
        }

        public class Cb2 : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                return NewSuccessResponse("OK response1");
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                Assert.AreEqual(stub.Function, "invoke");
                Assert.AreEqual(stub.Args.Count, 3);
                string aKey = stub.StringArgs[1];
                string bKey = stub.StringArgs[2];

                using (IQueryResultsEnumerable<IKeyValue> stateByRange = stub.GetStateByRange(aKey, bKey))
                {
                    foreach (IKeyValue kv in stateByRange)
                    {
                        //Do Nothing, just enumerate
                    }
                }

                return NewSuccessResponse("OK response2");
            }
        }

        public class Cb3 : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                return NewSuccessResponse("OK response1");
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                string query = stub.StringArgs[1];


                using (IQueryResultsEnumerable<IKeyValue> stateByRange = stub.GetQueryResult(query))
                {
                    foreach (IKeyValue kv in stateByRange)
                    {
                        //Do Nothing, just enumerate
                    }
                }

                return NewSuccessResponse("OK response2");
            }
        }

        public class Cb4 : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                return NewSuccessResponse("OK response1");
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                string key = stub.StringArgs[1];


                using (IQueryResultsEnumerable<IKeyModification> stateByRange = stub.GetHistoryForKey(key))
                {
                    foreach (IKeyModification kv in stateByRange)
                    {
                        //Do Nothing, just enumerate
                    }
                }

                return NewSuccessResponse("OK response2");
            }
        }

        public class Cb5 : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                return NewSuccessResponse("OK response1");
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                Response response = stub.InvokeChaincode("anotherChaincode", new List<byte[]>());
                return NewSuccessResponse("OK response2");
            }
        }

        public class Cb6 : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                return NewErrorResponse("Wrong response1");
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                return NewErrorResponse("Wrong response2");
            }
        }

        public class Cb7 : ChaincodeBase
        {
            public override Response Init(IChaincodeStub stub)
            {
                try
                {
                    Thread.Sleep(10);
                }
                catch (ThreadInterruptedException)
                {
                }

                return NewSuccessResponse();
            }

            public override Response Invoke(IChaincodeStub stub)
            {
                return NewSuccessResponse();
            }
        }
    }
}