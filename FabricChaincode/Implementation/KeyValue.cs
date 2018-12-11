/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class KeyValue : IKeyValue, IEquatable<KeyValue>
    {
        private readonly ByteString value;
        public KeyValue(KV kv)
        {
            Key = kv.Key;
            value = kv.Value;
        }
        public string Key { get; }
        public byte[] Value => value.ToByteArray();
        public string StringValue => value.ToStringUtf8();

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((Key == null) ? 0 : Key.GetHashCode());
            result = prime * result + ((value == null) ? 0 : value.GetHashCode());
            return result;
        }

        public bool Equals(KeyValue other)
        {
            if (this == other) return true;
            if (other == null) return false;
            if (!Key.Equals(other.Key)) return false;
            if (!value.Equals(other.value)) return false;
            return true;
        }
    }
}