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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class QueryResultsIterator<T> : IQueryResultsIterator<T>
    {
        private readonly string channelId;

        private readonly Handler handler;
        private readonly string txId;
        private List<QueryResultBytes> currentIterator;
        private QueryResponse currentQueryResponse;
        private bool disposed;
        private readonly Func<QueryResultBytes, T> mapper;

        public QueryResultsIterator(Handler handler, string channelId, string txId, QueryResponse queryResponse, Func<QueryResultBytes, T> mapper)
        {
            this.handler = handler;
            this.channelId = channelId;
            this.txId = txId;
            currentQueryResponse = queryResponse;
            currentIterator = currentQueryResponse.Results.ToList();
            this.mapper = mapper;
        }


        public IEnumerator<T> GetEnumerator()
        {
            do
            {
                for (int x = 0; x < currentIterator.Count; x++)
                    yield return mapper(currentIterator[x]);
                if (currentQueryResponse.HasMore)
                {
                    currentQueryResponse = handler.QueryStateNext(channelId, txId, currentQueryResponse.Id);
                    currentIterator = currentQueryResponse.Results.ToList();
                }
                else
                    currentIterator = new List<QueryResultBytes>();
            } while (currentIterator.Count > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                handler.QueryStateClose(channelId, txId, currentQueryResponse.Id);
                currentIterator = new List<QueryResultBytes>();
                currentQueryResponse = new QueryResponse {HasMore = false};
            }
        }
    }
}