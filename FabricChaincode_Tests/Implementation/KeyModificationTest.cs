/*
Copyright IBM 2017 All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

         http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
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
    }
}