/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;

namespace Hyperledger.Fabric.Shim.Ledger
{
    public interface IKeyModification
    {

        /**
         * Returns the transaction id.
         *
         * @return tx id of modification
         */
        string TxId { get; }

        /**
         * Returns the key's value at the time returned by {@link #getTimestamp()}.
         *
         * @return value
         */
        byte[] Value { get; }

        /**
         * Returns the key's value at the time returned by {@link #getTimestamp()},
         * decoded as a UTF-8 string.
         *
         * @return value as string
         */
        string StringValue { get; }

        /**
         * Returns the timestamp of the key modification entry.
         *
         * @return timestamp
         */
        DateTime? Timestamp { get; }

        /**
         * Returns the deletion marker.
         *
         * @return is key was deleted
         */
        bool IsDeleted { get; }

    }
}