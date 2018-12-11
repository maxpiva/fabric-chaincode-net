/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;

namespace Hyperledger.Fabric.Shim.Ledger
{
    public class CompositeKeyFormatException : ArgumentException
    {
        public CompositeKeyFormatException(string message, Exception parent) : base(message, parent)
        {

        }

        public CompositeKeyFormatException(string message) : base(message)
        {

        }

        public CompositeKeyFormatException(Exception exception) : base(exception.Message, exception)
        {

        }

        public static CompositeKeyFormatException ForInputString(string s, string group, int index)
        {
            return new CompositeKeyFormatException($"For input string '{s}', found 'U+{((int)group[0]),6:X}' at index {index}.");
        }
        public static CompositeKeyFormatException ForSimpleKey(string key)
        {
            return new CompositeKeyFormatException($"First character of the key [{key}] contains a 'U+{(int)CompositeKey.NAMESPACE[0],6:X}' which is not allowed");
        }
    }
}
