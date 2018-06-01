/*
Copyright DTCC, IBM 2016, 2017 All Rights Reserved.

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
using System.Linq;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class ChaincodeStub : IChaincodeStub
    {
        private readonly IReadOnlyList<ByteString> args;

        private readonly Handler handler;

        public ChaincodeStub(string channelId, string txId, Handler handler, List<ByteString> args)
        {
            ChannelId = channelId;
            TxId = txId;
            this.handler = handler;
            this.args = args.ToList();
        }

        public List<byte[]> Args => args.Select(a => a.ToByteArray()).ToList();
        public List<string> StringArgs => args.Select(a => a.ToStringUtf8()).ToList();
        public string Function => args.Count > 0 ? args[0].ToStringUtf8() : null;
        public List<string> Parameters => args.Count > 1 ? args.Skip(1).Select(a => a.ToStringUtf8()).ToList() : new List<string>();



        public void SetEvent(string name, byte[] payload)
        {
            if (name == null || name.Trim().Length == 0)
                throw new ArgumentException("Event name cannot be null or empty string.");
            Event = payload != null ? new ChaincodeEvent {EventName = name, Payload = ByteString.CopyFrom(payload)} : new ChaincodeEvent {EventName = name};
        }

        public Response InvokeChaincode(string chaincodeName, List<byte[]> arguments)
        {
            return InvokeChaincode(chaincodeName, arguments, null);
        }

        public Response InvokeChaincodeWithStringArgs(string chaincodeName, List<string> arguments, string channel)
        {
            return InvokeChaincode(chaincodeName, arguments.Select(a=>a.ToBytes()).ToList(), channel);
        }

        public Response InvokeChaincodeWithStringArgs(string chaincodeName, List<string> arguments)
        {
            return InvokeChaincodeWithStringArgs(chaincodeName, arguments, null);
        }

        public Response InvokeChaincodeWithStringArgs(string chaincodeName, params string[] arguments)
        {
            return InvokeChaincodeWithStringArgs(chaincodeName, arguments.ToList(), null);
        }

        public string GetStringState(string key)
        {
            return GetState(key).ToUTF8String();
        }

        public void PutStringState(string key, string value)
        {
            PutState(key,value.ToBytes());
        }


        public ChaincodeEvent Event { get; private set; }

        public string ChannelId { get; }

        public string TxId { get; }

        public byte[] GetState(string key) => handler.GetState(ChannelId, TxId, key).ToByteArray();


        public void PutState(string key, byte[] value)
        {
            handler.PutState(ChannelId, TxId, key, ByteString.CopyFrom(value));
        }


        public void DelState(string key)
        {
            handler.DeleteState(ChannelId, TxId, key);
        }

        IQueryResultsIterator<IKeyValue> IChaincodeStub.GetStateByRange(string startKey, string endKey)
        {
            return GetStateByRange(startKey, endKey);
        }

        IQueryResultsIterator<IKeyValue> IChaincodeStub.GetStateByPartialCompositeKey(string compositeKey)
        {
            return GetStateByPartialCompositeKey(compositeKey);
        }

        CompositeKey IChaincodeStub.CreateCompositeKey(string objectType, params string[] attributes)
        {
            return CreateCompositeKey(objectType, attributes);
        }

        CompositeKey IChaincodeStub.SplitCompositeKey(string compositeKey)
        {
            return SplitCompositeKey(compositeKey);
        }

        IQueryResultsIterator<IKeyValue> IChaincodeStub.GetQueryResult(string query)
        {
            return GetQueryResult(query);
        }

        public Response InvokeChaincode(string chaincodeName, List<byte[]> arguments, string channel)
        {
            // internally we handle chaincode name as a composite name
            string compositeName;
            if (channel != null && channel.Trim().Length > 0)
            {
                compositeName = chaincodeName + "/" + channel;
            }
            else
            {
                compositeName = chaincodeName;
            }

            return handler.InvokeChaincode(ChannelId, TxId, compositeName, arguments);
        }


        public IQueryResultsIterator<IKeyValue> GetStateByRange(string startKey, string endKey)
        {
            return new QueryResultsIterator<IKeyValue>(handler, ChannelId, TxId, handler.GetStateByRange(ChannelId, TxId, startKey, endKey), (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }


        public IQueryResultsIterator<IKeyValue> GetStateByPartialCompositeKey(string compositeKey)
        {
            return GetStateByRange(compositeKey, compositeKey + "\udbff\udfff");
        }


        public CompositeKey CreateCompositeKey(string objectType, params string[] attributes)
        {
            return new CompositeKey(objectType, attributes);
        }


        public CompositeKey SplitCompositeKey(string compositeKey)
        {
            return CompositeKey.ParseCompositeKey(compositeKey);
        }


        public IQueryResultsIterator<IKeyValue> GetQueryResult(string query)
        {
            return new QueryResultsIterator<IKeyValue>(handler, ChannelId, TxId, handler.GetQueryResult(ChannelId, TxId, query), (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }

        public IQueryResultsIterator<IKeyModification> GetHistoryForKey(string key)
        {
            return new QueryResultsIterator<IKeyModification>(handler, ChannelId, TxId, handler.GetHistoryForKey(ChannelId, TxId, key), (qv) => new KeyModification(Protos.Ledger.QueryResult.KeyModification.Parser.ParseFrom(qv.ResultBytes)));
        }
    }
}