/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Protos.Peer.ProposalPackage;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim
{
    public interface IChaincodeStub
    {
        /**
         * Returns the arguments corresponding to the call to
         * {@link Chaincode#init(ChaincodeStub)} or
         * {@link Chaincode#invoke(ChaincodeStub)}, each argument represented as byte array.
         *
         * @return a list of arguments (bytes arrays)
         */
        List<byte[]> Args { get; }

        /**
         * Returns the arguments corresponding to the call to
         * {@link Chaincode#init(ChaincodeStub)} or
         * {@link Chaincode#invoke(ChaincodeStub)}, cast to UTF-8 string.
         *
         * @return a list of arguments cast to UTF-8 strings
         */
        List<string> StringArgs { get; }

        /**
         * A convenience method that returns the first argument of the chaincode
         * invocation for use as a function name.
         * <p>
         * The bytes of the first argument are decoded as a UTF-8 string.
         *
         * @return the function name
         */
        string Function { get; }
        /**
         * A convenience method that returns all except the first argument of the
         * chaincode invocation for use as the parameters to the function returned
         * by #{@link ChaincodeStub#getFunction()}.
         * <p>
         * The bytes of the arguments are decoded as a UTF-8 strings and returned as
         * a list of string parameters.
         *
         * @return a list of parameters
         */
        List<string> Parameters { get; }

        /**
         * Returns the transaction id for the current chaincode invocation request.
         * <p>
         * The transaction id uniquely identifies the transaction within the scope of the channel.
         *
         * @return the transaction id
         */
        string TxId { get; }

        /**
         * Returns the channel id for the current proposal.
         * <p>
         * This would be the 'channel_id' of the transaction proposal
         * except where the chaincode is calling another on a different channel.
         *
         * @return the channel id
         */
        string ChannelId { get; }
        //{
        //putState(key, value.getBytes(UTF_8));
        //}

        /**
         * Returns the CHAINCODE type event that will be posted to interested
         * clients when the chaincode's result is committed to the ledger.
         *
         * @return the chaincode event or null
         */
        ChaincodeEvent Event { get; }

        /**
         * Locally calls the specified chaincode <code>invoke()</code> using the
         * same transaction context.
         * <p>
         * chaincode calling chaincode doesn't create a new transaction message.
         * <p>
         * If the called chaincode is on the same channel, it simply adds the called
         * chaincode read set and write set to the calling transaction.
         * <p>
         * If the called chaincode is on a different channel,
         * only the Response is returned to the calling chaincode; any <code>putState</code> calls
         * from the called chaincode will not have any effect on the ledger; that is,
         * the called chaincode on a different channel will not have its read set
         * and write set applied to the transaction. Only the calling chaincode's
         * read set and write set will be applied to the transaction. Effectively
         * the called chaincode on a different channel is a `Query`, which does not
         * participate in state validation checks in subsequent commit phase.
         * <p>
         * If `channel` is empty, the caller's channel is assumed.
         * <p>
         * Invoke another chaincode using the same transaction context.
         *
         * @param chaincodeName Name of chaincode to be invoked.
         * @param args          Arguments to pass on to the called chaincode.
         * @param channel       If not specified, the caller's channel is assumed.
         * @return {@link Response} object returned by called chaincode
         */
        
        Task<Response> InvokeChaincodeAsync(string chaincodeName, List<byte[]> arguments, string channel, CancellationToken token = default(CancellationToken));
        /**
         * Returns the value of the specified <code>key</code> from the ledger.
         * <p>
         * Note that getState doesn't read data from the writeset, which has not been committed to the ledger.
         * In other words, GetState doesn't consider data modified by PutState that has not been committed.
         *
         * @param key name of the value
         * @return value the value read from the ledger
         */
        Task<byte[]> GetStateAsync(string key, CancellationToken token=default(CancellationToken));
        /**
         * retrieves the key-level endorsement policy for <code>key</code>.
         * Note that this will introduce a read dependency on <code>key</code> in the transaction's readset.
         * @param key key to get key level endorsement
         * @return endorsement policy
         */
        Task<byte[]> GetStateValidationParameterAsync(string key, CancellationToken token = default(CancellationToken));

        /**
         * Puts the specified <code>key</code> and <code>value</code> into the transaction's
         * writeset as a data-write proposal.
         * <p>
         * putState doesn't effect the ledger
         * until the transaction is validated and successfully committed.
         * Simple keys must not be an empty string and must not start with 0x00
         * character, in order to avoid range query collisions with
         * composite keys
         *
         * @param key   name of the value
         * @param value the value to write to the ledger
         */
        Task PutStateAsync(string key, byte[] value, CancellationToken token = default(CancellationToken));
        /**
         * Sets the key-level endorsement policy for <code>key</code>.
         *
         * @param key key to set key level endorsement
         * @param value endorsement policy
         */
        Task SetStateValidationParameterAsync(string key, byte[] value, CancellationToken token = default(CancellationToken));

        /**
         * Records the specified <code>key</code> to be deleted in the writeset of
         * the transaction proposal.
         * <p>
         * The <code>key</code> and its value will be deleted from
         * the ledger when the transaction is validated and successfully committed.
         *
         * @param key name of the value to be deleted
         */
        Task DelStateAsync(string key, CancellationToken token = default(CancellationToken));

        /**
         * Returns all existing keys, and their values, that are lexicographically
         * between <code>startkey</code> (inclusive) and the <code>endKey</code>
         * (exclusive).
         * <p>
         * The keys are returned by the iterator in lexical order. Note
         * that startKey and endKey can be empty string, which implies unbounded range
         * query on start or end.
         * <p>
         * Call close() on the returned {@link QueryResultsIterator#close()} object when done.
         *
         * @param startKey key as the start of the key range (inclusive)
         * @param endKey   key as the end of the key range (exclusive)
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetStateByRangeAsync(string startKey, string endKey);

        /**
         * Returns a range iterator over a set of keys in the ledger. The iterator can be used to fetch keys between the
         * <code>startKey</code> (inclusive) and <code>endKey</code> (exclusive).
         * When an empty string is passed as a value to the <code>bookmark</code>  argument, the returned iterator can be used to fetch
         * the first <code>pageSize</code> keys between the  <code>startKey</code> and <code>endKey</code>.
         * When the <code>bookmark</code> is a non-empty string, the iterator can be used to fetch first <code>pageSize</code> keys between the
         * <code>bookmark</code> and <code>endKey</code>.
         * Note that only the bookmark present in a prior page of query results ({@link org.hyperledger.fabric.protos.peer.ChaincodeShim.QueryResponseMetadata})
         * can be used as a value to the bookmark argument. Otherwise, an empty string must be passed as bookmark.
         * The keys are returned by the iterator in lexical order. Note  that <code>startKey</code> and <code>endKey</code> can be empty string, which implies
         * unbounded range query on start or end.
         * This call is only supported in a read only transaction.
         *
         * @param startKey
         * @param endKey
         * @param pageSize
         * @param bookmark
         * @return
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetStateByRangeWithPaginationAsync(string startKey, string endKey, int pageSize, string bookmark);


        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey}.
         * <p>
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         * <p>
         *
         * <p>
         * This method takes responsibility to correctly parse the {@link CompositeKey} from a String
         * and behaves exactly as {@link ChaincodeStub#getStateByPartialCompositeKey(CompositeKey)}.
         * </p>
         * <p>
         * Call close() on the returned {@link QueryResultsIterator#close()} object when done.
         *
         * @param compositeKey partial composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyAsync(string compositeKey);

        /**
         * Queries the state in the ledger based on a given partial composite key. This function returns an iterator
         * which can be used to iterate over the composite keys whose prefix matches the given partial composite key. <p>
         * When an empty string is passed as a value to the <code>bookmark</code>  argument, the returned iterator can be used to fetch
         * the first <code>pageSize</code> composite keys whose prefix matches the given partial composite key. <p>
         * When the <code>bookmark</code> is a non-empty string, the iterator can be used to fetch first <code>pageSize</code> keys between the
         * <code>bookmark</code> (inclusive) and and the last matching composite key.<p>
         * Note that only the bookmark present in a prior page of query results ({@link org.hyperledger.fabric.protos.peer.ChaincodeShim.QueryResponseMetadata})
         * can be used as a value to the bookmark argument. Otherwise, an empty string must be passed as bookmark. <p>
         * This call is only supported in a read only transaction.
         *
         * @param compositeKey
         * @param pageSize
         * @param bookmark
         * @return
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyWithPaginationAsync(CompositeKey compositeKey, int pageSize, string bookmark);


        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey}.
         * <p>
         * It combines the attributes and the objectType to form a partial composite key.
         * <p>
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         * <p>
         * This method takes responsibility to correctly combine Object type and attributes
         * creating a {@link CompositeKey} and behaves exactly
         * as {@link ChaincodeStub#getStateByPartialCompositeKey(CompositeKey)}.
         * </p>
         * Call close() on the returned {@link QueryResultsIterator#close()} object when done.
         *
         * @param objectType: ObjectType of the compositeKey
         * @param attributes: Attributes of the composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyAsync(string objectType, params string[] attributes);

        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey}.
         * <p>
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         *
         * @param compositeKey partial composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyAsync(CompositeKey compositeKey);

        /**
         * Given a set of attributes, this method combines these attributes to
         * return a composite key.
         *
         * @param objectType A string used as the prefix of the resulting key
         * @param attributes List of attribute values to concatenate into the key
         * @return a composite key
         */
        CompositeKey CreateCompositeKey(string objectType, params string[] attributes);


        /**
         * Parses a composite key {@link CompositeKey} from a string.
         *
         * @param compositeKey a composite key string
         * @return a composite key
         */
        CompositeKey SplitCompositeKey(string compositeKey);

        /**
         * Performs a "rich" query against a state database.
         * <p>
         * It is only supported for state databases that support rich query,
         * e.g. CouchDB. The query string is in the native syntax
         * of the underlying state database. An {@link QueryResultsIterator} is returned
         * which can be used to iterate (next) over the query result set.
         *
         * @param query query string in a syntax supported by the underlying state
         *              database
         * @return {@link QueryResultsIterator} object contains query results
         * @throws UnsupportedOperationException if the underlying state database does not support rich
         *                                       queries.
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetQueryResultAsync(string query);
        /**
         * Performs a "rich" query against a state database.
         * It is only supported for state databases that support rich query, e.g., CouchDB. The query string is in the native syntax
         * of the underlying state database. An iterator is returned which can be used to iterate over keys in the query result set.
         * When an empty string is passed as a value to the <code>bookmark</code>  argument, the returned iterator can be used to fetch
         * the first <code>pageSize</code> of query results.. <p>
         * When the <code>bookmark</code> is a non-empty string, the iterator can be used to fetch first <code>pageSize</code> keys between the
         * <code>bookmark</code> (inclusive) and the last key in the query result.<p>
         * Note that only the bookmark present in a prior page of query results ({@link org.hyperledger.fabric.protos.peer.ChaincodeShim.QueryResponseMetadata})
         * can be used as a value to the bookmark argument. Otherwise, an empty string must be passed as bookmark. <p>
         * This call is only supported in a read only transaction.
         *
         * @param query
         * @param pageSize
         * @param bookmark
         * @return
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetQueryResultWithPaginationAsync(string query, int pageSize, string bookmark);


        /**
            /**
             * Returns a history of key values across time.
             * <p>
             * For each historic key update, the historic value and associated
             * transaction id and timestamp are returned. The timestamp is the
             * timestamp provided by the client in the proposal header.
             * This method requires peer configuration
             * <code>core.ledger.history.enableHistoryDatabase</code> to be true.
             *
             * @param key The state variable key
             * @return an {@link Iterable} of {@link KeyModification}
             */
        IAsyncQueryResultsEnumerable<IKeyModification> GetHistoryForKeyAsync(string key);

        /**
         * Returns the value of the specified <code>key</code> from the specified
         * <code>collection</code>.
         * <p>
         * Note that {@link #getPrivateData(String, String)} doesn't read data from the
         * private writeset, which has not been committed to the <code>collection</code>. In
         * other words, {@link #getPrivateData(String, String)} doesn't consider data modified by {@link #putPrivateData(String, String, byte[])}    * that has not been committed.
         *
         * @param collection name of the collection
         * @param key        name of the value
         * @return value the value read from the collection
         */
        Task<byte[]> GetPrivateDataAsync(string collection, string key, CancellationToken token = default(CancellationToken));
        /**
         * Retrieves the key-level endorsement
         * policy for the private data specified by <code>key</code>. Note that this introduces
         * a read dependency on <code>key</code> in the transaction's readset.
         *
         * @param collection name of the collection
         * @param key key to get endorsement policy
         * @return
         */
        Task<byte[]> GetPrivateDataValidationParameterAsync(string collection, string key, CancellationToken token = default(CancellationToken));
        /**
         * Puts the specified <code>key</code> and <code>value</code> into the transaction's
         * private writeset.
         * <p>
         * Note that only hash of the private writeset goes into the
         * transaction proposal response (which is sent to the client who issued the
         * transaction) and the actual private writeset gets temporarily stored in a
         * transient store. putPrivateData doesn't effect the <code>collection</code> until the
         * transaction is validated and successfully committed. Simple keys must not be
         * an empty string and must not start with null character (0x00), in order to
         * avoid range query collisions with composite keys, which internally get
         * prefixed with 0x00 as composite key namespace.
         *
         * @param collection name of the collection
         * @param key        name of the value
         * @param value      the value to write to the ledger
         */
        Task PutPrivateDataAsync(string collection, string key, byte[] value, CancellationToken token = default(CancellationToken));
        /**
 * Sets the key-level endorsement policy for the private data specified by <code>key</code>.
         *
         * @param collection name of the collection
         * @param key   key to set endorsement policy
         * @param value endorsement policy
         */
        Task SetPrivateDataValidationParameterAsync(string collection, string key, byte[] value, CancellationToken token = default(CancellationToken));
        /**
         * Records the specified <code>key</code> to be deleted in the private writeset of
         * the transaction.
         * <p>
         * Note that only hash of the private writeset goes into the
         * transaction proposal response (which is sent to the client who issued the
         * transaction) and the actual private writeset gets temporarily stored in a
         * transient store. The <code>key</code> and its value will be deleted from the collection
         * when the transaction is validated and successfully committed.
         *
         * @param collection name of the collection
         * @param key        name of the value to be deleted
         */
        Task DelPrivateDataAsync(string collection, string key, CancellationToken token = default(CancellationToken));

        /**
         * Returns all existing keys, and their values, that are lexicographically
         * between <code>startkey</code> (inclusive) and the <code>endKey</code>
         * (exclusive) in a given private collection.
         * <p>
         * Note that startKey and endKey can be empty string, which implies unbounded range
         * query on start or end.
         * The query is re-executed during validation phase to ensure result set
         * has not changed since transaction endorsement (phantom reads detected).
         *
         * @param collection name of the collection
         * @param startKey   private data variable key as the start of the key range (inclusive)
         * @param endKey     private data variable key as the end of the key range (exclusive)
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByRangeAsync(string collection, string startKey, string endKey);

        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey} in a given private collection.
         * <p>
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         * <p>
         * The query is re-executed during validation phase to ensure result set
         * has not changed since transaction endorsement (phantom reads detected).
         * <p>
         * This method takes responsibility to correctly parse the {@link CompositeKey} from a String
         * and behaves exactly as {@link ChaincodeStub#getPrivateDataByPartialCompositeKey(String, CompositeKey)}.
         * </p>
         *
         * @param collection   name of the collection
         * @param compositeKey partial composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKeyAsync(string collection, string compositeKey);


        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey} in a given private collection.
         * <p>
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         * <p>
         * The query is re-executed during validation phase to ensure result set
         * has not changed since transaction endorsement (phantom reads detected).
         *
         * @param collection   name of the collection
         * @param compositeKey partial composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKeyAsync(string collection, CompositeKey compositeKey);


        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey} in a given private collection.
         * <p>
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         * <p>
         * The query is re-executed during validation phase to ensure result set
         * has not changed since transaction endorsement (phantom reads detected).
         * <p>
         * This method takes responsibility to correctly combine Object type and attributes
         * creating a {@link CompositeKey} and behaves exactly
         * as {@link ChaincodeStub#getPrivateDataByPartialCompositeKey(String, CompositeKey)}.
         * </p>
         *
         * @param collection  name of the collection
         * @param objectType: ObjectType of the compositeKey
         * @param attributes: Attributes of the composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKeyAsync(string collection, string objectType, params string[] attributes);

        /**
         * Perform a rich query against a given private collection.
         * <p>
         * It is only supported for state databases that support rich query, e.g.CouchDB.
         * The query string is in the native syntax of the underlying state database.
         * An iterator is returned which can be used to iterate (next) over the query result set.
         * The query is NOT re-executed during validation phase, phantom reads are not detected.
         * That is, other committed transactions may have added, updated, or removed keys that
         * impact the result set, and this would not be detected at validation/commit time.
         * Applications susceptible to this should therefore not use GetQueryResult as part of
         * transactions that update ledger, and should limit use to read-only chaincode operations.
         *
         * @param collection name of the collection
         * @param query      query string in a syntax supported by the underlying state
         *                   database
         * @return {@link QueryResultsIterator} object contains query results
         * @throws UnsupportedOperationException if the underlying state database does not support rich
         *                                       queries.
         */
        IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataQueryResultAsync(string collection, string query);



        /**
         * Defines the CHAINCODE type event that will be posted to interested
         * clients when the chaincode's result is committed to the ledger.
         *
         * @param name    Name of event. Cannot be null or empty string.
         * @param payload Optional event payload.
         */
        void SetEvent(string name, byte[] payload);

        /**
         * Invoke another chaincode using the same transaction context.
         * <p>
         * Same as {@link #invokeChaincode(String, List, String)}
         * using channelId to <code>null</code>
         *
         * @param chaincodeName Name of chaincode to be invoked.
         * @param args          Arguments to pass on to the called chaincode.
         * @return {@link Response} object returned by called chaincode
         */
        
        Task<Response> InvokeChaincodeAsync(string chaincodeName, List<byte[]> arguments, CancellationToken token = default(CancellationToken));

        /**
         * Invoke another chaincode using the same transaction context.
         * <p>
         * This is a convenience version of
         * {@link #invokeChaincode(String, List, String)}. The string args will be
         * encoded into as UTF-8 bytes.
         *
         * @param chaincodeName Name of chaincode to be invoked.
         * @param args          Arguments to pass on to the called chaincode.
         * @param channel       If not specified, the caller's channel is assumed.
         * @return {@link Response} object returned by called chaincode
         */
        Task<Response> InvokeChaincodeWithStringArgsAsync(string chaincodeName, List<string> arguments, string channel, CancellationToken token = default(CancellationToken));

        /**
         * Invoke another chaincode using the same transaction context.
         * <p>
         * This is a convenience version of {@link #invokeChaincode(String, List)}.
         * The string args will be encoded into as UTF-8 bytes.
         *
         * @param chaincodeName Name of chaincode to be invoked.
         * @param args          Arguments to pass on to the called chaincode.
         * @return {@link Response} object returned by called chaincode
         */
        Task<Response> InvokeChaincodeWithStringArgsAsync(string chaincodeName, List<string> arguments, CancellationToken token = default(CancellationToken));

        /**
         * Invoke another chaincode using the same transaction context.
         * <p>
         * This is a convenience version of {@link #invokeChaincode(String, List)}.
         * The string args will be encoded into as UTF-8 bytes.
         *
         * @param chaincodeName Name of chaincode to be invoked.
         * @param args          Arguments to pass on to the called chaincode.
         * @return {@link Response} object returned by called chaincode
         */
        Task<Response> InvokeChaincodeWithStringArgsAsync(string chaincodeName, CancellationToken token = default(CancellationToken), params string[] arguments);


        /**
         * Returns the byte array value specified by the key and decoded as a UTF-8
         * encoded string, from the ledger.
         * <p>
         * This is a convenience version of {@link #getState(String)}
         *
         * @param key name of the value
         * @return value the value read from the ledger
         */
        Task<string> GetStringStateAsync(string key, CancellationToken token=default(CancellationToken));
            /**
             * Writes the specified value and key into the sidedb collection
             * value converted to byte array.
             *
             * @param collection collection name
             * @param key        name of the value
             * @param value      the value to write to the ledger
             */

         Task PutPrivateDataAsync(string collection, string key, string value, CancellationToken token = default(CancellationToken));


        /**
         * Returns the byte array value specified by the key and decoded as a UTF-8
         * encoded string, from the sidedb collection.
         *
         * @param collection collection name
         * @param key        name of the value
         * @return value the value read from the ledger
         */

        Task<string> GetPrivateDataUTF8Async(string collection, string key, CancellationToken token = default(CancellationToken));

        /**
         * Writes the specified value and key into the ledger
         *
         * @param key   name of the value
         * @param value the value to write to the ledger
         */
        Task PutStringStateAsync(string key, string value, CancellationToken token = default(CancellationToken));

        /**
        * Returns the signed transaction proposal currently being executed.
        *
        * @return null if the current transaction is an internal call to a system
        * chaincode.
        */
        SignedProposal SignedProposal { get; }

        /**
         * Returns the timestamp when the transaction was created.
         *
         * @return timestamp as specified in the transaction's channel header.
         */
        DateTimeOffset? TxTimestamp { get; }

        /**
         * Returns the identity of the agent (or user) submitting the transaction.
         *
         * @return the bytes of the creator field of the proposal's signature
         * header.
         */
        byte[] Creator { get; }

        /**
         * Returns the transient map associated with the current transaction.
         *
         * @return map of transient field
         */
        Dictionary<string, byte[]> Transient { get; }

        /**
         * Returns the transaction binding.
         *
         * @return binding between application data and proposal
         */
        byte[] Binding { get; }
    }
}