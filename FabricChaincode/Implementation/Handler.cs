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
using Grpc.Core;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Fsm;
using Hyperledger.Fabric.Shim.Fsm.Exceptions;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Logging;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class Handler
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(Handler));
        private readonly ChaincodeBase chaincode;

        private readonly AsyncDuplexStreamingCall<ChaincodeMessage, ChaincodeMessage> chatStream;

        private readonly FSM fsm;

        private readonly Dictionary<string, bool> isTransaction;
        public BlockingCollection<NextStateInfo> nextState;
        private readonly Dictionary<string, BlockingCollection<ChaincodeMessage>> responseChannel;

        public Handler(AsyncDuplexStreamingCall<ChaincodeMessage, ChaincodeMessage> chatStream, ChaincodeBase chaincode)
        {
            this.chatStream = chatStream;
            this.chaincode = chaincode;

            responseChannel = new Dictionary<string, BlockingCollection<ChaincodeMessage>>();
            isTransaction = new Dictionary<string, bool>();
            nextState = new BlockingCollection<NextStateInfo>();

            fsm = new FSM("created");

            fsm.AddEvents(
                //            Event Name              From           To
                new EventDesc(ChaincodeMessage.Types.Type.Register.ToString().ToUpperInvariant(), "created", "established"), new EventDesc(ChaincodeMessage.Types.Type.Ready.ToString().ToUpperInvariant(), "established", "ready"), new EventDesc(ChaincodeMessage.Types.Type.Error.ToString().ToUpperInvariant(), "init", "established"), new EventDesc(ChaincodeMessage.Types.Type.Response.ToString().ToUpperInvariant(), "init", "init"), new EventDesc(ChaincodeMessage.Types.Type.Init.ToString().ToUpperInvariant(), "ready", "ready"), new EventDesc(ChaincodeMessage.Types.Type.Transaction.ToString().ToUpperInvariant(), "ready", "ready"), new EventDesc(ChaincodeMessage.Types.Type.Response.ToString().ToUpperInvariant(), "ready", "ready"), new EventDesc(ChaincodeMessage.Types.Type.Error.ToString().ToUpperInvariant(), "ready", "ready"), new EventDesc(ChaincodeMessage.Types.Type.Completed.ToString().ToUpperInvariant(), "init", "ready"), new EventDesc(ChaincodeMessage.Types.Type.Completed.ToString().ToUpperInvariant(), "ready", "ready"));

            fsm.AddCallbacks(
                //         Type          Trigger                Callback
                new CBDesc(CallbackType.BEFORE_EVENT, ChaincodeMessage.Types.Type.Register.ToString().ToUpperInvariant(), (evnt) => BeforeRegistered(evnt)), new CBDesc(CallbackType.AFTER_EVENT, ChaincodeMessage.Types.Type.Response.ToString().ToUpperInvariant(), (evnt) => AfterResponse(evnt)), new CBDesc(CallbackType.AFTER_EVENT, ChaincodeMessage.Types.Type.Error.ToString().ToUpperInvariant(), (evnt) => AfterError(evnt)), new CBDesc(CallbackType.BEFORE_EVENT, ChaincodeMessage.Types.Type.Init.ToString().ToUpperInvariant(), (evnt) => BeforeInit(evnt)), new CBDesc(CallbackType.BEFORE_EVENT, ChaincodeMessage.Types.Type.Transaction.ToString().ToUpperInvariant(), (evnt) => BeforeTransaction(evnt)));
        }

        private void TriggerNextState(ChaincodeMessage message, bool send)
        {
            if (logger.IsTraceEnabled())
                logger.Trace($"triggerNextState for message {message}");
            nextState.Add(new NextStateInfo(message, send));
            nextState.CompleteAdding();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SerialSend(ChaincodeMessage message)
        {
            logger.Debug($"[{message.Txid,-8}]Sending {message.Type} message to peer.", message.Txid, message.Type);
            if (logger.IsTraceEnabled())
                logger.Trace($"[{message.Txid,-8}]ChaincodeMessage: {message.ToJsonString()}");
            try
            {
                chatStream.RequestStream.WriteAsync(message).RunAndUnwarp();
                if (logger.IsTraceEnabled())
                    logger.Trace($"[{message.Txid,-8}] {message.Type} sent.");
            }
            catch (Exception e)
            {
                logger.Error($"[{message.Txid,-8}] Error sending {message.Type}: {e.Message}");
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private BlockingCollection<ChaincodeMessage> AquireResponseChannelForTx(string channelId, string txId)
        {
            BlockingCollection<ChaincodeMessage> channel = new BlockingCollection<ChaincodeMessage>();
            string key = channelId + txId;
            lock (responseChannel)
            {
                if (responseChannel.ContainsKey(key))
                    throw new InvalidOperationException($"[{txId,-8}]Response channel already exists. Another request must be pending.");
                responseChannel.Add(key, channel);
            }

            if (logger.IsTraceEnabled())
                logger.Trace($"[{txId,-8}]Response channel created.");
            return channel;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SendChannel(ChaincodeMessage message)
        {
            string key = message.ChannelId + message.Txid;
            lock (responseChannel)
            {
                if (!responseChannel.ContainsKey(key))
                    throw new InvalidOperationException($"[{message.Txid},-8]sendChannel does not exist");
                logger.Debug($"[{message.Txid},-8]Before send");
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
                logger.Debug("channel.take() failed");
                // Channel has been closed?
                // TODO
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ReleaseResponseChannelForTx(string channelId, string txId)
        {
            string key = channelId + txId;
            lock (responseChannel)
            {
                responseChannel.Remove(key);
            }

            if (logger.IsTraceEnabled())
                logger.Trace($"[{txId},-8]Response channel closed.");
        }

        /**
         * Marks a CHANNELID+UUID as either a transaction or a query
         *
         * @param channelId
         *            channel ID to be marked
         * @param uuid
         *            ID to be marked
         * @param isTransaction
         *            true for transaction, false for query
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

            string key = channelId + uuid;
            isTransaction.Add(key, istransact);
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void DeleteIsTransaction(string channelId, string uuid)
        {
            string key = channelId + uuid;
            isTransaction.Remove(key);
        }

        private void BeforeRegistered(FSMEvent evnt)
        {
            ExtractMessageFromEvent(evnt);
            logger.Debug($"Received {ChaincodeMessage.Types.Type.Register}, ready for invocations");
        }

        /**
	 * Handles requests to initialize chaincode
	 *
	 * @param message
	 *            chaincode to be initialized
	 */
        private void HandleInit(ChaincodeMessage message)
        {
            HandleTransaction(message);
        }

        // enterInitState will initialize the chaincode if entering init from established.
        private void BeforeInit(FSMEvent evnt)
        {
            logger.Debug($"Before {evnt.Name} event.");
            logger.Debug($"Current state {fsm.Current}");
            ChaincodeMessage message = ExtractMessageFromEvent(evnt);
            logger.Debug($"[{message.Txid},-8]Received {message.Type}, initializing chaincode");
            if (message.Type == ChaincodeMessage.Types.Type.Init)
            {
                // Call the chaincode's Run function to initialize
                HandleInit(message);
            }
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
                    IChaincodeStub stub = new ChaincodeStub(message.ChannelId, message.Txid, this, input.Args.ToList());

                    // Call chaincode's init
                    Response result = chaincode.Init(stub);

                    if (result.Status >= Status.INTERNAL_SERVER_ERROR)
                    {
                        // Send ERROR with entire result.Message as payload
                        logger.Error($"[{message.Txid},-8]Init failed. Sending {ChaincodeMessage.Types.Type.Error}");
                        TriggerNextState(NewErrorEventMessage(message.ChannelId, message.Txid, result.Message, stub.Event), true);
                    }
                    else
                    {
                        // Send COMPLETED with entire result as payload
                        logger.Debug($"[{message.Txid},-8]Init succeeded. Sending {ChaincodeMessage.Types.Type.Completed}");
                        TriggerNextState(NewCompletedEventMessage(message.ChannelId, message.Txid, result, stub.Event), true);
                    }
                }
                catch (Exception e)
                {
                    logger.ErrorException($"[{message.Txid},-8]Init failed. Sending {ChaincodeMessage.Types.Type.Error}", e);
                    TriggerNextState(NewErrorEventMessage(message.ChannelId, message.Txid, e), true);
                }
                finally
                {
                    // delete isTransaction entry
                    DeleteIsTransaction(message.ChannelId, message.Txid);
                }
            });
        }

        // enterTransactionState will execute chaincode's Run if coming from a TRANSACTION event.
        private void BeforeTransaction(FSMEvent evnt)
        {
            ChaincodeMessage message = ExtractMessageFromEvent(evnt);
            logger.Debug($"[{message.Txid}Received {message.Type}, invoking transaction on chaincode(src:{evnt.Src}, dst:{evnt.Dst})");
            if (message.Type == ChaincodeMessage.Types.Type.Transaction)
            {
                // Call the chaincode's Run function to invoke transaction
                HandleTransaction(message);
            }
        }

        // afterResponse is called to deliver a response or error to the chaincode stub.
        private void AfterResponse(FSMEvent evnt)
        {
            ChaincodeMessage message = ExtractMessageFromEvent(evnt);
            try
            {
                SendChannel(message);
                logger.Debug($"[{message.Txid,-8}Received {message.Type}, communicated (state:{fsm.Current})");
            }
            catch (Exception e)
            {
                logger.Error($"[{message.Txid,-8}error sending {message.Type} (state:{fsm.Current}): {e.Message}");
            }
        }

        private ChaincodeMessage ExtractMessageFromEvent(FSMEvent evnt)
        {
            try
            {
                return (ChaincodeMessage) evnt.Args[0];
            }
            catch (Exception e)
            {
                InvalidOperationException error = new InvalidOperationException("No chaincode message found in event.", e);
                evnt.Cancel(error);
                throw error;
            }
        }

        private void AfterError(FSMEvent evnt)
        {
            ChaincodeMessage message = ExtractMessageFromEvent(evnt);
            /*
             * TODO- revisit. This may no longer be needed with the
             * serialized/streamlined messaging model There are two situations in
             * which the ERROR event can be triggered:
             *
             * 1. When an error is encountered within handleInit or
             * handleTransaction - some issue at the chaincode side; In this case
             * there will be no responseChannel and the message has been sent to the
             * validator.
             *
             * 2. The chaincode has initiated a request (get/put/del state) to the
             * validator and is expecting a response on the responseChannel; If
             * ERROR is received from validator, this needs to be notified on the
             * responseChannel.
             */
            try
            {
                SendChannel(message);
            }
            catch (Exception e)
            {
                logger.Debug($"[{message.Txid,-8}Error received from validator {message.Type}, communicated(state:{fsm.Current}). {e.Message}");
            }
        }

        // handleGetState communicates with the validator to fetch the requested state information from the ledger.
        public ByteString GetState(string channelId, string txId, string key)
        {
            return InvokeChaincodeSupport(NewGetStateEventMessage(channelId, txId, key));
        }

        private bool IsTransaction(string channelId, string uuid)
        {
            string key = channelId + uuid;
            return isTransaction.ContainsKey(key) && isTransaction[key];
        }

        public void PutState(string channelId, string txId, string key, ByteString value)
        {
            logger.Debug($"[{txId,-8}]Inside putstate (\"{key}\":\"{value}\"), isTransaction = {IsTransaction(channelId, txId)}");
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot put state in query context");
            InvokeChaincodeSupport(NewPutStateEventMessage(channelId, txId, key, value));
        }

        public void DeleteState(string channelId, string txId, string key)
        {
            if (!IsTransaction(channelId, txId))
                throw new InvalidOperationException("Cannot del state in query context");
            InvokeChaincodeSupport(NewDeleteStateEventMessage(channelId, txId, key));
        }

        public QueryResponse GetStateByRange(string channelId, string txId, string startKey, string endKey)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.GetStateByRange, new GetStateByRange {StartKey = startKey, EndKey = endKey}.ToByteString());
        }

        public QueryResponse QueryStateNext(string channelId, string txId, string queryId)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.QueryStateNext, new QueryStateNext {Id = queryId}.ToByteString());
        }

        public void QueryStateClose(string channelId, string txId, string queryId)
        {
            InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.QueryStateClose, new QueryStateClose {Id = queryId}.ToByteString());
        }

        public QueryResponse GetQueryResult(string channelId, string txId, string query)
        {
            return InvokeQueryResponseMessage(channelId, txId, ChaincodeMessage.Types.Type.GetQueryResult, new GetQueryResult {Query = query}.ToByteString());
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
                logger.Error($"[{txId}unmarshall error");
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
                SerialSend(message);

                // wait for response
                ChaincodeMessage response = ReceiveChannel(respChannel);
                logger.Debug($"[{txId},-8]{response.Type} response received.");

                // handle response
                switch (response.Type)
                {
                    case ChaincodeMessage.Types.Type.Response:
                        logger.Debug($"[{txId},-8]Successful response received.");
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
                logger.Debug($"[{txId},-8]{responseMessage.Type} response received from other chaincode.");
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

        // handleMessage message handles loop for org.hyperledger.fabric.shim side
        // of chaincode/validator stream.
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void HandleMessage(ChaincodeMessage message)
        {
            if (message.Type == ChaincodeMessage.Types.Type.Keepalive)
            {
                logger.Debug($"[{message.Txid},-8]Received KEEPALIVE message, do nothing");
                // Received a keep alive message, we don't do anything with it for
                // now and it does not touch the state machine
                return;
            }

            logger.Debug($"[{message.Txid},-8]Handling ChaincodeMessage of type: {message.Type}(state:{fsm.Current}x)");

            if (fsm.EventCannotOccur(message.Type.ToString().ToUpperInvariant()))
            {
                string errStr = $"[{message.Txid},-8]Chaincode handler org.hyperledger.fabric.shim.fsm cannot handle message ({message.Type}) with payload size ({message.Payload.Length}) while in state: {fsm.Current}";
                SerialSend(NewErrorEventMessage(message.ChannelId, message.Txid, errStr));
                throw new InvalidOperationException(errStr);
            }

            // Filter errors to allow NoTransitionError and CanceledError
            // to not propagate for cases where embedded Err == nil.
            try
            {
                fsm.RaiseEvent(message.Type.ToString().ToUpperInvariant(), message);
            }
            catch (NoTransitionException e)
            {
                if (e.InnerException != null) throw;
                logger.Debug($"[{message.Txid},-8]Ignoring NoTransitionError");
            }
            catch (CancelledException e)
            {
                if (e.InnerException != null) throw;
                logger.Debug($"[{message.Txid},-8]Ignoring CanceledError", message.Txid);
            }
        }


        private static Response NewErrorChaincodeResponse(string message)
        {
            return new Response(Status.INTERNAL_SERVER_ERROR, message, null);
        }

        private static ChaincodeMessage NewGetStateEventMessage(string channelId, string txId, string key)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.GetState, channelId, txId, new GetState {Key = key}.ToByteString());
        }

        private static ChaincodeMessage NewPutStateEventMessage(string channelId, string txId, string key, ByteString value)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.PutState, channelId, txId, new PutState {Key = key, Value = value}.ToByteString());
        }

        private static ChaincodeMessage NewDeleteStateEventMessage(string channelId, string txId, string key)
        {
            return NewEventMessage(ChaincodeMessage.Types.Type.DelState, channelId, txId, new DelState {Key = key}.ToByteString());
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

        private static ChaincodeMessage NewEventMessage(ChaincodeMessage.Types.Type type, string channelId, string txId, ByteString payload)
        {
            return NewEventMessage(type, channelId, txId, payload, null);
        }

        private static ChaincodeMessage NewEventMessage(ChaincodeMessage.Types.Type type, string channelId, string txId, ByteString payload, ChaincodeEvent evnt)
        {
            if (evnt == null)
            {
                return new ChaincodeMessage {Type = type, Txid = txId, Payload = payload, ChannelId = channelId};
            }
            else
            {
                return new ChaincodeMessage {Type = type, Txid = txId, Payload = payload, ChannelId = channelId, ChaincodeEvent = evnt};
            }
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