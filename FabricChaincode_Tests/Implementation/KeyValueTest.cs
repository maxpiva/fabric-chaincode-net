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

using Google.Protobuf;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Implementation
{
    [TestClass]
    public class KeyValueTest
    {
        [TestMethod]
        public void TestKeyValueImpl()
        {
            var _=new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
        }

        [TestMethod]
        public void TestGetKey()
        {
            KeyValue kv = new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
            Assert.AreEqual(kv.Key, "key");
        }


        [TestMethod]
        public void TestGetValue()
        {
            KeyValue kv = new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
            CollectionAssert.AreEqual(kv.Value, "value".ToBytes());
        }

        [TestMethod]
        public void TestGetStringValue()
        {
            KeyValue kv = new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
            Assert.AreEqual(kv.StringValue, "value");
        }

        [TestMethod]
        public void TestHashCode()
        {
            KeyValue kv = new KeyValue(new KV());

            int expectedHashCode = 31;
            expectedHashCode = expectedHashCode + "".GetHashCode();
            expectedHashCode = expectedHashCode * 31 + ByteString.CopyFromUtf8("").GetHashCode();            
            Assert.AreEqual(expectedHashCode, kv.GetHashCode(), "Wrong hashcode");
        }

        [TestMethod]
        public void TestEquals()
        {
            KeyValue kv1 = new KeyValue(new KV { Key = "a", Value = ByteString.CopyFromUtf8("valueA") });
            KeyValue kv2 = new KeyValue(new KV { Key = "a", Value = ByteString.CopyFromUtf8("valueB") });
            KeyValue kv3 = new KeyValue(new KV { Key = "b", Value = ByteString.CopyFromUtf8("valueA") });
            KeyValue kv4 = new KeyValue(new KV { Key = "a", Value = ByteString.CopyFromUtf8("valueA") });

            Assert.IsFalse(kv1.Equals(kv2));
            Assert.IsFalse(kv2.Equals(kv3));
            Assert.IsTrue(kv1.Equals(kv4));

        }
    }
}