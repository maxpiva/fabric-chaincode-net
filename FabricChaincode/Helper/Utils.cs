using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public static T RunAndUnwrap<T>(this Task<T> func)
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
        public static void RunAndUnwrap(this Task func)
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
        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            IEnumerable<T> enumerable = source.ToList();
            int max = enumerable
                          .SelectMany(i => Regex.Matches(selector(i), @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length))
                          .Max() ?? 0;

            return enumerable.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }

    }
}
