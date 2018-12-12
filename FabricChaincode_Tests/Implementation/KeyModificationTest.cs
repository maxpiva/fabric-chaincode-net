/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Shim.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Implementation
{
    [TestClass]
    public class KeyModificationTest
    {
        [TestMethod]
        public void TestKeyModificationImpl()
        {
            var _=new Shim.Implementation.KeyModification(new KeyModification {TxId = "txid", Value = ByteString.CopyFromUtf8("value"), Timestamp = new Timestamp {Nanos = 123456789, Seconds = 1234567890}, IsDelete = true});
        }
        [TestMethod]
        public void TestGetTxId()
        {
            Shim.Implementation.KeyModification km = new Shim.Implementation.KeyModification(new KeyModification {TxId = "txid"});
            Assert.AreEqual(km.TxId, "txid");
        }
        [TestMethod]
        public void TestGetValue()
        {
            Shim.Implementation.KeyModification km = new Shim.Implementation.KeyModification(new KeyModification {Value = ByteString.CopyFromUtf8("value")});
            CollectionAssert.AreEqual(km.Value, "value".ToBytes());
        }

        [TestMethod]
        public void TestGetStringValue()
        {
            Shim.Implementation.KeyModification km = new Shim.Implementation.KeyModification(new KeyModification {Value = ByteString.CopyFromUtf8("value")});
            Assert.AreEqual(km.StringValue, "value");
        }

        [TestMethod]
        public void TestGetTimestamp()
        {
            Shim.Implementation.KeyModification km = new Shim.Implementation.KeyModification(new KeyModification {Timestamp = new Timestamp {Nanos = 123456789, Seconds = 1234567890}});
            DateTime s = new Timestamp {Nanos = 123456789, Seconds = 1234567890}.ToDateTime();
            Assert.AreEqual(km.Timestamp, s);
        }

        [TestMethod]
        public void TestIsDeleted()
        {
            new List<bool> {true, false}.ForEach((b) =>
            {
                Shim.Implementation.KeyModification km = new Shim.Implementation.KeyModification(new KeyModification {IsDelete = b});
                Assert.AreEqual(km.IsDeleted, b);
            });
        }

        [TestMethod]
        public void TestHashCode()
        {
            Shim.Implementation.KeyModification km = new Shim.Implementation.KeyModification(new KeyModification {IsDelete = false});
            
            int expectedHashCode = 31;
            expectedHashCode = expectedHashCode + 1237;
            expectedHashCode = expectedHashCode * 31 + 0;
            expectedHashCode = expectedHashCode * 31 + "".GetHashCode();
            expectedHashCode = expectedHashCode * 31 + ByteString.CopyFromUtf8("").GetHashCode();
            Assert.AreEqual(expectedHashCode, km.GetHashCode(), "Wrong hash code");

        }

        [TestMethod]
        public void TestEquals()
        {
            Shim.Implementation.KeyModification km1 = new Shim.Implementation.KeyModification(new KeyModification { IsDelete = false});
            Shim.Implementation.KeyModification km2 = new Shim.Implementation.KeyModification(new KeyModification { IsDelete = true });
            Shim.Implementation.KeyModification km3 = new Shim.Implementation.KeyModification(new KeyModification { IsDelete = false });

            Assert.IsFalse(km1.Equals(km2));
            Assert.IsTrue(km1.Equals(km3));
        }

    }
}