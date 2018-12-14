/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Implementation
{
    [TestClass]
    public class AsyncQueryResultsEnumerableTest
    {



        [TestMethod]
        public void TestGetMetadata()
        {
            AsyncQueryResultsEnumerable<int> testIter = new AsyncQueryResultsEnumerable<int>(null, "", "", PrepareQueryResponseAsync, (qv) => 0);
            Assert.AreEqual(testIter.GetMetadataAsync().RunAndUnwrap().Bookmark, "asdf");
            Assert.AreEqual(testIter.GetMetadataAsync().RunAndUnwrap().FetchedRecordsCount, 2);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidProtocolBufferException))]
        public void TestGetInvalidMetadata()
        {
            AsyncQueryResultsEnumerable<int> testIter = new AsyncQueryResultsEnumerable<int>(null, "", "", PrepareQueryResponseWrongMetaAsync, (qv) => 0);
            testIter.GetMetadataAsync().RunAndUnwrap();
            Assert.Fail("Expected bad constructed metadata");
        }

        private Task<QueryResponse> PrepareQueryResponseAsync(CancellationToken token)
        {
            QueryResponseMetadata qrm = new QueryResponseMetadata {Bookmark = "asdf", FetchedRecordsCount = 2};
            return Task.FromResult(new QueryResponse {HasMore = false, Metadata = qrm.ToByteString()});
        }

        private Task<QueryResponse> PrepareQueryResponseWrongMetaAsync(CancellationToken token)
        {
            ByteString bs = ByteString.CopyFrom(new byte[] {0, 0});

            return Task.FromResult(new QueryResponse {HasMore = false, Metadata = bs});
        }
    }
}
