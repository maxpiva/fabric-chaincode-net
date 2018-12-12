using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;

namespace Hyperledger.Fabric.Shim.Tests
{
    [TestClass]
    public class BlockingCollectionTest
    {
        private BlockingCollection<int> testChannel = new BlockingCollection<int>();

        [TestMethod]
        public void TestChannel()
        {
            testChannel.Add(1);
            testChannel.Add(2);
            Assert.AreEqual((long) 1, (long) testChannel.Take(), "Wrong item come out the channel");
            testChannel.Dispose();
            try
            {
                testChannel.Take();
                Assert.Fail("Failed, take after closing");
            }
            catch (ObjectDisposedException)
            {
                //ignore
            }
            try
            {
                testChannel.Add(1);
                Assert.Fail("Failed, add after closing");
            }
            catch (ObjectDisposedException)
            {
                //ignore
            }
   
        }
    }
}

