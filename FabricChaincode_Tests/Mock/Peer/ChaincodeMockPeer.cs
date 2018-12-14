/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Logging;

namespace Hyperledger.Fabric.Shim.Tests.Mock.Peer
{
    /**
     * Mock peer implementation
     */
    public class ChaincodeMockPeer
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(ChaincodeMockPeer));



        private Server server;
        private readonly ChaincodeMockPeerService service;

        /**
         * Constructor
         *
         * @param scenario list of scenario steps
         * @param port     mock peer communication port
         * @throws IOException
         */
        public ChaincodeMockPeer(List<IScenarioStep> scenario, int port)
        {
            service = new ChaincodeMockPeerService(scenario);
            server = new Server {Services = {ChaincodeSupport.BindService(service)}, Ports = {new ServerPort("127.0.0.1", port, ServerCredentials.Insecure)}};
        }

        /**
         * Check last executed step number, to check where in scenario we stopped
         *
         * @return
         */
        public int LastExecutedStep => service?.LastExecutedStep ?? -1;

        /**
         * @return last received message from chaincode
         */
        public ChaincodeMessage LastMessageRcvd => service?.LastMessageRcvd;

        /**
         * @return last message sent by peer to chaincode
         */
        public ChaincodeMessage LastMessageSend => service?.LastMessageSend;

        /**
         * Start serving requests.
         */
        public void Start()
        {
            server.Start();
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }
        public void ProcessExit(object ob, EventArgs args)
        {
            Stop();
        }
        /**
         * Stop serving requests and shutdown resources.
         */
        public void Stop()
        {
            AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
            try
            {
                server?.KillAsync().Wait();
            }
            catch (Exception)
            {
                //ignored
            }
            server = null;
        }

        /**
         * Send message from mock peer to chaincode (to start init, invoke, etc)
         *
         * @param msg
         */
        public void Send(ChaincodeMessage msg)
        {
            logger.Info("Mock peer => Sending message: " + msg);
            service.Send(msg);
        }


        /**
         * Creates new isntanse of mock peer server, starts it and returns
         *
         * @param scenario
         * @return
         * @throws Exception
         */
        public static ChaincodeMockPeer StartServer(List<IScenarioStep> scenario)
        {
            ChaincodeMockPeer server = new ChaincodeMockPeer(scenario, 7052);
            server.Start();
            return server;
        }
    }
}