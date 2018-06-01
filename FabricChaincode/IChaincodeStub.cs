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
/*
package org.hyperledger.fabric.shim;

import static java.nio.charset.StandardCharsets.UTF_8;
import static java.util.stream.Collectors.toList;

import java.util.Arrays;
import java.util.List;

import org.hyperledger.fabric.protos.peer.ChaincodeEventPackage.ChaincodeEvent;
import org.hyperledger.fabric.shim.Chaincode.Response;
import org.hyperledger.fabric.shim.ledger.CompositeKey;
import org.hyperledger.fabric.shim.ledger.KeyModification;
import org.hyperledger.fabric.shim.ledger.KeyValue;
import org.hyperledger.fabric.shim.ledger.QueryResultsIterator;
*/

using System;
using System.Collections.Generic;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Protos.Peer.ProposalResponsePackage;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim
{
    public interface IChaincodeStub
    {

        /**
         * Returns the arguments corresponding to the call to
         * {@link Chaincode#init(ChaincodeStub)} or
         * {@link Chaincode#invoke(ChaincodeStub)}.
         *
         * @return a list of arguments
         */
        List<byte[]> Args { get; }

        /**
         * Returns the arguments corresponding to the call to
         * {@link Chaincode#init(ChaincodeStub)} or
         * {@link Chaincode#invoke(ChaincodeStub)}.
         *
         * @return a list of arguments cast to UTF-8 strings
         */
        List<string> StringArgs { get; }

        /**
         * A convenience method that returns the first argument of the chaincode
         * invocation for use as a function name.
         *
         * The bytes of the first argument are decoded as a UTF-8 string.
         *
         * @return the function name
         */
        string Function { get; }

        /**
         * A convenience method that returns all except the first argument of the
         * chaincode invocation for use as the parameters to the function returned
         * by #{@link ChaincodeStub#getFunction()}.
         *
         * The bytes of the arguments are decoded as a UTF-8 strings and returned as
         * a list of string parameters..
         *
         * @return a list of parameters
         */
        List<string> Parameters { get; }

        /**
         * Returns the transaction id
         *
         * @return the transaction id
         */
        string TxId { get; }

        /**
         * Returns the channel id
         *
         * @return the channel id
         */
        string ChannelId { get; }

        /**
         * Invoke another chaincode using the same transaction context.
         *
         * @param chaincodeName
         *            Name of chaincode to be invoked.
         * @param args
         *            Arguments to pass on to the called chaincode.
         * @param channel
         *            If not specified, the caller's channel is assumed.
         * @return
         */
        Response InvokeChaincode(string chaincodeName, List<byte[]> args, string channel);

        /**
         * Returns the byte array value specified by the key, from the ledger.
         *
         * @param key
         *            name of the value
         * @return value the value read from the ledger
         */
        byte[] GetState(string key);

        /**
         * Writes the specified value and key into the ledger
         *
         * @param key
         *            name of the value
         * @param value
         *            the value to write to the ledger
         */
        void PutState(string key, byte[] value);

        /**
         * Removes the specified key from the ledger
         *
         * @param key
         *            name of the value to be deleted
         */
        void DelState(string key);

        /**
         * Returns all existing keys, and their values, that are lexicographically
         * between <code>startkey</code> (inclusive) and the <code>endKey</code>
         * (exclusive).
         *
         * @param startKey
         * @param endKey
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IQueryResultsIterator<IKeyValue> GetStateByRange(string startKey, string endKey);

        /**
         * Returns all existing keys, and their values, that are prefixed by the
         * specified partial {@link CompositeKey}.
         *
         * If a full composite key is specified, it will not match itself, resulting
         * in no keys being returned.
         *
         * @param compositeKey
         *            partial composite key
         * @return an {@link Iterable} of {@link KeyValue}
         */
        IQueryResultsIterator<IKeyValue> GetStateByPartialCompositeKey(string compositeKey);

        /**
         * Given a set of attributes, this method combines these attributes to
         * return a composite key.
         *
         * @param objectType
         * @param attributes
         * @return a composite key
         * @throws CompositeKeyFormatException
         *             if any parameter contains either a U+000000 or U+10FFFF code
         *             point.
         */
        CompositeKey CreateCompositeKey(string objectType, params string[] attributes);

        /**
         * Parses a composite key from a string.
         *
         * @param compositeKey
         *            a composite key string
         * @return a composite key
         */
        CompositeKey SplitCompositeKey(string compositeKey);

        /**
         * Perform a rich query against the state database.
         *
         * @param query
         *            query string in a syntax supported by the underlying state
         *            database
         * @return
         * @throws UnsupportedOperationException
         *             if the underlying state database does not support rich
         *             queries.
         */
        IQueryResultsIterator<IKeyValue> GetQueryResult(string query);

        /**
         * Returns the history of the specified key's values across time.
         *
         * @param key
         * @return an {@link Iterable} of {@link KeyModification}
         */
        IQueryResultsIterator<IKeyModification> GetHistoryForKey(string key);

        /**
         * Defines the CHAINCODE type event that will be posted to interested
         * clients when the chaincode's result is committed to the ledger.
         *
         * @param name
         *            Name of event. Cannot be null or empty string.
         * @param payload
         *            Optional event payload.
         */
        void SetEvent(string name, byte[] payload);

        /**
         * Invoke another chaincode using the same transaction context.
         *
         * @param chaincodeName
         *            Name of chaincode to be invoked.
         * @param args
         *            Arguments to pass on to the called chaincode.
         * @return
         */
        Response InvokeChaincode(string chaincodeName, List<byte[]> args);
//        {
//		return invokeChaincode(chaincodeName, args, null);
//	}

        /**
         * Invoke another chaincode using the same transaction context.
         *
         * This is a convenience version of
         * {@link #invokeChaincode(String, List, String)}. The string args will be
         * encoded into as UTF-8 bytes.
         *
         * @param chaincodeName
         *            Name of chaincode to be invoked.
         * @param args
         *            Arguments to pass on to the called chaincode.
         * @param channel
         *            If not specified, the caller's channel is assumed.
         * @return
         */
        Response InvokeChaincodeWithStringArgs(string chaincodeName, List<string> args, string channel);
        //{
        //	return invokeChaincode(chaincodeName, args.stream().map(x -> x.getBytes(UTF_8)).collect(toList()), channel);
        //}

        /**
         * Invoke another chaincode using the same transaction context.
         *
         * This is a convenience version of {@link #invokeChaincode(String, List)}.
         * The string args will be encoded into as UTF-8 bytes.
         *
         *
         * @param chaincodeName
         *            Name of chaincode to be invoked.
         * @param args
         *            Arguments to pass on to the called chaincode.
         * @return
         */
        Response InvokeChaincodeWithStringArgs(string chaincodeName, List<string> args);
        //{
        //return invokeChaincodeWithStringArgs(chaincodeName, args, null);
        //}

        /**
         * Invoke another chaincode using the same transaction context.
         *
         * This is a convenience version of {@link #invokeChaincode(String, List)}.
         * The string args will be encoded into as UTF-8 bytes.
         *
         *
         * @param chaincodeName
         *            Name of chaincode to be invoked.
         * @param args
         *            Arguments to pass on to the called chaincode.
         * @return
         */
        Response InvokeChaincodeWithStringArgs(string chaincodeName, params string[] args);
        //{
//		return invokeChaincodeWithStringArgs(chaincodeName, Arrays.asList(args), null);
//	}

        /**
         * Returns the byte array value specified by the key and decoded as a UTF-8
         * encoded string, from the ledger.
         *
         * @param key
         *            name of the value
         * @return value the value read from the ledger
         */
        string GetStringState(string key);
        //{
        //return new String(getState(key), UTF_8);
        //}

        /**
         * Writes the specified value and key into the ledger
         *
         * @param key
         *            name of the value
         * @param value
         *            the value to write to the ledger
         */
        void PutStringState(String key, String value);
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

    }
}
