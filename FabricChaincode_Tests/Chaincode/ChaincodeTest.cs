using System;
using System.Linq;
using Hyperledger.Fabric.Shim.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Chaincode
{
    [TestClass]
    public class ChaincodeTest
    {
        [TestMethod]
        public void TestResponse()
        {
            Response resp = new Response(Status.SUCCESS, "No message", "no payload".ToBytes());
            Assert.AreEqual(Status.SUCCESS, resp.Status, "Incorrect status");
            Assert.AreEqual("No message", resp.Message, "Incorrect message");
            Assert.AreEqual("no payload", resp.StringPayload, "Incorrect payload");
        }


        [TestMethod]
        public void TestResponseWithCode()
        {
            Response resp = new Response((Status) 200, "No message", "no payload".ToBytes());
            Assert.AreEqual(Status.SUCCESS, resp.Status, "Incorrect status");
            Assert.AreEqual(200, (int) resp.Status, "Incorrect status");
            Assert.AreEqual("No message", resp.Message, "Incorrect message");
            Assert.AreEqual("no payload", resp.StringPayload, "Incorrect payload");

            resp = new Response((Status) 404, "No message", "no payload".ToBytes());
            Assert.AreEqual(404, (int) resp.Status, "Incorrect status");
            Assert.AreEqual("No message", resp.Message, "Incorrect message");
            Assert.AreEqual("no payload", resp.StringPayload, "Incorrect payload");

            resp = new Response(Status.ERROR_THRESHOLD, "No message", "no payload".ToBytes());
            Assert.AreEqual(Status.ERROR_THRESHOLD, resp.Status, "Incorrect status");
            Assert.AreEqual(400, (int) resp.Status, "Incorrect status");
            Assert.AreEqual("No message", resp.Message, "Incorrect message");
            Assert.AreEqual("no payload", resp.StringPayload, "Incorrect payload");
        }

        [TestMethod]
        public void TestStatus()
        {
            Assert.AreEqual(Status.SUCCESS, (Status) 200, "Wrong status");
            Assert.AreEqual(Status.ERROR_THRESHOLD, (Status) 400, "Wrong status");
            Assert.AreEqual(Status.INTERNAL_SERVER_ERROR, (Status) 500, "Wrong status");
            Status badstatus = (Status) 501;
            Assert.IsFalse(Enum.GetValues(typeof(Status)).Cast<Status>().Contains(badstatus));
        }
    }
}