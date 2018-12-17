using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Serilog;


#pragma warning disable 4014

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class ChaincodeSupportStream
    {
        private static readonly ILogger logger = Log.ForContext<ChaincodeSupportStream>();
        private Handler handler;

        public async Task ProcessAndBlockAsync(Channel connection, IChaincodeAsync chaincode, string id, CancellationToken token = default(CancellationToken))
        {
            ChaincodeSupport.ChaincodeSupportClient stub = new ChaincodeSupport.ChaincodeSupportClient(connection);
            logger.Information("Connecting to peer.");
            AsyncDuplexStreamingCall<ChaincodeMessage, ChaincodeMessage> requestObserver = stub.Register();
            CancellationTokenSource src = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task.Run(async () =>
            {
                try
                {
                    while (await requestObserver.ResponseStream.MoveNext(src.Token).ConfigureAwait(false))
                    {
                        ChaincodeMessage message = requestObserver.ResponseStream.Current;
                        logger.Debug("Got message from peer: " + message.ToJsonString());
                        try
                        {
                            logger.Debug($"[{message.Txid}]Received message {message.Type} from org.hyperledger.fabric.shim");
                            await handler.OnChaincodeMessageAsync(message, src.Token).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            logger.Error($"Server Error: {e.Message}");
                        }
                    }

                    src.Cancel();
                }
                catch (OperationCanceledException)
                {
                    //ignored
                }
                catch (Exception e)
                {
                    logger.Error($"Server Error: {e.Message}");
                    src.Cancel();
                }

                logger.Information("Chaincode stream is shutting down.");
            }, src.Token);


            // Create the org.hyperledger.fabric.shim handler responsible for all
            // control logic
            //Thread 2 Process Client Requests
            handler = await Handler.CreateAsync(new ChaincodeID {Name = id}, chaincode, src.Token).ConfigureAwait(false);
            while (true)
            {
                try
                {
                    ChaincodeMessage message = null;
                    try
                    {
                        message = await handler.NextOutboundChaincodeMessageAsync(src.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        //ignored processed below
                    }

                    if (src.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await requestObserver.RequestStream.CompleteAsync().ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            //Ignored (Server died)
                        }

                        return;
                    }

                    await requestObserver.RequestStream.WriteAsync(message).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    logger.Error(e,e.Message);
                    break;
                }
            }

        }
    }
}