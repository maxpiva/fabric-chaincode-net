using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hyperledger.Fabric.Protos.Common;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Protos.Peer.ProposalPackage;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Implementation;
using Hyperledger.Fabric.Shim.Ledger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using KeyModification = Hyperledger.Fabric.Protos.Ledger.QueryResult.KeyModification;

namespace Hyperledger.Fabric.Shim.Tests.Implementation
{
    [TestClass]
    public class ChaincodeStubTest
    {
        private static readonly string TEST_COLLECTION = "testcoll";

        private readonly Mock<Handler> handler = new Mock<Handler>(MockBehavior.Loose);

        [TestMethod]
        public void TestGetArgs()
        {
            List<ByteString> args = new List<ByteString>() {ByteString.CopyFromUtf8("arg0"), ByteString.CopyFromUtf8("arg1"), ByteString.CopyFromUtf8("arg2")};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, args, null);
            Assert.That.ContainsArray(stub.Args, args.Select(a => a.ToByteArray()));
        }

        [TestMethod]
        public void TestGetStringArgs()
        {
            List<ByteString> args = new List<ByteString>() {ByteString.CopyFromUtf8("arg0"), ByteString.CopyFromUtf8("arg1"), ByteString.CopyFromUtf8("arg2")};

            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, args, null);
            Assert.That.Contains(stub.StringArgs, args.Select(a => a.ToStringUtf8()));
        }

        [TestMethod]
        public void TestGetFunction()
        {
            List<ByteString> args = new List<ByteString>() {ByteString.CopyFromUtf8("function"), ByteString.CopyFromUtf8("arg0"), ByteString.CopyFromUtf8("arg1")};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, args, null);
            Assert.AreEqual(stub.Function, "function");
        }

        [TestMethod]
        public void TestGetParameters()
        {
            List<ByteString> args = new List<ByteString>() {ByteString.CopyFromUtf8("function"), ByteString.CopyFromUtf8("arg0"), ByteString.CopyFromUtf8("arg1")};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, args, null);
            Assert.That.Contains(stub.Parameters, new List<string> {"arg0", "arg1"});
        }

        [TestMethod]
        public void TestSetGetEvent()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            byte[] payload = new byte[] {0x10, 0x20, 0x20};
            string eventName = "event_name";
            stub.SetEvent(eventName, payload);
            ChaincodeEvent evnt = stub.Event;
            Assert.AreEqual(evnt.EventName, eventName);
            Assert.AreEqual(evnt.Payload, ByteString.CopyFrom(payload));
            stub.SetEvent(eventName, null);
            evnt = stub.Event;
            Assert.IsNotNull(evnt);
            Assert.AreEqual(evnt.EventName, eventName);
            Assert.AreEqual(evnt.Payload, ByteString.CopyFrom(new byte[0]));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void testSetEventEmptyName()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            stub.SetEvent("", new byte[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSetEventNullName()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            stub.SetEvent(null, new byte[0]);
        }

        [TestMethod]
        public void TestGetTxId()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            Assert.AreEqual(stub.TxId, "txId");
        }

        [TestMethod]
        public void TestGetState()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            byte[] value = new byte[] {0x10, 0x20, 0x30};
            handler.Setup((a) => a.GetState("myc", "txId", "", "key")).Returns(ByteString.CopyFrom(value));
            CollectionAssert.AreEqual(stub.GetState("key"), value);
        }

        [TestMethod]
        public void TestGetStringState()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            string value = "TEST";
            handler.Setup((a) => a.GetState("myc", "txId", "", "key")).Returns(ByteString.CopyFromUtf8(value));
            Assert.AreEqual(stub.GetStringState("key"), value);
        }

        [TestMethod]
        public void TestPutState()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            byte[] value = new byte[] {0x10, 0x20, 0x30};
            stub.PutState("key", value);
            handler.Verify((a) => a.PutState("myc", "txId", "", "key", ByteString.CopyFrom(value)));
            try
            {
                stub.PutState(null, value);
                Assert.Fail("Null key check fails");
            }
            catch (ArgumentException)
            {
                //Ignore
            }

            try
            {
                stub.PutState("", value);
                Assert.Fail("Empty key check fails");
            }
            catch (ArgumentException)
            {
                //Ignore
            }
        }

        [TestMethod]
        public void TestStringState()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            string value = "TEST";
            stub.PutStringState("key", value);
            handler.Verify((a) => a.PutState("myc", "txId", "", "key", ByteString.CopyFromUtf8(value)));
        }

        [TestMethod]
        public void TestDelState()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            stub.DelState("key");
            handler.Verify((a) => a.DeleteState("myc", "txId", "", "key"));
        }

        [TestMethod]
        public void TestGetStateByRange()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            string startKey = "START";
            string endKey = "END";
            KV[] keyValues = new KV[] {new KV {Key = "A", Value = ByteString.CopyFromUtf8("Value of A")}, new KV {Key = "B", Value = ByteString.CopyFromUtf8("Value of B")}};
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[0].ToByteString()});
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[1].ToByteString()});
            handler.Setup((a) => a.GetStateByRange("myc", "txId", "", startKey, endKey)).Returns(value);
            Assert.That.Contains(stub.GetStateByRange(startKey, endKey), keyValues.Select(a => new KeyValue(a)));
        }

        [TestMethod]
        public void TestGetStateByPartialCompositeKey()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();
            stub.GetStateByPartialCompositeKey("KEY");
            string key = new CompositeKey("KEY").ToString();
            handler.Verify(a => a.GetStateByRange("myc", "txId", "", key, key + "\udbff\udfff"));
            stub.GetStateByPartialCompositeKey("");
            key = new CompositeKey("").ToString();
            handler.Verify(a => a.GetStateByRange("myc", "txId", "", key, key + "\udbff\udfff"));
        }

        [TestMethod]
        public void TestGetStateByPartialCompositeKey_withAttributesAsString()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();
            CompositeKey cKey = new CompositeKey("KEY", "attr1", "attr2");
            stub.GetStateByPartialCompositeKey(cKey.ToString());
            handler.Verify(a => a.GetStateByRange("myc", "txId", "", cKey.ToString(), cKey.ToString() + "\udbff\udfff"));
        }

        [TestMethod]
        public void TestGetStateByPartialCompositeKey_withAttributesWithSplittedParams()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();
            CompositeKey cKey = new CompositeKey("KEY", "attr1", "attr2", "attr3");
            stub.GetStateByPartialCompositeKey("KEY", "attr1", "attr2", "attr3");
            handler.Verify(a => a.GetStateByRange("myc", "txId", "", cKey.ToString(), cKey.ToString() + "\udbff\udfff"));
        }

        [TestMethod]
        public void testGetStateByPartialCompositeKey_withCompositeKey()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();

            CompositeKey key = new CompositeKey("KEY");
            stub.GetStateByPartialCompositeKey(key);
            handler.Verify(a => a.GetStateByRange("myc", "txId", "", key.ToString(), key.ToString() + "\udbff\udfff"));
            key = new CompositeKey("");
            stub.GetStateByPartialCompositeKey(key);
            handler.Verify(a => a.GetStateByRange("myc", "txId", "", key.ToString(), key.ToString() + "\udbff\udfff"));
        }

        private ChaincodeStub PrepareStubAndMockHandler()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            KV[] keyValues = new KV[] {new KV {Key = "A", Value = ByteString.CopyFromUtf8("Value of A")}, new KV {Key = "B", Value = ByteString.CopyFromUtf8("Value of B")}};
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[0].ToByteString()});
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[1].ToByteString()});
            handler.Setup((a) => a.GetStateByRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(value);
            return stub;
        }

        [TestMethod]
        public void TestCreateCompositeKey()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            CompositeKey key = stub.CreateCompositeKey("abc", "def", "ghi", "jkl", "mno");
            Assert.AreEqual(key.ObjectType, "abc");
            Assert.AreEqual(key.Attributes.Count, 4);
            Assert.AreEqual(key.ToString(), "\u0000abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        public void TestSplitCompositeKey()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            CompositeKey key = stub.SplitCompositeKey("\u0000abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
            Assert.AreEqual(key.ObjectType, "abc");
            Assert.That.Contains(key.Attributes, new List<string> {"def", "ghi", "jkl", "mno"});
            Assert.AreEqual(key.ToString(), "\u0000abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        public void testGetQueryResult()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            KV[] keyValues = new KV[] {new KV {Key = "A", Value = ByteString.CopyFromUtf8("Value of A")}, new KV {Key = "B", Value = ByteString.CopyFromUtf8("Value of B")}};
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[0].ToByteString()});
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[1].ToByteString()});
            handler.Setup((a) => a.GetQueryResult("myc", "txId", "", "QUERY")).Returns(value);
            Assert.That.Contains(stub.GetQueryResult("QUERY"), keyValues.Select(a => new KeyValue(a)));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidProtocolBufferException))]
        public void TestGetQueryResultWithException()
        {
            string txId = "txId", query = "QUERY", channelId = "myc";
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = ByteString.CopyFromUtf8("exception")});

            handler.Setup((a) => a.GetQueryResult(channelId, txId, "", query)).Returns(value);
            try
            {
                stub.GetQueryResult(query).First();
            }
            catch (Exception e)
            {
                if (e is InvalidProtocolBufferException)
                    throw;
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }

        [TestMethod]
        public void TestGetHistoryForKey()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            KeyModification[] keyValues = new KeyModification[] {new KeyModification {TxId = "tx0", Value = ByteString.CopyFromUtf8("Value A"), Timestamp = new Timestamp()}, new KeyModification {TxId = "tx1", Value = ByteString.CopyFromUtf8("Value B"), Timestamp = new Timestamp()}};
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[0].ToByteString()});
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[1].ToByteString()});

            handler.Setup((a) => a.GetHistoryForKey("myc", "txId", "KEY")).Returns(value);
            Assert.That.Contains(stub.GetHistoryForKey("KEY"), keyValues.Select(a => new Shim.Implementation.KeyModification(a)));
        }

        [TestMethod]
        public void TestGetPrivateData()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            byte[] value = new byte[] {0x10, 0x20, 0x30};
            handler.Setup(a => a.GetState("myc", "txId", "testcoll", "key")).Returns(ByteString.CopyFrom(value));
            CollectionAssert.AreEqual(stub.GetPrivateData("testcoll", "key"), value);
            try
            {
                stub.GetPrivateData(null, "key");
                Assert.Fail("Null collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.GetPrivateData("", "key");
                Assert.Fail("Empty collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }
        }

        [TestMethod]
        public void TestGetStringPrivateData()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            string value = "TEST";
            handler.Setup(a => a.GetState("myc", "txId", "testcoll", "key")).Returns(ByteString.CopyFromUtf8(value));
            Assert.AreEqual(stub.GetPrivateDataUTF8("testcoll", "key"), value);
        }

        [TestMethod]
        public void TestPutPrivateData()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            byte[] value = new byte[] {0x10, 0x20, 0x30};
            stub.PutPrivateData("testcoll", "key", value);
            handler.Verify(a => a.PutState("myc", "txId", "testcoll", "key", ByteString.CopyFrom(value)));
            try
            {
                stub.PutPrivateData(null, "key", value);
                Assert.Fail("Null collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.PutPrivateData("", "key", value);
                Assert.Fail("Empty collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.PutPrivateData("testcoll", null, value);
                Assert.Fail("Null key check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.PutPrivateData("testcoll", "", value);
                Assert.Fail("Empty key check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }
        }

        [TestMethod]
        public void TestPutStringPrivateData()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            string value = "TEST";
            stub.PutPrivateData("testcoll", "key", value);
            handler.Verify(a => a.PutState("myc", "txId", "testcoll", "key", ByteString.CopyFromUtf8(value)));
        }

        [TestMethod]
        public void TestDelPrivateState()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            stub.DelPrivateData("testcoll", "key");
            handler.Verify(a => a.DeleteState("myc", "txId", "testcoll", "key"));
            try
            {
                stub.DelPrivateData(null, "key");
                Assert.Fail("Null collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.DelPrivateData("", "key");
                Assert.Fail("Empty collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }
        }

        [TestMethod]
        public void TestGetPrivateDataByRange()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            string startKey = "START";
            string endKey = "END";
            KV[] keyValues = new KV[] {new KV {Key = "A", Value = ByteString.CopyFromUtf8("Value of A")}, new KV {Key = "B", Value = ByteString.CopyFromUtf8("Value of B")}};
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[0].ToByteString()});
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[1].ToByteString()});
            handler.Setup(a => a.GetStateByRange("myc", "txId", "testcoll", startKey, endKey)).Returns(value);
            Assert.That.Contains(stub.GetPrivateDataByRange("testcoll", startKey, endKey), keyValues.Select(a => new KeyValue(a)));

            try
            {
                stub.GetPrivateDataByRange(null, startKey, endKey);
                Assert.Fail("Null collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.GetPrivateDataByRange("", startKey, endKey);
                Assert.Fail("Empty collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }
        }

        [TestMethod]
        public void TestGetPrivateDataByPartialCompositeKey()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();

            CompositeKey key = new CompositeKey("KEY");
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, "KEY");
            handler.Verify(a => a.GetStateByRange("myc", "txId", TEST_COLLECTION, key.ToString(), key.ToString() + "\udbff\udfff"));
            key = new CompositeKey("");
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, (string) null);
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, "");
            handler.Verify(a => a.GetStateByRange("myc", "txId", TEST_COLLECTION, key.ToString(), key.ToString() + "\udbff\udfff"), Times.AtLeast(2));
        }

        [TestMethod]
        public void TestGetPrivateDataByPartialCompositeKey_withAttributesAsString()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();
            CompositeKey cKey = new CompositeKey("KEY", "attr1", "attr2");
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, cKey.ToString());

            handler.Verify(a => a.GetStateByRange("myc", "txId", TEST_COLLECTION, cKey.ToString(), cKey.ToString() + "\udbff\udfff"));
        }

        [TestMethod]
        public void testGetPrivateDataByPartialCompositeKey_withAttributesWithSplittedParams()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();
            CompositeKey cKey = new CompositeKey("KEY", "attr1", "attr2", "attr3");
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, "KEY", "attr1", "attr2", "attr3");
            handler.Verify(a => a.GetStateByRange("myc", "txId", TEST_COLLECTION, cKey.ToString(), cKey.ToString() + "\udbff\udfff"));
        }

        [TestMethod]
        public void TestGetPrivateDataByPartialCompositeKey_withCompositeKey()
        {
            ChaincodeStub stub = PrepareStubAndMockHandler();

            CompositeKey key = new CompositeKey("KEY");
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, key);
            handler.Verify(a => a.GetStateByRange("myc", "txId", TEST_COLLECTION, key.ToString(), key.ToString() + "\udbff\udfff"));

            key = new CompositeKey("");
            stub.GetPrivateDataByPartialCompositeKey(TEST_COLLECTION, key);
            handler.Verify(a => a.GetStateByRange("myc", "txId", TEST_COLLECTION, key.ToString(), key.ToString() + "\udbff\udfff"));
        }

        [TestMethod]
        public void TestGetPrivateDataQueryResult()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            KV[] keyValues = new KV[] {new KV {Key = "A", Value = ByteString.CopyFromUtf8("Value of A")}, new KV {Key = "B", Value = ByteString.CopyFromUtf8("Value of B")}};
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[0].ToByteString()});
            value.Results.Add(new QueryResultBytes {ResultBytes = keyValues[1].ToByteString()});
            handler.Setup(a => a.GetQueryResult("myc", "txId", "testcoll", "QUERY")).Returns(value);

            Assert.That.Contains(stub.GetPrivateDataQueryResult("testcoll", "QUERY"), keyValues.Select(a => new KeyValue(a)));

            try
            {
                stub.GetPrivateDataQueryResult(null, "QUERY");
                Assert.Fail("Null collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }

            try
            {
                stub.GetPrivateDataQueryResult("", "QUERY");
                Assert.Fail("Empty collection check fails");
            }
            catch (ArgumentException)
            {
                //ignored
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidProtocolBufferException))]
        public void TestGetPrivateDataQueryResultWithException()
        {
            string txId = "txId", query = "QUERY", channelId = "myc";
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = ByteString.CopyFromUtf8("exception")});
            handler.Setup(a => a.GetQueryResult(channelId, txId, "testcoll", query)).Returns(value);
            try
            {
                stub.GetPrivateDataQueryResult("testcoll", query).First();
            }
            catch (Exception e)
            {
                if (e is InvalidProtocolBufferException)
                    throw;
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidProtocolBufferException))]
        public void TestGetHistoryForKeyWithException()
        {
            string txId = "txId", key = "KEY", channelId = "myc";
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            QueryResponse value = new QueryResponse {HasMore = false};
            value.Results.Add(new QueryResultBytes {ResultBytes = ByteString.CopyFromUtf8("exception")});

            handler.Setup(a => a.GetHistoryForKey(channelId, txId, key)).Returns(value);
            try
            {
                stub.GetHistoryForKey(key).First();
            }
            catch (Exception e)
            {
                if (e is InvalidProtocolBufferException)
                    throw;
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }

        [TestMethod]
        public void TestInvokeChaincode()
        {
            string txId = "txId", chaincodeName = "CHAINCODE_ID", channel = "CHAINCODE_CHANNEL";
            ChaincodeStub stub = new ChaincodeStub(channel, txId, handler.Object, new List<ByteString>(), null);
            Response expectedResponse = new Response(Status.SUCCESS, "MESSAGE", "PAYLOAD".ToBytes());
            handler.Setup(a => a.InvokeChaincode(channel, txId, chaincodeName, new List<byte[]>())).Returns(expectedResponse);
            Assert.AreEqual(stub.InvokeChaincode(chaincodeName, new List<byte[]>()), expectedResponse);
            handler.Setup(a => a.InvokeChaincode(It.Is<string>(b => b == channel), It.Is<string>(b => b == txId), It.Is<string>(b => b == chaincodeName + "/" + channel), It.IsAny<List<byte[]>>())).Returns(expectedResponse);
            Assert.AreEqual(stub.InvokeChaincode(chaincodeName, new List<byte[]>(), channel), expectedResponse);
        }

        [TestMethod]
        public void TestInvokeChaincodeWithStringArgs()
        {
            string txId = "txId", chaincodeName = "CHAINCODE_ID", channel = "CHAINCODE_CHANNEL";
            ChaincodeStub stub = new ChaincodeStub(channel, txId, handler.Object, new List<ByteString>(), null);
            Response expectedResponse = new Response(Status.SUCCESS, "MESSAGE", "PAYLOAD".ToBytes());

            handler.Setup(a => a.InvokeChaincode(channel, txId, chaincodeName, new List<byte[]>())).Returns(expectedResponse);
            Assert.AreEqual(stub.InvokeChaincodeWithStringArgs(chaincodeName), expectedResponse);

            handler.Setup(a => a.InvokeChaincode(channel, txId, chaincodeName, new List<byte[]>())).Returns(expectedResponse);
            Assert.AreEqual(stub.InvokeChaincodeWithStringArgs(chaincodeName, new List<string>()), expectedResponse);

            handler.Setup(a => a.InvokeChaincode(It.Is<string>(b => b == channel), It.Is<string>(b => b == txId), It.Is<string>(b => b == chaincodeName + "/" + channel), It.IsAny<List<byte[]>>())).Returns(expectedResponse);
            Assert.AreEqual(stub.InvokeChaincodeWithStringArgs(chaincodeName, new List<string>(), channel), expectedResponse);
        }

        [TestMethod]
        public void TestGetSignedProposal()
        {
            SignedProposal signedProposal = new SignedProposal {ProposalBytes = new Proposal {Header = new Header {ChannelHeader = new ChannelHeader {Type = (int) HeaderType.EndorserTransaction, Timestamp = new Timestamp()}.ToByteString()}.ToByteString()}.ToByteString()};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            Assert.AreEqual(stub.SignedProposal, signedProposal);
        }

        [TestMethod]
        public void TestGetSignedProposalWithEmptyProposal()
        {
            SignedProposal signedProposal = new SignedProposal {ProposalBytes = ByteString.Empty};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            Assert.AreEqual(stub.SignedProposal, signedProposal);
        }

        [TestMethod]
        public void TestGetTxTimestamp()
        {
            DateTimeOffset? instant = DateTimeOffset.Now;
            Timestamp timestamp = Timestamp.FromDateTimeOffset(instant.Value);
            SignedProposal signedProposal = new SignedProposal {ProposalBytes = new Proposal {Header = new Header {ChannelHeader = new ChannelHeader {Type = (int) HeaderType.EndorserTransaction, Timestamp = timestamp}.ToByteString()}.ToByteString()}.ToByteString()};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            Assert.AreEqual(stub.TxTimestamp, instant);
        }

        [TestMethod]
        public void TestGetTxTimestampNullSignedProposal()
        {
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), null);
            Assert.IsNull(stub.TxTimestamp);
        }

        [TestMethod]
        public void TestGetTxTimestampEmptySignedProposal()
        {
            SignedProposal signedProposal = new SignedProposal {ProposalBytes = ByteString.Empty};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            Assert.IsNull(stub.TxTimestamp);
        }

        [TestMethod]
        public void TestGetCreator()
        {
            DateTimeOffset? instant = DateTimeOffset.Now;

            byte[] creator = "CREATOR".ToBytes();
            Timestamp timestamp = Timestamp.FromDateTimeOffset(instant.Value);
            SignedProposal signedProposal = new SignedProposal {ProposalBytes = new Proposal {Header = new Header {ChannelHeader = new ChannelHeader {Type = (int) HeaderType.EndorserTransaction, Timestamp = timestamp}.ToByteString(), SignatureHeader = new SignatureHeader {Creator = ByteString.CopyFrom(creator)}.ToByteString()}.ToByteString(),}.ToByteString()};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            CollectionAssert.AreEqual(stub.Creator, creator);
        }

        [TestMethod]
        public void testGetTransient()
        {
            ChaincodeProposalPayload payload = new ChaincodeProposalPayload();
            payload.TransientMap.Add("key0", ByteString.CopyFromUtf8("value0"));
            payload.TransientMap.Add("key1", ByteString.CopyFromUtf8("value1"));

            SignedProposal signedProposal = new SignedProposal {ProposalBytes = new Proposal {Header = new Header {ChannelHeader = new ChannelHeader {Type = (int) HeaderType.EndorserTransaction, Timestamp = new Timestamp()}.ToByteString(),}.ToByteString(), Payload = payload.ToByteString()}.ToByteString()};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            Assert.That.ContainsDictionary<string,byte[],byte>(stub.Transient, new Dictionary<string, byte[]> {{"key0", "value0".ToBytes()}, {"key1", "value1".ToBytes()}});
        }

        [TestMethod]
        public void TestGetBinding()
        {
            byte[] expectedDigest = "5093dd4f4277e964da8f4afbde0a9674d17f2a6a5961f0670fc21ae9b67f2983".FromHexString();

            SignedProposal signedProposal = new SignedProposal {ProposalBytes = new Proposal {Header = new Header {ChannelHeader = new ChannelHeader {Type = (int) HeaderType.EndorserTransaction, Timestamp = new Timestamp(), Epoch = 10}.ToByteString(), SignatureHeader = new SignatureHeader {Nonce = ByteString.CopyFromUtf8("nonce"), Creator = ByteString.CopyFromUtf8("creator")}.ToByteString()}.ToByteString(),}.ToByteString()};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            CollectionAssert.AreEqual(stub.Binding, expectedDigest);
        }

        [TestMethod]
        public void TestGetBindingEmptyProposal()
        {
            SignedProposal signedProposal = new SignedProposal {ProposalBytes = ByteString.Empty};
            ChaincodeStub stub = new ChaincodeStub("myc", "txId", handler.Object, new List<ByteString>(), signedProposal);
            Assert.IsNull(stub.Binding);
        }
    }
}