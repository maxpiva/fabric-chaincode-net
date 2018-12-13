/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using Hyperledger.Fabric.Shim.Helper;

namespace Hyperledger.Fabric.Shim
{

    /**
     * Wrapper around protobuf Response, contains status, message and payload. Object returned by
     * call to {@link #init(ChaincodeStub)} and{@link #invoke(ChaincodeStub)}
     */
    public class Response
    {
        public Response(Status status, string message, byte[] payload)
        {
            Status = status;
            Message = message;
            Payload = payload;
            
        }

        public Status Status { get; }
        public string Message { get; }
        public byte[] Payload { get; }
        public string StringPayload => Payload.ToUTF8String();
    }
}