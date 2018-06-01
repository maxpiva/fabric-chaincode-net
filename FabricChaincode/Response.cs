using Hyperledger.Fabric.Shim.Helper;

namespace Hyperledger.Fabric.Shim
{
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