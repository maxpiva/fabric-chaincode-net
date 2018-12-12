using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Ledger;
#pragma warning disable 693

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class AsyncQueryResultsEnumerable<T> : IAsyncQueryResultsEnumerable<T> where T : class
    {
        //Full async (need C# 8)
        //Support caching (multiple enumerators can consume, without re-requesting the peer).
        //usage using await foreach
        private readonly Cache<T> cache;

        private bool disposed;

        public AsyncQueryResultsEnumerable(Handler handler, string channelId, string txId, Func<CancellationToken, Task<QueryResponse>> query, Func<QueryResultBytes, T> mapper)
        {
            cache = new Cache<T>(new AsyncEnumerator<T>(handler, channelId, txId, query, mapper));
        }

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new CachedEnumerator<T>(cache);
        }



        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                cache?.Dispose();
            }
        }

        public IQueryResultsEnumerable<T> ToSyncEnumerable()
        {
            return new QueryResultEnumerable<T>(this);
        }

        internal class QueryResultEnumerable<T> : IQueryResultsEnumerable<T> where T : class
        {
            private readonly AsyncQueryResultsEnumerable<T> original;


            public QueryResultEnumerable(AsyncQueryResultsEnumerable<T> original)
            {
                this.original = original;
            }

            public IEnumerator<T> GetEnumerator()
            {
                IAsyncEnumerator<T> enumerator = original.GetEnumerator();
                while (enumerator.MoveNext().RunAndUnwrap())
                {
                    yield return enumerator.Current;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Dispose()
            {
                original?.Dispose();
            }
        }

        internal class Cache<T> : IDisposable where T : class
        {
            private int currentpos = -1;
            private readonly AsyncEnumerator<T> en;
            private readonly List<T> Obtained = new List<T>();

            public Cache(AsyncEnumerator<T> en)
            {
                this.en = en;
            }

            public void Dispose()
            {
                en?.Dispose();
            }

            public async Task<T> Get(int pos, CancellationToken cancellationToken)
            {
                if (pos == -1)
                    return null;
                if (Obtained.Count < pos)
                    return Obtained[pos];
                while (currentpos < pos)
                {
                    if (await en.MoveNext(cancellationToken))
                    {
                        Obtained.Add(en.Current);
                        currentpos++;
                    }
                    else
                        return null;
                }

                return Obtained[pos];
            }
        }

        internal class CachedEnumerator<T> : IAsyncEnumerator<T> where T : class
        {
            private readonly Cache<T> cache;
            private int cnt = -1;

            public CachedEnumerator(Cache<T> c)
            {
                cache = c;
            }

            public void Dispose()
            {
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                cnt++;
                Current = await cache.Get(cnt, cancellationToken);
                return Current != null;
            }

            public T Current { get; private set; }
        }

        internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly string channelId;
            private readonly Handler handler;
            private readonly Func<QueryResultBytes, T> mapper;
            private readonly Func<CancellationToken, Task<QueryResponse>> query;
            private readonly string txId;
            private QueryResultBytes[] currentIterator;
            private int currentpos = -1;
            private QueryResponse currentQueryResponse;
            private bool disposed;

            public AsyncEnumerator(Handler handler, string channelId, string txId, Func<CancellationToken, Task<QueryResponse>> query, Func<QueryResultBytes, T> mapper)
            {
                this.handler = handler;
                this.channelId = channelId;
                this.txId = txId;
                this.query = query;
                this.mapper = mapper;
            }

            public void Dispose() //Microsoft, where is the async Disposable?
            {
                if (!disposed)
                {
                    disposed = true;
                    if (currentIterator == null)
                        return;
                    handler.QueryStateCloseAsync(channelId, txId, currentQueryResponse.Id).RunAndUnwrap();
                    currentIterator = null;
                    currentQueryResponse = new QueryResponse {HasMore = false};
                }
            }

            public async Task<bool> MoveNext(CancellationToken token)
            {
                if (currentIterator == null)
                {
                    currentpos = -1;
                    currentQueryResponse = await query(token).ConfigureAwait(false);
                    currentIterator = currentQueryResponse.Results.ToArray();
                }

                if (currentpos + 1 == currentIterator.Length)
                {
                    if (!currentQueryResponse.HasMore)
                        return false;
                    currentpos = -1;
                    currentQueryResponse = await handler.QueryStateNextAsync(channelId, txId, currentQueryResponse.Id, token).ConfigureAwait(false);
                    currentIterator = currentQueryResponse.Results.ToArray();
                }

                currentpos++;
                return true;
            }

            public T Current
            {
                get
                {
                    if (currentIterator == null)
                        return default(T);
                    return mapper(currentIterator[currentpos]);
                }
            }
        }
    }
}