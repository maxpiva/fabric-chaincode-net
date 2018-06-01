using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Newtonsoft.Json;

namespace Hyperledger.Fabric.Shim.Helper
{
    public static class Utils
    {
        public static TValue GetOrNull<TKey, TValue>(this Dictionary<TKey, TValue> tr, TKey key)
        {
            if (tr.ContainsKey(key))
                return tr[key];
            return default(TValue);
        }
        public static string ToUTF8String(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
        public static byte[] ToBytes(this string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        public static string ToJsonString(this ChaincodeMessage message)
        {
            try
            {
                return JsonConvert.SerializeObject(message, Formatting.None);
            }
            catch (InvalidProtocolBufferException)
            {
                return $"{{ Type: {message.Type}, TxId: {message.Txid} }}";
            }
        }
        public static T RunAndUnwarp<T>(this Task<T> func)
        {
            try
            {
                return func.GetAwaiter().GetResult();
            }
            catch (AggregateException e)
            {
                throw e.Flatten().InnerExceptions.First();
            }
        }
        public static void RunAndUnwarp(this Task func)
        {
            try
            {
                func.GetAwaiter().GetResult();
            }
            catch (AggregateException e)
            {
                throw e.Flatten().InnerExceptions.First();
            }
        }
    }
}
