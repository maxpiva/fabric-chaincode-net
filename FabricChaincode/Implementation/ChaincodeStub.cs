/*
Copyright IBM Corp., DTCC All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Common;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Protos.Peer.ProposalPackage;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class ChaincodeStub : IChaincodeStub
    {
        private static readonly string UNSPECIFIED_KEY = char.ConvertFromUtf32(1).ToString();
        public static readonly string MAX_UNICODE_RUNE = "\udbff\udfff";


        private readonly IReadOnlyList<ByteString> args;
        private readonly Handler handler;
        private readonly ByteString creator;
        private Dictionary<string, ByteString> transientMap;

        public ChaincodeStub(string channelId, string txId, Handler handler, List<ByteString> args, SignedProposal signedProposal)
        {
            ChannelId = channelId;
            TxId = txId;
            this.handler = handler;
            this.args = args.ToList();
            SignedProposal = signedProposal;
            if (SignedProposal == null || SignedProposal.ProposalBytes.IsEmpty)
            {
                creator = null;
                TxTimestamp = null;
                transientMap = new Dictionary<string, ByteString>();
                Binding = null;
            }
            else
            {
                Proposal proposal = Proposal.Parser.ParseFrom(signedProposal.ProposalBytes);
                Header header = Header.Parser.ParseFrom(proposal.Header);
                ChannelHeader channelHeader = ChannelHeader.Parser.ParseFrom(header.ChannelHeader);
                ValidateProposalType(channelHeader);
                SignatureHeader signatureHeader = SignatureHeader.Parser.ParseFrom(header.SignatureHeader);
                ChaincodeProposalPayload chaincodeProposalPayload = ChaincodeProposalPayload.Parser.ParseFrom(proposal.Payload);
                TxTimestamp = channelHeader.Timestamp.ToDateTimeOffset();
                creator = signatureHeader.Creator;
                transientMap = chaincodeProposalPayload.TransientMap.ToDictionary(a => a.Key, a => a.Value);
                Binding = ComputeBinding(channelHeader, signatureHeader);
            }
        }

        public List<byte[]> Args => args.Select(a => a.ToByteArray()).ToList();
        public List<string> StringArgs => args.Select(a => a.ToStringUtf8()).ToList();
        public string Function => args.Count > 0 ? args[0].ToStringUtf8() : null;
        public List<string> Parameters => args.Count > 1 ? args.Skip(1).Select(a => a.ToStringUtf8()).ToList() : new List<string>();
        public SignedProposal SignedProposal { get; }
        public DateTimeOffset? TxTimestamp { get; }

        public Dictionary<string, byte[]> Transient => transientMap.ToDictionary(a => a.Key, a => a.Value.ToByteArray());
        public byte[] Creator => creator.ToByteArray();

        public byte[] Binding { get; }


        public void SetEvent(string name, byte[] payload)
        {
            if (name == null || name.Trim().Length == 0)
                throw new ArgumentException("event name can not be nil string");
            Event = payload != null ? new ChaincodeEvent {EventName = name, Payload = ByteString.CopyFrom(payload)} : new ChaincodeEvent {EventName = name};
        }

        public Response InvokeChaincode(string chaincodeName, List<byte[]> arguments)
        {
            return InvokeChaincode(chaincodeName, arguments, null);
        }

        public Response InvokeChaincodeWithStringArgs(string chaincodeName, List<string> arguments, string channel)
        {
            return InvokeChaincode(chaincodeName, arguments.Select(a => a.ToBytes()).ToList(), channel);
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

        public void PutPrivateData(string collection, string key, string value)
        {
            PutPrivateData(collection,key, value.ToBytes());
        }

        public string GetPrivateDataUTF8(string collection, string key)
        {
            return GetPrivateData(collection, key).ToUTF8String();
        }

        public void PutStringState(string key, string value)
        {
            PutState(key, value.ToBytes());
        }


        public ChaincodeEvent Event { get; private set; }

        public string ChannelId { get; }

        public string TxId { get; }

        public byte[] GetState(string key) => handler.GetState(ChannelId, TxId,"", key).ToByteArray();


        public void PutState(string key, byte[] value)
        {
            ValidateKey(key);
            handler.PutState(ChannelId, TxId,"", key, ByteString.CopyFrom(value));
        }


        public void DelState(string key)
        {
            handler.DeleteState(ChannelId, TxId,"", key);
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

        public IQueryResultsIterator<IKeyValue> GetStateByPartialCompositeKey(string objectType, params string[] attributes)
        {
            return GetStateByPartialCompositeKey(new CompositeKey(objectType, attributes));
        }


        public IQueryResultsIterator<IKeyValue> GetStateByPartialCompositeKey(CompositeKey compositeKey)
        {
            if (compositeKey == null)
                compositeKey = new CompositeKey(UNSPECIFIED_KEY);
            string cKeyAsString = compositeKey.ToString();

            return ExecuteGetStateByRange("", cKeyAsString, cKeyAsString + MAX_UNICODE_RUNE);
        }


        public byte[] GetPrivateData(string collection, string key)
        {
            ValidateCollection(collection);
            return handler.GetState(ChannelId, TxId, collection, key).ToByteArray();
        }


        public void PutPrivateData(string collection, string key, byte[] value)
        {
            ValidateKey(key);
            ValidateCollection(collection);
            handler.PutState(ChannelId, TxId, collection, key, ByteString.CopyFrom(value));
        }


        public void DelPrivateData(string collection, string key)
        {
            ValidateCollection(collection);
            handler.DeleteState(ChannelId, TxId, collection, key);
        }

        public IQueryResultsIterator<IKeyValue> GetPrivateDataByRange(string collection, string startKey, string endKey)
        {
            ValidateCollection(collection);
            if (string.IsNullOrEmpty(startKey))
                startKey = UNSPECIFIED_KEY;
            if (string.IsNullOrEmpty(endKey))
                endKey = UNSPECIFIED_KEY;
            CompositeKey.ValidateSimpleKeys(startKey, endKey);

            return ExecuteGetStateByRange(collection, startKey, endKey);
        }

        public IQueryResultsIterator<IKeyValue> GetPrivateDataByPartialCompositeKey(string collection, string compositeKey)
        {
            if (compositeKey == null)
                compositeKey = "";

            CompositeKey key = compositeKey.StartsWith(CompositeKey.NAMESPACE) ? CompositeKey.ParseCompositeKey(compositeKey) : new CompositeKey(compositeKey);

            return GetPrivateDataByPartialCompositeKey(collection, key);
        }

        public IQueryResultsIterator<IKeyValue> GetPrivateDataByPartialCompositeKey(string collection, CompositeKey compositeKey)
        {
            if (compositeKey == null)
                compositeKey = new CompositeKey(UNSPECIFIED_KEY);

            string cKeyAsString = compositeKey.ToString();

            return ExecuteGetStateByRange(collection, cKeyAsString, cKeyAsString + MAX_UNICODE_RUNE);
        }

        public IQueryResultsIterator<IKeyValue> GetPrivateDataByPartialCompositeKey(string collection, string objectType, params string[] attributes)
        {
            return GetPrivateDataByPartialCompositeKey(collection, new CompositeKey(objectType, attributes));
        }

        public IQueryResultsIterator<IKeyValue> GetPrivateDataQueryResult(string collection, string query)
        {
            ValidateCollection(collection);
            return new QueryResultsIterator<IKeyValue>(handler, ChannelId, TxId, handler.GetQueryResult(ChannelId, TxId, collection, query), (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }

        public IQueryResultsIterator<IKeyModification> GetHistoryForKey(string key)
        {
            return new QueryResultsIterator<IKeyModification>(handler, ChannelId, TxId, handler.GetHistoryForKey(ChannelId, TxId, key), (qv) => new KeyModification(Protos.Ledger.QueryResult.KeyModification.Parser.ParseFrom(qv.ResultBytes)));
        }

        private byte[] ComputeBinding(ChannelHeader channelHeader, SignatureHeader signatureHeader)
        {
            using (SHA256Managed digest = new SHA256Managed())
            {
                digest.Initialize();
                digest.TransformBlock(signatureHeader.Nonce.ToByteArray(), 0, signatureHeader.Nonce.Length, null, 0);
                digest.TransformBlock(creator.ToByteArray(), 0, creator.Length, null, 0);
                //TODO mpiva Might be inverted, check...
                byte[] epoch = BitConverter.GetBytes(channelHeader.Epoch);
                if (BitConverter.IsLittleEndian)
                    epoch = epoch.Reverse().ToArray();
                digest.TransformFinalBlock(epoch, 0, epoch.Length);
                return digest.Hash;
            }
        }

        private void ValidateProposalType(ChannelHeader channelHeader)
        {
            switch ((HeaderType) channelHeader.Type)
            {
                case HeaderType.EndorserTransaction:
                case HeaderType.Config:
                    return;
                default:
                    throw new Exception($"Unexpected transaction type: {((HeaderType) channelHeader.Type).ToString()}");
            }
        }


        public IQueryResultsIterator<IKeyValue> GetStateByRange(string startKey, string endKey)
        {
            if (string.IsNullOrEmpty(startKey))
                startKey = UNSPECIFIED_KEY;
            if (string.IsNullOrEmpty(endKey))
                endKey = UNSPECIFIED_KEY;
            CompositeKey.ValidateSimpleKeys(startKey, endKey);
            return ExecuteGetStateByRange("", startKey, endKey);
        }

        private IQueryResultsIterator<IKeyValue> ExecuteGetStateByRange(string collection, string startKey, string endKey)
        {
            return new QueryResultsIterator<IKeyValue>(handler, ChannelId, TxId, handler.GetStateByRange(ChannelId, TxId, collection, startKey, endKey), (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }


        public IQueryResultsIterator<IKeyValue> GetStateByPartialCompositeKey(string compositeKey)
        {
            CompositeKey key = compositeKey.StartsWith(CompositeKey.NAMESPACE) ? CompositeKey.ParseCompositeKey(compositeKey) : new CompositeKey(compositeKey);
            return GetStateByPartialCompositeKey(key);
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
            return new QueryResultsIterator<IKeyValue>(handler, ChannelId, TxId, handler.GetQueryResult(ChannelId, TxId,"", query), (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }

        private void ValidateKey(string key)
        {
            if (key == null)
                throw new ArgumentException("key cannot be null");
            if (key.Length == 0)
                throw new ArgumentException("key cannot not be an empty string");
        }

        private void ValidateCollection(string collection)
        {
            if (collection == null)
                throw new ArgumentException("collection cannot be null");
            if (collection.Length == 0)
                throw new ArgumentException("collection must not be an empty string");
        }
    }
}