/*
Copyright DTCC 2016 All Rights Reserved.

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
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Implementation;
using Hyperledger.Fabric.Shim.Logging;
using Mono.Options;

namespace Hyperledger.Fabric.Shim
{
    public abstract class ChaincodeBase : IChaincode
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(ChaincodeBase));

        public static readonly string DEFAULT_HOST = "127.0.0.1";
        public static readonly int DEFAULT_PORT = 7051;

        private static readonly string CORE_CHAINCODE_ID_NAME = "CORE_CHAINCODE_ID_NAME";
        private static readonly string CORE_PEER_ADDRESS = "CORE_PEER_ADDRESS";
        private static readonly string CORE_PEER_TLS_ENABLED = "CORE_PEER_TLS_ENABLED";
        private static readonly string CORE_PEER_TLS_SERVERHOSTOVERRIDE = "CORE_PEER_TLS_SERVERHOSTOVERRIDE";
        private static readonly string CORE_PEER_TLS_ROOTCERT_FILE = "CORE_PEER_TLS_ROOTCERT_FILE";

        private Handler handler;
        private bool help;

        private string host = DEFAULT_HOST;
        private string hostOverrideAuthority = "";
        private string id;
        private OptionSet options;
        private int port = DEFAULT_PORT;
        private string rootCertFile = "/etc/hyperledger/fabric/peer.crt";
        private bool tlsEnabled;


        public abstract Response Init(IChaincodeStub stub);


        public abstract Response Invoke(IChaincodeStub stub);


        private void ProcessCommandLineOptions(string[] args)
        {
            try
            {
                string peerAddress = null;
                string hname = null;
                options = new OptionSet {{"a|peerAddress", "Address of peer to connect to", a => peerAddress = a}, {"s|securityEnabled", "Present if security is enabled", a => tlsEnabled = a != null}, {"i|id", "Identity of chaincode", a => id = a}, {"o|hostNameOverride", "Hostname override for server certificate", a => hname = a}, {"h|help|?", "Show help", a => help = a != null}};
                options.Parse(args);
                if (help)
                    return;
                if (!string.IsNullOrEmpty(peerAddress))
                {
                    if (host.Contains(":"))
                    {
                        string[] spl = host.Split(':');
                        host = spl[0];
                        port = int.Parse(spl[1]);
                    }
                    else
                    {
                        host = peerAddress;
                        port = DEFAULT_PORT;
                    }
                }

                if (tlsEnabled)
                {
                    logger.Info("TLS enabled");
                    if (!string.IsNullOrEmpty(hname))
                    {
                        logger.Info($"server host override given {hname}");
                        hostOverrideAuthority = hname;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warn("cli parsing failed with exception", e);
            }
        }

        private void ProcessEnvironmentOptions()
        {
            string env = Environment.GetEnvironmentVariable(CORE_CHAINCODE_ID_NAME);
            if (!string.IsNullOrEmpty(env))
                id = env;
            env = Environment.GetEnvironmentVariable(CORE_PEER_ADDRESS);
            if (!string.IsNullOrEmpty(env))
                host = env;
            env = Environment.GetEnvironmentVariable(CORE_PEER_TLS_ENABLED);
            if (!string.IsNullOrEmpty(env))
            {
                tlsEnabled = bool.Parse(env);
                env = Environment.GetEnvironmentVariable(CORE_PEER_TLS_SERVERHOSTOVERRIDE);
                if (!string.IsNullOrEmpty(env))
                    hostOverrideAuthority = env;
                env = Environment.GetEnvironmentVariable(CORE_PEER_TLS_ROOTCERT_FILE);
                if (!string.IsNullOrEmpty(env))
                    rootCertFile = env;
            }
        }

        /**
	 * Start chaincode
	 * 
	 * @param args
	 *            command line arguments
	 */
        public void Start(string[] args)
        {
            ProcessEnvironmentOptions();
            ProcessCommandLineOptions(args);
            if (help)
            {
                StringWriter wr = new StringWriter();
                options.WriteOptionDescriptions(wr);
                logger.Info("Usage chaincode [OPTIONS]");
                logger.Info("Options:");
                logger.Info(wr.ToString());
                return;
            }

            if (id == null)
                logger.Error($"The chaincode id must be specified using either the -i or --i command line options or the {CORE_CHAINCODE_ID_NAME} environment variable.");
            Task.Factory.StartNew(() =>
            {
                logger.Trace("chaincode started");
                Channel connection = NewPeerClientConnection();
                logger.Trace("connection created");
                ChatWithPeer(connection);
                logger.Trace("chatWithPeer DONE");
            });
        }


        public Channel NewPeerClientConnection()
        {
            List<ChannelOption> ops = new List<ChannelOption>();
            logger.Info("Configuring channel connection to peer.");
            ChannelCredentials cred = ChannelCredentials.Insecure;
            if (tlsEnabled)
            {
                logger.Info("TLS is enabled");
                if (!File.Exists(rootCertFile))
                {
                    string msg = $"Root certificate not found at {rootCertFile}";
                    logger.Error(msg);
                    throw new ArgumentException(msg);
                }

                string rootcert = File.ReadAllText(rootCertFile);
                cred = new SslCredentials(rootcert);
                if (!string.IsNullOrEmpty(hostOverrideAuthority))
                {
                    logger.Info("Host override " + hostOverrideAuthority);
                    ops.Add(new ChannelOption("grpc.ssl_target_name_override", hostOverrideAuthority));
                }
            }

            return new Channel(host, port, cred, ops);
        }

        public void ChatWithPeer(Channel connection)
        {
            // Establish stream with validating peer
            ChaincodeSupport.ChaincodeSupportClient stub = new ChaincodeSupport.ChaincodeSupportClient(connection);

            logger.Info("Connecting to peer.");

            AsyncDuplexStreamingCall<ChaincodeMessage, ChaincodeMessage> requestObserver = stub.Register();
            Task.Run(async () =>
            {
                try
                {
                    while (await requestObserver.ResponseStream.MoveNext())
                    {
                        ChaincodeMessage message = requestObserver.ResponseStream.Current;
                        logger.Debug("Got message from peer: " + message.ToJsonString());
                        try
                        {
                            logger.Debug($"[{message.Txid}]Received message {message.Type} from org.hyperledger.fabric.shim");
                            handler.HandleMessage(message);
                        }
                        catch (Exception)
                        {
                            Environment.Exit(-1); //?
                        }
                    }

                    await connection.ShutdownAsync();

                }
                catch (Exception e)
                {
                    logger.Error($"Unable to connect to peer server: {e.Message}");
                    Environment.Exit(-1); //?
                }
            });


            // Create the org.hyperledger.fabric.shim handler responsible for all
            // control logic
            handler = new Handler(requestObserver, this);

            // Send the ChaincodeID during register.
            ChaincodeID chaincodeID = new ChaincodeID {Name = id};
            ChaincodeMessage payload = new ChaincodeMessage {Payload = chaincodeID.ToByteString(), Type = ChaincodeMessage.Types.Type.Register};
            // Register on the stream
            logger.Info($"Registering as '{id}' ... sending {ChaincodeMessage.Types.Type.Register}");
            handler.SerialSend(payload);

            while (true)
            {
                try
                {
                    NextStateInfo nsInfo = handler.nextState.Take();
                    ChaincodeMessage message = nsInfo.Message;
                    handler.HandleMessage(message);
                    // keepalive messages are PONGs to the fabric's PINGs
                    if (nsInfo.SendToCC || message.Type == ChaincodeMessage.Types.Type.Keepalive)
                    {
                        if (message.Type == ChaincodeMessage.Types.Type.Keepalive)
                        {
                            logger.Info("Sending KEEPALIVE response");
                        }
                        else
                        {
                            logger.Info($"[{message.Txid},-8]Send state message {message.Type}");
                        }

                        handler.SerialSend(message);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        protected static Response NewSuccessResponse(string message, byte[] payload)
        {
            return new Response(Status.SUCCESS, message, payload);
        }

        protected static Response NewSuccessResponse()
        {
            return NewSuccessResponse(null, null);
        }

        protected static Response NewSuccessResponse(string message)
        {
            return NewSuccessResponse(message, null);
        }

        protected static Response NewSuccessResponse(byte[] payload)
        {
            return NewSuccessResponse(null, payload);
        }

        protected static Response NewErrorResponse(string message, byte[] payload)
        {
            return new Response(Status.INTERNAL_SERVER_ERROR, message, payload);
        }

        protected static Response NewErrorResponse()
        {
            return NewErrorResponse(null, null);
        }

        protected static Response NewErrorResponse(string message)
        {
            return NewErrorResponse(message, null);
        }

        protected static Response NewErrorResponse(byte[] payload)
        {
            return NewErrorResponse(null, payload);
        }

        protected static Response newErrorResponse(Exception throwable)
        {
            return NewErrorResponse(throwable.Message, throwable.StackTrace.ToBytes());
        }
    }
}