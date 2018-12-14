/*
Copyright IBM Corp., DTCC All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly string UNSPECIFIED_KEY = char.ConvertFromUtf32(1);
        public static readonly string MAX_UNICODE_RUNE = "\udbff\udfff";

        public const string VALIDATION_PARAMETER = "VALIDATION_PARAMETER"; 

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

        public virtual Task<Response> InvokeChaincodeAsync(string chaincodeName, List<byte[]> arguments, CancellationToken token=default(CancellationToken))
        {
            return InvokeChaincodeAsync(chaincodeName, arguments, null, token);
        }



        public Task<Response> InvokeChaincodeWithStringArgsAsync(string chaincodeName, List<string> arguments, string channel, CancellationToken token = default(CancellationToken))
        {
            return InvokeChaincodeAsync(chaincodeName, arguments.Select(a => a.ToBytes()).ToList(), channel, token);
        }

        public Task<Response> InvokeChaincodeWithStringArgsAsync(string chaincodeName, List<string> arguments, CancellationToken token = default(CancellationToken))
        {
            return InvokeChaincodeWithStringArgsAsync(chaincodeName, arguments, null, token);
        }

        public Task<Response> InvokeChaincodeWithStringArgsAsync(string chaincodeName, CancellationToken token = default(CancellationToken), params string[] arguments)
        {
            return InvokeChaincodeWithStringArgsAsync(chaincodeName, arguments.ToList(), null,token);
        }


        public async Task<string> GetStringStateAsync(string key, CancellationToken token) => (await handler.GetStateAsync(ChannelId,TxId,"",key,token).ConfigureAwait(false)).ToStringUtf8();

        public Task PutPrivateDataAsync(string collection, string key, string value, CancellationToken token=default(CancellationToken))
        {
            return PutPrivateDataAsync(collection,key, value.ToBytes(), token);
        }


        public async Task<string> GetPrivateDataUTF8Async(string collection, string key, CancellationToken token = default(CancellationToken))
        {
            return (await GetPrivateDataAsync(collection, key, token).ConfigureAwait(false)).ToUTF8String();
        }
        

        public Task PutStringStateAsync(string key, string value, CancellationToken token = default(CancellationToken))
        {
            return PutStateAsync(key, value.ToBytes(), token);
        }


        public ChaincodeEvent Event { get; private set; }

        public string ChannelId { get; }

        public string TxId { get; }


        public async Task<byte[]> GetStateAsync(string key, CancellationToken token=default(CancellationToken)) => (await handler.GetStateAsync(ChannelId, TxId, "", key,token).ConfigureAwait(false)).ToByteArray();

        public async Task<byte[]> GetStateValidationParameterAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Dictionary<string, ByteString> metadata = await handler.GetStateMetadataAsync(ChannelId, TxId, "", key, token).ConfigureAwait(false);
            //Hardcoded, not sure if ToString Will get it the right 
            if (metadata.ContainsKey(VALIDATION_PARAMETER))
                return metadata[VALIDATION_PARAMETER].ToByteArray();
            return null;
        }
        public Task PutStateAsync(string key, byte[] value, CancellationToken token = default(CancellationToken))
        {
            ValidateKey(key);
            return handler.PutStateAsync(ChannelId, TxId,"", key, ByteString.CopyFrom(value), token);
        }
        public Task SetStateValidationParameterAsync(string key, byte[] value, CancellationToken token = default(CancellationToken))
        {
            ValidateKey(key);
            return handler.PutStateMetadataAsync(ChannelId, TxId, "", key, VALIDATION_PARAMETER, ByteString.CopyFrom(value),token);
        }

        public Task DelStateAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return handler.DeleteStateAsync(ChannelId, TxId,"", key, token);
        }

        public Task<Response> InvokeChaincodeAsync(string chaincodeName, List<byte[]> arguments, string channel, CancellationToken token=default(CancellationToken))
        {
            // internally we handle chaincode name as a composite name
            string compositeName = channel != null && channel.Trim().Length > 0 ? chaincodeName + "/" + channel : chaincodeName;
            return handler.InvokeChaincodeAsync(ChannelId, TxId, compositeName, arguments, token);
        }

        public IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyAsync(string objectType, params string[] attributes)
        {
            return GetStateByPartialCompositeKeyAsync(new CompositeKey(objectType, attributes));
        }


        public IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyAsync(CompositeKey compositeKey)
        {
            if (compositeKey == null)
                compositeKey = new CompositeKey(UNSPECIFIED_KEY);
            string cKeyAsString = compositeKey.ToString();

            return ExecuteGetStateByRange("", cKeyAsString, cKeyAsString + MAX_UNICODE_RUNE);
        }
        public async Task<byte[]> GetPrivateDataValidationParameterAsync(string collection, string key, CancellationToken token = default(CancellationToken))
        {
            ValidateCollection(collection);
            Dictionary<string, ByteString> metadata = await handler.GetStateMetadataAsync(ChannelId, TxId, collection, key, token).ConfigureAwait(false);
            if (metadata.ContainsKey(VALIDATION_PARAMETER))
                return metadata[VALIDATION_PARAMETER].ToByteArray();
            return null;
        }

        public virtual async Task<byte[]> GetPrivateDataAsync(string collection, string key, CancellationToken token=default(CancellationToken))
        {
            ValidateCollection(collection);
            return (await handler.GetStateAsync(ChannelId, TxId, collection, key,token).ConfigureAwait(false)).ToByteArray();
        }


        public Task PutPrivateDataAsync(string collection, string key, byte[] value, CancellationToken token=default(CancellationToken))
        {
            ValidateKey(key);
            ValidateCollection(collection);
            return handler.PutStateAsync(ChannelId, TxId, collection, key, ByteString.CopyFrom(value), token);
        }
        public Task SetPrivateDataValidationParameterAsync(string collection, string key, byte[] value, CancellationToken token = default(CancellationToken))
        {
            ValidateKey(key);
            ValidateCollection(collection);
            return handler.PutStateMetadataAsync(ChannelId,TxId, collection, key, VALIDATION_PARAMETER, ByteString.CopyFrom(value), token);
        }


        public Task DelPrivateDataAsync(string collection, string key, CancellationToken token = default(CancellationToken))
        {
            ValidateCollection(collection);
            return handler.DeleteStateAsync(ChannelId, TxId, collection, key, token);
        }

        public IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByRangeAsync(string collection, string startKey, string endKey)
        {
            ValidateCollection(collection);
            if (string.IsNullOrEmpty(startKey))
                startKey = UNSPECIFIED_KEY;
            if (string.IsNullOrEmpty(endKey))
                endKey = UNSPECIFIED_KEY;
            CompositeKey.ValidateSimpleKeys(startKey, endKey);

            return ExecuteGetStateByRange(collection, startKey, endKey);
        }

        public IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKeyAsync(string collection, string compositeKey)
        {
            if (compositeKey == null)
                compositeKey = "";

            CompositeKey key = compositeKey.StartsWith(CompositeKey.NAMESPACE) ? CompositeKey.ParseCompositeKey(compositeKey) : new CompositeKey(compositeKey);

            return GetPrivateDataByPartialCompositeKeyAsync(collection, key);
        }

        public IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKeyAsync(string collection, CompositeKey compositeKey)
        {
            if (compositeKey == null)
                compositeKey = new CompositeKey(UNSPECIFIED_KEY);

            string cKeyAsString = compositeKey.ToString();

            return ExecuteGetStateByRange(collection, cKeyAsString, cKeyAsString + MAX_UNICODE_RUNE);
        }

        public IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKeyAsync(string collection, string objectType, params string[] attributes)
        {
            return GetPrivateDataByPartialCompositeKeyAsync(collection, new CompositeKey(objectType, attributes));
        }

        public virtual IAsyncQueryResultsEnumerable<IKeyValue> GetPrivateDataQueryResultAsync(string collection, string query)
        {
            ValidateCollection(collection);
            return new AsyncQueryResultsEnumerable<IKeyValue>(handler, ChannelId, TxId, 
                token=>handler.GetQueryResultAsync(ChannelId, TxId, collection, query,null, token),
                (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }

        public IAsyncQueryResultsEnumerable<IKeyModification> GetHistoryForKeyAsync(string key)
        {
            return new AsyncQueryResultsEnumerable<IKeyModification>(handler, ChannelId, TxId, 
                token=>handler.GetHistoryForKeyAsync(ChannelId, TxId, key,token),
                (qv) => new KeyModification(Protos.Ledger.QueryResult.KeyModification.Parser.ParseFrom(qv.ResultBytes)));
        }

        private byte[] ComputeBinding(ChannelHeader channelHeader, SignatureHeader signatureHeader)
        {
            using (SHA256Managed digest = new SHA256Managed())
            {
                digest.Initialize();
                digest.TransformBlock(signatureHeader.Nonce.ToByteArray(), 0, signatureHeader.Nonce.Length, null, 0);
                digest.TransformBlock(creator.ToByteArray(), 0, creator.Length, null, 0);
                byte[] epoch = BitConverter.GetBytes(channelHeader.Epoch);
                if (!BitConverter.IsLittleEndian)
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


        public IAsyncQueryResultsEnumerable<IKeyValue> GetStateByRangeAsync(string startKey, string endKey)
        {
            if (string.IsNullOrEmpty(startKey))
                startKey = UNSPECIFIED_KEY;
            if (string.IsNullOrEmpty(endKey))
                endKey = UNSPECIFIED_KEY;
            CompositeKey.ValidateSimpleKeys(startKey, endKey);
            return ExecuteGetStateByRange("", startKey, endKey);
        }

        private IAsyncQueryResultsEnumerable<IKeyValue> ExecuteGetStateByRange(string collection, string startKey, string endKey)
        {
            return new AsyncQueryResultsEnumerable<IKeyValue>(handler, ChannelId, TxId,
                (token)=>handler.GetStateByRangeAsync(ChannelId, TxId, collection, startKey, endKey,null,token),
                (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }

        public IAsyncQueryResultsEnumerable<IKeyValue> GetStateByRangeWithPaginationAsync(string startKey, string endKey, int pageSize, string bookmark)
        {
            if (string.IsNullOrEmpty(startKey))
                startKey = UNSPECIFIED_KEY;
            if (string.IsNullOrEmpty(endKey))
                endKey = UNSPECIFIED_KEY;

            CompositeKey.ValidateSimpleKeys(startKey, endKey);

            QueryMetadata queryMetadata = new QueryMetadata { Bookmark = bookmark, PageSize = pageSize};
            
            return ExecuteGetStateByRangeWithMetadata("", startKey, endKey, queryMetadata.ToByteString());
        }

        private IAsyncQueryResultsEnumerable<IKeyValue> ExecuteGetStateByRangeWithMetadata(string collection, string startKey, string endKey, ByteString metadata)
        {
            return new AsyncQueryResultsEnumerable<IKeyValue>(handler,ChannelId,TxId,
                (token)=>handler.GetStateByRangeAsync(ChannelId,TxId,collection,startKey,endKey,metadata, token),
                qv=> new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }
        public IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyAsync(string compositeKey)
        {
            CompositeKey key = compositeKey.StartsWith(CompositeKey.NAMESPACE) ? CompositeKey.ParseCompositeKey(compositeKey) : new CompositeKey(compositeKey);
            return GetStateByPartialCompositeKeyAsync(key);
        }
        public IAsyncQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyWithPaginationAsync(CompositeKey compositeKey, int pageSize, string bookmark)
        {
            if (compositeKey == null)
                compositeKey = new CompositeKey(UNSPECIFIED_KEY);

            string cKeyAsString = compositeKey.ToString();

            QueryMetadata queryMetadata = new QueryMetadata { Bookmark = bookmark, PageSize = pageSize };

            return ExecuteGetStateByRangeWithMetadata("", cKeyAsString, cKeyAsString + MAX_UNICODE_RUNE, queryMetadata.ToByteString());
        }

        public CompositeKey CreateCompositeKey(string objectType, params string[] attributes)
        {
            return new CompositeKey(objectType, attributes);
        }


        public CompositeKey SplitCompositeKey(string compositeKey)
        {
            return CompositeKey.ParseCompositeKey(compositeKey);
        }


        public IAsyncQueryResultsEnumerable<IKeyValue> GetQueryResultAsync(string query)
        {
            return new AsyncQueryResultsEnumerable<IKeyValue>(handler, ChannelId, TxId, 
                token=>handler.GetQueryResultAsync(ChannelId, TxId,"", query,null, token),
                (qv) => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }
        public IAsyncQueryResultsEnumerable<IKeyValue> GetQueryResultWithPaginationAsync(string query, int pageSize, string bookmark)
        {
            QueryMetadata queryMetadata = new QueryMetadata { Bookmark = bookmark, PageSize = pageSize };
            return new AsyncQueryResultsEnumerable<IKeyValue>(handler, ChannelId, TxId,
                (token) => handler.GetQueryResultAsync(ChannelId, TxId, "",query, queryMetadata.ToByteString(),token),
                qv => new KeyValue(KV.Parser.ParseFrom(qv.ResultBytes)));
        }


        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ValidateKey(string key)
        {
            if (key == null)
                throw new ArgumentException("key cannot be null");
            if (key.Length == 0)
                throw new ArgumentException("key cannot not be an empty string");
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ValidateCollection(string collection)
        {
            if (collection == null)
                throw new ArgumentException("collection cannot be null");
            if (collection.Length == 0)
                throw new ArgumentException("collection must not be an empty string");
        }
    }
}