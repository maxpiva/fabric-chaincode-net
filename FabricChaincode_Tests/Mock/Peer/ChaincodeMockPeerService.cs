using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Serilog;


namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    public class ChaincodeMockPeerService : ChaincodeSupport.ChaincodeSupportBase
    {
        private static readonly ILogger logger = Log.ForContext<ChaincodeMockPeerService>();
        private int lastExecutedStepNumber;
        private ChaincodeMessage lastMessageRcvd;
        private ChaincodeMessage lastMessageSend;
        private readonly List<IScenarioStep> scenario;
        IServerStreamWriter<ChaincodeMessage>  writer;

        public int LastExecutedStep => lastExecutedStepNumber;
        public ChaincodeMessage LastMessageSend => lastMessageSend;
        public ChaincodeMessage LastMessageRcvd => lastMessageRcvd;

        public void Send(ChaincodeMessage msg)
        {
            lastMessageSend = msg;
            writer.WriteAsync(msg).RunAndUnwrap();
        }
        public ChaincodeMockPeerService(List<IScenarioStep> scenario)
        {
            this.scenario = scenario;
            lastExecutedStepNumber = 0;
        }

        /**
        * Attaching observer to steams
        *
        * @param responseObserver
        * @return
        */
        public override async Task Register(IAsyncStreamReader<ChaincodeMessage> requestStream, IServerStreamWriter<ChaincodeMessage> responseStream, ServerCallContext context)
        {
            try
            {
                writer = responseStream;
                while (await requestStream.MoveNext().ConfigureAwait(false))
                {
                    ChaincodeMessage chaincodeMessage = requestStream.Current;
                    logger.Information("Mock peer => Got message: " + chaincodeMessage);
                    lastMessageRcvd = chaincodeMessage;
                    if (scenario.Count > 0)
                    {
                        IScenarioStep step = scenario[0];
                        scenario.RemoveAt(0);
                        if (step.Expected(chaincodeMessage))
                        {
                            List<ChaincodeMessage> nextSteps = step.Next();
                            foreach (ChaincodeMessage m in nextSteps)
                            {
                                lastMessageSend = m;
                                logger.Information("Mock peer => Sending response message: " + m);
                                await responseStream.WriteAsync(m).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            logger.Warning($"Non expected message rcvd in step {step.GetType().Name}");
                        }

                    lastExecutedStepNumber++;
                    }
                }
            }
            catch (Exception)
            {
                //Ignored
            }

        }
    }
}