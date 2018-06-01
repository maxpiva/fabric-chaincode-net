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

using Hyperledger.Fabric.Shim.Ledger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Ledger
{
    [TestClass]
    public class CompositeKeyTest
    {
        [TestMethod]
        public void TestCompositeKeyStringStringArray()
        {
            CompositeKey key = new CompositeKey("abc", "def", "ghi", "jkl", "mno");
            Assert.AreEqual(key.ObjectType, "abc");
            Assert.AreEqual(key.Attributes.Count, 4);
            Assert.AreEqual(key.ToString(), "abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        public void TestCompositeKeyStringListOfString()
        {
            CompositeKey key = new CompositeKey("abc", new string[] {"def", "ghi", "jkl", "mno"});
            Assert.AreEqual(key.ObjectType, "abc");
            Assert.AreEqual(key.Attributes.Count, 4);
            Assert.AreEqual(key.ToString(), "abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        [ExpectedException(typeof(CompositeKeyFormatException))]
        public void TestCompositeKeyWithInvalidObjectTypeDelimiter()
        {
            var _=new CompositeKey("ab\u0000c", new string[] {"def", "ghi", "jkl", "mno"});
        }

        [TestMethod]
        [ExpectedException(typeof(CompositeKeyFormatException))]
        public void TestCompositeKeyWithInvalidAttributeDelimiter()
        {
            var _ = new CompositeKey("abc", new string[] {"def", "ghi", "j\u0000kl", "mno"});
        }

        [TestMethod]
        [ExpectedException(typeof(CompositeKeyFormatException))]
        public void TestCompositeKeyWithInvalidObjectTypeMaxCodePoint()
        {
            var _ = new CompositeKey("ab\udbff\udfffc", new string[] {"def", "ghi", "jkl", "mno"});
        }

        [TestMethod]
        [ExpectedException(typeof(CompositeKeyFormatException))]
        public void TestCompositeKeyWithInvalidAttributeMaxCodePoint()
        {
            var _ = new CompositeKey("abc", new string[] {"def", "ghi", "jk\udbff\udfffl", "mno"});
        }

        [TestMethod]
        public void TestGetObjectType()
        {
            CompositeKey key = new CompositeKey("abc", new string[] {"def", "ghi", "jkl", "mno"});
            Assert.AreEqual(key.ObjectType, "abc");
        }

        [TestMethod]
        public void TestGetAttributes()
        {
            CompositeKey key = new CompositeKey("abc", new string[] {"def", "ghi", "jkl", "mno"});
            Assert.AreEqual(key.ObjectType, "abc");
            Assert.AreEqual(key.Attributes.Count, 4);
            CollectionAssert.AreEquivalent(key.Attributes, new string[] {"def", "ghi", "jkl", "mno"});
        }

        [TestMethod]
        public void TestToString()
        {
            CompositeKey key = new CompositeKey("abc", new string[] {"def", "ghi", "jkl", "mno"});
            Assert.AreEqual(key.ToString(), "abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        public void TestParseCompositeKey()
        {
            CompositeKey key = CompositeKey.ParseCompositeKey("abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
            Assert.AreEqual(key.ObjectType, "abc");
            Assert.AreEqual(key.Attributes.Count, 4);
            CollectionAssert.AreEquivalent(key.Attributes, new string[] {"def", "ghi", "jkl", "mno"});
            Assert.AreEqual(key.ToString(), "abc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        [ExpectedException(typeof(CompositeKeyFormatException))]
        public void TestParseCompositeKeyInvalidObjectType()
        {
            var _ = CompositeKey.ParseCompositeKey("ab\udbff\udfffc\u0000def\u0000ghi\u0000jkl\u0000mno\u0000");
        }

        [TestMethod]
        [ExpectedException(typeof(CompositeKeyFormatException))]
        public void TestParseCompositeKeyInvalidAttribute()
        {
            var _ = CompositeKey.ParseCompositeKey("abc\u0000def\u0000ghi\u0000jk\udbff\udfffl\u0000mno\u0000");
        }
    }
}