/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Logging;
using Nito.AsyncEx;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class Handler
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(Handler));


        private readonly Dictionary<string, bool> isTransaction = new Dictionary<string, bool>();
        private readonly AsyncProducerConsumerQueue<ChaincodeMessage> outboundChaincodeMessages = new AsyncProducerConsumerQueue<ChaincodeMessage>();
        private readonly Dictionary<string, AsyncProducerConsumerQueue<ChaincodeMessage>> responseChannel = new Dictionary<string, AsyncProducerConsumerQueue<ChaincodeMessage>>();
        private readonly AsyncLock _channelLock = new AsyncLock();

        private IChaincodeAsync chaincode;

        internal Handler()
        {
            //Parameterless constructor for Mocking
        }

        public CCState State { get; private set; }

        public static async Task<Handler> CreateAsync(ChaincodeID chaincodeId, IChaincodeAsync chaincode, CancellationToken token = default(CancellationToken))
        {
            Handler h = new Handler();
            h.chaincode = chaincode;
            h.State = CCState.CREATED;
            await h.QueueOutboundChaincodeMessageAsync(NewRegisterChaincodeMessage(chaincodeId), token).ConfigureAwait(false);
            return h;
        }

     
        public Task<ChaincodeMessage> NextOutboundChaincodeMessageAsync(CancellationToken token)
        {
            return outboundChaincodeMessages.DequeueAsync(token);
        }

        public Task OnChaincodeMessageAsync(ChaincodeMessage chaincodeMessage, CancellationToken token)
        {
            logger.Trace($"[{chaincodeMessage.Txid,-8}s] {chaincodeMessage.ToJsonString()}");
            return HandleChaincodeMessageAsync(chaincodeMessage, token);
        }

        private async Task HandleChaincodeMessageAsync(ChaincodeMessage message, CancellationToken token)
        {
            logger.Debug($"[{message.Txid,-8}s] Handling ChaincodeMessage of type: {message.Type}, handler state {State}");
            if (message.Type == ChaincodeMessage.Types.Type.Keepalive)
            {
                logger.Trace($"[{message.Txid,-8}s] Received KEEPALIVE: nothing to do");
                return;
            }

            switch (State)
            {
                case CCState.CREATED:
                    HandleCreated(message);
                    break;
                case CCState.ESTABLISHED:
                    HandleEstablished(message);
                    break;
                case CCState.READY:
                    await HandleReadyAsync(message, token).ConfigureAwait(false);
                    break;
                default:
                    logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
                    break;
            }
        }

        private void HandleCreated(ChaincodeMessage message)
        {
            if (message.Type == ChaincodeMessage.Types.Type.Registered)
            {
                State = CCState.ESTABLISHED;
                logger.Trace($"[{message.Txid,-8}s] Received REGISTERED: moving to established state");
            }
            else
                logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
        }

        private void HandleEstablished(ChaincodeMessage message)
        {
            if (message.Type == ChaincodeMessage.Types.Type.Ready)
            {
                State = CCState.READY;
                logger.Trace($"[{message.Txid,-8}s] Received READY: ready for invocations");
            }
            else
                logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
        }

        private Task HandleReadyAsync(ChaincodeMessage message, CancellationToken token)
        {
            switch (message.Type)
            {
                case ChaincodeMessage.Types.Type.Response:
                    logger.Trace($"[{message.Txid,-8}s] Received RESPONSE: publishing to channel");
                    return SendChannelAsync(message, token);
                case ChaincodeMessage.Types.Type.Error:
                    logger.Trace($"[{message.Txid,-8}s] Received ERROR: publishing to channel");
                    return SendChannelAsync(message, token);
                case ChaincodeMessage.Types.Type.Init:
                    logger.Trace($"[{message.Txid,-8}s] Received INIT: invoking chaincode init");
                    return HandleInitAsync(message, token);
                case ChaincodeMessage.Types.Type.Transaction:
                    logger.Trace($"[{message.Txid,-8}s] Received TRANSACTION: invoking chaincode");
                    return HandleTransactionAsync(message, token);
                default:
                    logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
                    return Task.FromResult(0);
            }
        }

        private string GetTxKey(string channelId, string txid) => channelId + txid;

        private Task QueueOutboundChaincodeMessageAsync(ChaincodeMessage chaincodeMessage, CancellationToken token)
        {
            return outboundChaincodeMessages.EnqueueAsync(chaincodeMessage, token);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private AsyncProducerConsumerQueue<ChaincodeMessage> AquireResponseChannelForTx(string channelId, string txId)
        {
            AsyncProducerConsumerQueue<ChaincodeMessage> channel = new AsyncProducerConsumerQueue<ChaincodeMessage>();
            using (_channelLock.Lock())
            {
                string key = GetTxKey(channelId, txId);
                if (responseChannel.ContainsKey(key))
                    throw new InvalidOperationException($"[{txId,-8}]Response channel already exists. Another request must be pending.");
                responseChannel.Add(key, channel);
            }
            logger.Trace($"[{txId,-8}]Response channel created.");
            return channel;
        }

        private async Task SendChannelAsync(ChaincodeMessage message, CancellationToken token)
        {
            using (await _channelLock.LockAsync(token).ConfigureAwait(false))
            {
                string key = GetTxKey(message.ChannelId, message.Txid);
                if (!responseChannel.ContainsKey(key))
                    throw new InvalidOperationException($"[{message.Txid},-8]sendChannel does not exist");
                logger.Debug($"[{message.Txid},-8.8]Before send");
                await responseChannel[key].EnqueueAsync(message, token).ConfigureAwait(false);
                logger.Debug($"[{message.Txid},-8]After send");
            }
        }


        private Task<ChaincodeMessage> ReceiveChannelAsync(AsyncProducerConsumerQueue<ChaincodeMessage> channel, CancellationToken token)
        {
            return channel.DequeueAsync(token);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ReleaseResponseChannelForTx(string channelId, string txId)
        {
            string key = GetTxKey(channelId, txId);
            lock (responseChannel)
            {
                responseChannel.Remove(key);
            }

            logger.Trace($"[{txId},-8]Response channel closed.");
        }

        /**
         * Marks a CHANNELID+UUID as either a transaction or a query
         *
         * @param uuid          ID to be marked
         * @param isTransaction true for transaction, false for query
         * @return whether or not the UUID was successfully marked
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool MarkIsTransaction(string channelId, string uuid, bool istransact)
        {
            if (isTransaction == null)
                return false;
            string key = GetTxKey(channelId, uuid);
            isTransaction.Add(key, istransact);
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void DeleteIsTransaction(string channelId, string uuid)
        {
            string key = GetTxKey(channelId, uuid);
            isTransaction.Remove(key);
        }


        /**
         * Handles requests to initialize chaincode
         *
         * @param message chaincode to be initialized
         */
        private Task HandleInitAsync(ChaincodeMessage message, CancellationToken token)
        {
            InternalHandleTransaction(message, token, chaincode.InitAsync);
            return Task.FromResult(0);
        }

        private Task HandleTransactionAsync(ChaincodeMessage message, CancellationToken token)
        {
            InternalHandleTransaction(message, token, chaincode.InvokeAsync);
            return Task.FromResult(0);
        }


        // handleTransaction Handles request to execute a transaction.
        private void InternalHandleTransaction(ChaincodeMessage message, CancellationToken token,Func<IChaincodeStub,CancellationToken,Task<Response>> action)
        {
            Task.Run(async()=>
            {
                try
                {
                    // Get the function and args from Payload
                    ChaincodeInput input = ChaincodeInput.Parser.ParseFrom(message.Payload);
                    // Mark as a transaction (allow put/del state)
                    MarkIsTransaction(message.ChannelId, message.Txid, true);
                    // Create the ChaincodeStub which the chaincode can use to
                    // callback
                    IChaincodeStub stub = new ChaincodeStub(message.ChannelId, message.Txid, this, input.Args.ToList(), message.Proposal);
                    // Call chaincode's init
                    Response result = await action(stub, token).ConfigureAwait(false);
                    if (result.Status >= Status.INTERNAL_SERVER_ERROR)
                    {
                        // Send ERROR with entire result.Message as payload
                        logger.Error($"[{message.Txid},-8]Init failed. Sending {ChaincodeMessage.Types.Type.Error}");
                        await QueueOutboundChaincodeMessageAsync(NewErrorEventMessage(message.ChannelId, message.Txid, result.Message, stub.Event), token).ConfigureAwait(false);
                    }
                    else
                    {
                        // Send COMPLETED with entire result as payload
                        logger.Trace($"[{message.Txid},-8]Init succeeded. Sending {ChaincodeMessage.Types.Type.Completed}");
                        await QueueOutboundChaincodeMessageAsync(NewCompletedEventMessage(message.ChannelId, message.Txid, result, stub.Event), token).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    logger.ErrorException($"[{message.Txid},-8]Init failed. Sending {ChaincodeMessage.Types.Type.Error}", e);
                    await QueueOutboundChaincodeMessageAsync(NewErrorEventMessage(message.ChannelId, message.Txid, e), token).ConfigureAwait(false);
                }
                finally
                {
                    // delete isTransaction entry
                    DeleteIsTransaction(message.ChannelId, message.Txid);
                }

            },token);
        }


        // handleGetState communicates with the validator to fetch the requested state information from the ledger.
        public virtual Task<ByteString> GetStateAsync(string channelId, string txId, string collection, string key, CancellationToken token = default(CancellationToken))
        {
            return InvokeChaincodeSupportAsync(NewGetStateEventMessage(channelId, txId, collection, key), token);
        }
        public virtual async Task<Dictionary<string,ByteString>> GetStateMetadataAsync(string channelId, string txId, string collection, string key, CancellationToken token = default(CancellationToken))
        {
            ByteString payload = await InvokeChaincodeSupportAsync(NewGetStateMetadataEventMessage(channelId, txId, collection, key), token).ConfigureAwait(false);
            try
            {
                StateMetadataResult stateMetadataResult = StateMetadataResult.Parser.ParseFrom(payload);
                Dictionary<string, ByteString> stateMetadataMap = new Dictionary<string, ByteString>();
                stateMetadataResult.Entries.ToList().ForEach(a=>stateMetadataMap.Add(a.Metakey,a.Value));
                return stateMetadataMap;
            }
            catch (InvalidProtocolBufferException e)
            {
                logger.Error($"[{txId},-8] unmarshall error");
                throw new Exception("Error unmarshalling StateMetadataResult.", e);
            }
        }
        private bool IsTransaction(string channelId, string uuid)
        {
            string key = GetTxKey(channelId, uuid);
            return isTransaction.ContainsKey(key) && isTransaction[key];
        }

        public virtual Task PutStateAsync(string channelId, string txId, string collection, string key, ByteString value, CancellationToken token = default(CancellationToken))
        {
            logger.Trace($"[{txId,-8}]Inside putstate (\"{collection}\":\"{key}\":\"{value}\"), isTransaction = {IsTransaction(channelId, txId)}");
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot put state in query context");
            return InvokeChaincodeSupportAsync(NewPutStateEventMessage(channelId, txId, collection, key, value), token);
        }
        public virtual Task PutStateMetadataAsync(string channelId, string txId, string collection, string key, string metakey, ByteString value, CancellationToken token = default(CancellationToken))
        {
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot put state metadata in query context");
            return InvokeChaincodeSupportAsync(NewPutStateMatadateEventMessage(channelId, txId, collection, key, metakey, value),token);
        }
        public virtual Task DeleteStateAsync(string channelId, string txId, string collection, string key, CancellationToken token = default(CancellationToken))
        {
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot del state in query context");
            return InvokeChaincodeSupportAsync(NewDeleteStateEventMessage(channelId, txId, collection, key), token);
        }

        public virtual Task<QueryResponse> GetStateByRangeAsync(string channelId, string txId, string collection, string startKey, string endKey, ByteString metadata, CancellationToken token = default(CancellationToken))
        {
            GetStateByRange gsr=new GetStateByRange { Collection = collection, StartKey = startKey, EndKey = endKey};
            if (metadata!=null)
                gsr.Metadata=metadata;
            return InvokeQueryResponseMessageAsync(channelId, txId, ChaincodeMessage.Types.Type.GetStateByRange, gsr.ToByteString(), token);
        }

        public Task<QueryResponse> QueryStateNextAsync(string channelId, string txId, string queryId, CancellationToken token = default(CancellationToken))
        {
            return InvokeQueryResponseMessageAsync(channelId, txId, ChaincodeMessage.Types.Type.QueryStateNext, new QueryStateNext {Id = queryId}.ToByteString(), token);
        }

        public Task<QueryResponse> QueryStateCloseAsync(string channelId, string txId, string queryId, CancellationToken token = default(CancellationToken))
        {
            return InvokeQueryResponseMessageAsync(channelId, txId, ChaincodeMessage.Types.Type.QueryStateClose, new QueryStateClose {Id = queryId}.ToByteString(), token);
        }

        public virtual Task<QueryResponse> GetQueryResultAsync(string channelId, string txId, string collection, string query, ByteString metadata, CancellationToken token = default(CancellationToken))
        {
            GetQueryResult gsr = new GetQueryResult { Query = query, Collection = collection };
            if (metadata != null)
                gsr.Metadata = metadata;
            return InvokeQueryResponseMessageAsync(channelId, txId, ChaincodeMessage.Types.Type.GetQueryResult, gsr.ToByteString(), token);
        }

        public virtual Task<QueryResponse> GetHistoryForKeyAsync(string channelId, string txId, string key, CancellationToken token = default(CancellationToken))
        {
            return InvokeQueryResponseMessageAsync(channelId, txId, ChaincodeMessage.Types.Type.GetHistoryForKey, new GetQueryResult {Query = key}.ToByteString(), token);
        }

        private async Task<QueryResponse> InvokeQueryResponseMessageAsync(string channelId, string txId, ChaincodeMessage.Types.Type type, ByteString payload, CancellationToken token)
        {
            try
            {
                return QueryResponse.Parser.ParseFrom(await InvokeChaincodeSupportAsync(NewEventMessage(type, channelId, txId, payload), token).ConfigureAwait(false));
            }
            catch (Exception e)
            {
                logger.Error($"[{txId,-8}s] unmarshall error");
                throw new InvalidOperationException("Error unmarshalling QueryResponse.", e);
            }
        }

        private async Task<ByteString> InvokeChaincodeSupportAsync(ChaincodeMessage message, CancellationToken token)
        {
            string channelId = message.ChannelId;
            string txId = message.Txid;

            try
            {
                // create a new response channel
                AsyncProducerConsumerQueue<ChaincodeMessage> respChannel = AquireResponseChannelForTx(channelId, txId);

                // send the message
                await QueueOutboundChaincodeMessageAsync(message, token).ConfigureAwait(false);

                // wait for response
                ChaincodeMessage response = await ReceiveChannelAsync(respChannel, token).ConfigureAwait(false);
                logger.Trace($"[{txId},-8]{response.Type} response received.");

                // handle response
                switch (response.Type)
                {
                    case ChaincodeMessage.Types.Type.Response:
                        logger.Trace($"[{txId},-8]Successful response received.");
                        return response.Payload;
                    case ChaincodeMessage.Types.Type.Error:
                        string error = $"[{txId},-8]Unsuccessful response received.";
                        logger.Error(error);
                        throw new InvalidOperationException(error);
                    default:
                        string error2 = $"[{txId},-8]Unexpected {response.Type} response received. Expected {ChaincodeMessage.Types.Type.Response} or {ChaincodeMessage.Types.Type.Error}.";
                        logger.Error(error2);
                        throw new InvalidOperationException(error2);
                }
            }
            finally
            {
                ReleaseResponseChannelForTx(channelId, txId);
            }
        }

        public virtual async Task<Response> InvokeChaincodeAsync(string channelId, string txId, string chaincodeName, List<byte[]> args, CancellationToken token = default(CancellationToken))
        {
            try
            {
                // create invocation specification of the chaincode to invoke
                ChaincodeSpec invocationSpec = new ChaincodeSpec {ChaincodeId = new ChaincodeID {Name = chaincodeName}, Input = new ChaincodeInput {Args = {args.Select(ByteString.CopyFrom)}}};
                // invoke other chaincode
                ByteString payload = await InvokeChaincodeSupportAsync(NewInvokeChaincodeMessage(channelId, txId, invocationSpec.ToByteString()), token).ConfigureAwait(false);

                // response message payload should be yet another chaincode
                // message (the actual response message)
                ChaincodeMessage responseMessage = ChaincodeMessage.Parser.ParseFrom(payload);
                // the actual response message must be of type COMPLETED
                logger.Trace($"[{txId},-8]{responseMessage.Type} response received from other chaincode.");
                if (responseMessage.Type == ChaincodeMessage.Types.Type.Completed)
                {
                    // success
                    return ToChaincodeResponse(Protos.Peer.ProposalResponsePackage.Response.Parser.ParseFrom(responseMessage.Payload));
                }

                // error
                return NewErrorChaincodeResponse(responseMessage.Payload.ToStringUtf8());
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }


        private static Response NewErrorChaincodeResponse(string message)
        {
            return new Response(Status.INTERNAL_SERVER_ERROR, message, null);
        }

        private static ChaincodeMessage NewGetStateEventMessage(string channelId, string txId, string collection, string key)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.GetState, channelId, txId, new GetState {Key = key, Collection = collection}.ToByteString());
        }
        private static ChaincodeMessage NewGetStateMetadataEventMessage(string channelId, string txId, string collection, string key)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.GetStateMetadata, channelId, txId, new GetStateMetadata { Key = key, Collection = collection}.ToByteString());
        }

        private static ChaincodeMessage NewPutStateEventMessage(string channelId, string txId, string collection, string key, ByteString value)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.PutState, channelId, txId, new PutState {Key = key, Value = value, Collection = collection}.ToByteString());
        }
        private static ChaincodeMessage NewPutStateMatadateEventMessage(string channelId, string txId, string collection, string key, string metakey, ByteString value)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.PutStateMetadata, channelId, txId, new PutStateMetadata {
                Key =key, Collection = collection,
                Metadata = new StateMetadata
                {
                    Metakey = metakey,  Value = value
                } }.ToByteString());
        }
        private static ChaincodeMessage NewDeleteStateEventMessage(string channelId, string txId, string collection, string key)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.DelState, channelId, txId, new DelState {Key = key, Collection = collection}.ToByteString());
        }

        private static ChaincodeMessage NewErrorEventMessage(string channelId, string txId, Exception throwable)
        {
            return NewErrorEventMessage(channelId, txId, throwable.StackTrace);
        }

        private static ChaincodeMessage NewErrorEventMessage(string channelId, string txId, string message)
        {
            return NewErrorEventMessage(channelId, txId, message, null);
        }

        private static ChaincodeMessage NewErrorEventMessage(string channelId, string txId, string message, ChaincodeEvent evnt)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.Error, channelId, txId, ByteString.CopyFromUtf8(message), evnt);
        }

        private static ChaincodeMessage NewCompletedEventMessage(string channelId, string txId, Response response, ChaincodeEvent evnt)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.Completed, channelId, txId, ToProtoResponse(response).ToByteString(), evnt);
        }

        private static ChaincodeMessage NewInvokeChaincodeMessage(string channelId, string txId, ByteString payload)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.InvokeChaincode, channelId, txId, payload, null);
        }

        private static ChaincodeMessage NewRegisterChaincodeMessage(ChaincodeID chaincodeId)
        {
            return new ChaincodeMessage {Type = ChaincodeMessage.Types.Type.Register, Payload = chaincodeId.ToByteString()};
        }

        private static ChaincodeMessage NewEventMessage(ChaincodeMessage.Types.Type type, string channelId, string txId, ByteString payload)
        {
            return NewEventMessage(type, channelId, txId, payload, null);
        }

        private static ChaincodeMessage NewEventMessage(ChaincodeMessage.Types.Type type, string channelId, string txId, ByteString payload, ChaincodeEvent evnt)
        {
            ChaincodeMessage msg = new ChaincodeMessage {Type = type, ChannelId = channelId, Txid = txId, Payload = payload};
            if (evnt != null)
                msg.ChaincodeEvent = evnt;
            return msg;
        }

        private static Protos.Peer.ProposalResponsePackage.Response ToProtoResponse(Response response)
        {
            Protos.Peer.ProposalResponsePackage.Response resp = new Protos.Peer.ProposalResponsePackage.Response() {Status = (int) response.Status};
            if (response.Message != null)
                resp.Message = response.Message;
            if (response.Payload != null)
                resp.Payload = ByteString.CopyFrom(response.Payload);
            return resp;
        }

        private static Response ToChaincodeResponse(Protos.Peer.ProposalResponsePackage.Response response)
        {
            return new Response((Status) response.Status, response.Message, response.Payload.ToByteArray());
        }
    }
}