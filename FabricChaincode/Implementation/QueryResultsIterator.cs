/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
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