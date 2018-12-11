/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Logging;
using Newtonsoft.Json;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class Handler
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(Handler));

        private readonly IChaincode chaincode;


        private readonly Dictionary<string, bool> isTransaction;
        private readonly BlockingCollection<ChaincodeMessage> outboundChaincodeMessages = new BlockingCollection<ChaincodeMessage>();
        private readonly Dictionary<string, BlockingCollection<ChaincodeMessage>> responseChannel;

        public Handler(ChaincodeID chaincodeId, IChaincode chaincode)
        {
            this.chaincode = chaincode;
            State = CCState.CREATED;
            QueueOutboundChaincodeMessage(NewRegisterChaincodeMessage(chaincodeId));
        }

        public CCState State { get; private set; }

        public ChaincodeMessage NextOutboundChaincodeMessage()
        {
            try
            {
                return outboundChaincodeMessages.Take();
            }
            catch (InvalidOperationException e)
            {
                return NewErrorEventMessage("UNKNOWN", "UNKNOWN", e);
            }
        }

        public void OnChaincodeMessage(ChaincodeMessage chaincodeMessage)
        {
            logger.Trace($"[{chaincodeMessage.Txid,-8}s] {ToJsonString(chaincodeMessage)}");
            HandleChaincodeMessage(chaincodeMessage);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void HandleChaincodeMessage(ChaincodeMessage message)
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
                    HandleReady(message);
                    break;
                default:
                    logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
                    break;
            }
        }

        private void HandleCreated(ChaincodeMessage message)
        {
            if (message.Type == ChaincodeMessage.Types.Type.Register)
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
                State = CCState.ESTABLISHED;
                logger.Trace($"[{message.Txid,-8}s] Received READY: ready for invocations");
            }
            else
                logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
        }

        private void HandleReady(ChaincodeMessage message)
        {
            switch (message.Type)
            {
                case ChaincodeMessage.Types.Type.Response:
                    logger.Trace($"[{message.Txid,-8}s] Received RESPONSE: publishing to channel");
                    SendChannel(message);
                    break;
                case ChaincodeMessage.Types.Type.Error:
                    logger.Trace($"[{message.Txid,-8}s] Received ERROR: publishing to channel");
                    SendChannel(message);
                    break;
                case ChaincodeMessage.Types.Type.Init:
                    logger.Trace($"[{message.Txid,-8}s] Received INIT: invoking chaincode init");
                    HandleInit(message);
                    break;
                case ChaincodeMessage.Types.Type.Transaction:
                    logger.Trace($"[{message.Txid,-8}s] Received TRANSACTION: invoking chaincode");
                    HandleTransaction(message);
                    break;
                default:
                    logger.Warn($"[{message.Txid,-8}s] Received {message.Type}: cannot handle");
                    break;
            }
        }

        private string GetTxKey(string channelId, string txid)
        {
            return channelId + txid;
        }

        private void QueueOutboundChaincodeMessage(ChaincodeMessage chaincodeMessage)
        {
            outboundChaincodeMessages.Add(chaincodeMessage);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private BlockingCollection<ChaincodeMessage> AquireResponseChannelForTx(string channelId, string txId)
        {
            BlockingCollection<ChaincodeMessage> channel = new BlockingCollection<ChaincodeMessage>();
            string key = GetTxKey(channelId, txId);
            lock (responseChannel)
            {
                if (responseChannel.ContainsKey(key))
                    throw new InvalidOperationException($"[{txId,-8}]Response channel already exists. Another request must be pending.");
                responseChannel.Add(key, channel);
            }

            logger.Trace($"[{txId,-8}]Response channel created.");
            return channel;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SendChannel(ChaincodeMessage message)
        {
            string key = GetTxKey(message.ChannelId, message.Txid);
            lock (responseChannel)
            {
                if (!responseChannel.ContainsKey(key))
                    throw new InvalidOperationException($"[{message.Txid},-8]sendChannel does not exist");
                logger.Debug($"[{message.Txid},-8.8]Before send");
                responseChannel[key].Add(message);
                responseChannel[key].CompleteAdding();
                logger.Debug($"[{message.Txid},-8]After send");
            }
        }


        private ChaincodeMessage ReceiveChannel(BlockingCollection<ChaincodeMessage> channel)
        {
            try
            {
                return channel.Take();
            }
            catch (Exception)
            {
                logger.Trace("channel.take() failed");
                // Channel has been closed?
                // TODO
                return null;
            }
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
            {
                return false;
            }

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
        private void HandleInit(ChaincodeMessage message)
        {
            HandleTransaction(message);
        }


        // handleTransaction Handles request to execute a transaction.
        private void HandleTransaction(ChaincodeMessage message)
        {
            Task.Run(() =>
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
                    Response result = chaincode.Init(stub);

                    if (result.Status >= Status.INTERNAL_SERVER_ERROR)
                    {
                        // Send ERROR with entire result.Message as payload
                        logger.Error($"[{message.Txid},-8]Init failed. Sending {ChaincodeMessage.Types.Type.Error}");
                        QueueOutboundChaincodeMessage(NewErrorEventMessage(message.ChannelId, message.Txid, result.Message, stub.Event));
                    }
                    else
                    {
                        // Send COMPLETED with entire result as payload
                        logger.Trace($"[{message.Txid},-8]Init succeeded. Sending {ChaincodeMessage.Types.Type.Completed}");
                        QueueOutboundChaincodeMessage(NewCompletedEventMessage(message.ChannelId, message.Txid, result, stub.Event));
                    }
                }
                catch (Exception e)
                {
                    logger.ErrorException($"[{message.Txid},-8]Init failed. Sending {ChaincodeMessage.Types.Type.Error}", e);
                    QueueOutboundChaincodeMessage(NewErrorEventMessage(message.ChannelId, message.Txid, e));
                }
                finally
                {
                    // delete isTransaction entry
                    DeleteIsTransaction(message.ChannelId, message.Txid);
                }
            });
        }


        // handleGetState communicates with the validator to fetch the requested state information from the ledger.
        public ByteString GetState(string channelId, string txId, string collection, string key)
        {
            return InvokeChaincodeSupport(NewGetStateEventMessage(channelId, txId, collection, key));
        }

        private bool IsTransaction(string channelId, string uuid)
        {
            string key = GetTxKey(channelId, uuid);
            return isTransaction.ContainsKey(key) && isTransaction[key];
        }

        public void PutState(string channelId, string txId, string collection, string key, ByteString value)
        {
            logger.Trace($"[{txId,-8}]Inside putstate (\"{collection}\":\"{key}\":\"{value}\"), isTransaction = {IsTransaction(channelId, txId)}");
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot put state in query context");
            InvokeChaincodeSupport(NewPutStateEventMessage(channelId, txId, collection, key, value));
        }

        public void DeleteState(string channelId, string txId, string collection, string key)
        {
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot del state in query context");
            InvokeChaincodeSupport(NewDeleteStateEventMessage(channelId, txId, collection, key));
        }

        public QueryResponse GetStateByRange(string channelId, string txId, string collection, string startKey, string endKey)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.GetStateByRange, new GetStateByRange {StartKey = startKey, EndKey = endKey, Collection = collection}.ToByteString());
        }

        public QueryResponse QueryStateNext(string channelId, string txId, string queryId)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.QueryStateNext, new QueryStateNext {Id = queryId}.ToByteString());
        }

        public void QueryStateClose(string channelId, string txId, string queryId)
        {
            InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.QueryStateClose, new QueryStateClose {Id = queryId}.ToByteString());
        }

        public QueryResponse GetQueryResult(string channelId, string txId, string collection, string query)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.GetQueryResult, new GetQueryResult {Query = query, Collection = collection}.ToByteString());
        }

        public QueryResponse GetHistoryForKey(string channelId, string txId, string key)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.GetHistoryForKey, new GetQueryResult {Query = key}.ToByteString());
        }

        private QueryResponse InvokeQueryResponseMessage(string channelId, string txId, ChaincodeMessage.Types.Type type, ByteString payload)
        {
            try
            {
                return QueryResponse.Parser.ParseFrom(InvokeChaincodeSupport(NewEventMessage(type, channelId, txId, payload)));
            }
            catch (Exception e)
            {
                logger.Error($"[{txId,-8}s] unmarshall error");
                throw new InvalidOperationException("Error unmarshalling QueryResponse.", e);
            }
        }

        private ByteString InvokeChaincodeSupport(ChaincodeMessage message)
        {
            string channelId = message.ChannelId;
            string txId = message.Txid;

            try
            {
                // create a new response channel
                BlockingCollection<ChaincodeMessage> respChannel = AquireResponseChannelForTx(channelId, txId);

                // send the message
                QueueOutboundChaincodeMessage(message);

                // wait for response
                ChaincodeMessage response = ReceiveChannel(respChannel);
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

        public Response InvokeChaincode(string channelId, string txId, string chaincodeName, List<byte[]> args)
        {
            try
            {
                // create invocation specification of the chaincode to invoke
                ChaincodeSpec invocationSpec = new ChaincodeSpec {ChaincodeId = new ChaincodeID {Name = chaincodeName}, Input = new ChaincodeInput {Args = {args.Select(ByteString.CopyFrom)}}};
                // invoke other chaincode
                ByteString payload = InvokeChaincodeSupport(NewInvokeChaincodeMessage(channelId, txId, invocationSpec.ToByteString()));

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
                else
                {
                    // error
                    return NewErrorChaincodeResponse(responseMessage.Payload.ToStringUtf8());
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }

        private static string ToJsonString(ChaincodeMessage message)
        {
            try
            {
                return JsonConvert.SerializeObject(message);
            }
            catch (InvalidProtocolBufferException e)
            {
                return $"{{ Type: {message.Type}, TxId: {message.Txid} }}";
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

        private static ChaincodeMessage NewPutStateEventMessage(string channelId, string txId, string collection, string key, ByteString value)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.PutState, channelId, txId, new PutState {Key = key, Value = value, Collection = collection}.ToByteString());
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