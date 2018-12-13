using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Utils
{


    public static class Extensions
    {
        public static async Task TimeoutAsync(this Task task, TimeSpan timeout, CancellationToken token)
        {
            using (var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var firsttask = await Task.WhenAny(task, Task.Delay(timeout, cancelSource.Token)).ConfigureAwait(false);
                if (firsttask == task)
                {
                    cancelSource.Cancel();
                    await task.ConfigureAwait(false); //Propagate Exceptions
                }
                else
                    throw new TimeoutException("The operation has timed out.");
            }
        }

        public static void ContainsArray<T>(this Assert assert, IEnumerable<IEnumerable<T>> list, IEnumerable<IEnumerable<T>> sublist)
        {
            foreach (IEnumerable<T> data in sublist)
            {
                bool fnd = false;
                foreach (IEnumerable<T> dt in list)
                {
                    if (data.SequenceEqual(dt))
                    {
                        fnd = true;
                        break;
                    }
                }

                if (!fnd)
                    throw new AssertFailedException("Item Missing");
            }
        }
        public static void ContainsDictionary<T,S,W>(this Assert assert, IDictionary<T,S> list, IDictionary<T, S> sublist) where S:IEnumerable<W>
        {
            foreach (T data in sublist.Keys)
            {
                if (!list.ContainsKey(data))
                    throw new AssertFailedException("Item Missing");
                if (!list[data].SequenceEqual(sublist[data]))
                    throw new AssertFailedException("Item Content Different");                
            }
        }
        public static void Contains<T>(this Assert assert, IEnumerable<T> list, IEnumerable<T> sublist)
        {
            foreach (T data in sublist)
            {
                bool fnd = false;
                foreach (T dt in list)
                {
                    if (dt.Equals(data))
                    {
                        fnd = true;
                        break;
                    }
                }

                if (!fnd)
                    throw new AssertFailedException("Item Missing");
            }
        }
        public static byte[] FromHexString(this string data)
        {
            return Regex.Split(data, "(?<=\\G..)(?!$)").Select(x => Convert.ToByte(x, 16)).ToArray();
        }

    }

}

