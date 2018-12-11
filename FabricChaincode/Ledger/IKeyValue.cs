/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/
namespace Hyperledger.Fabric.Shim.Ledger
{
    /**
     * Query Result associating a state key with a value.
     *
     */
    public interface IKeyValue
    {

        /**
         * Returns the state key.
         *
         * @return key as string
         */
        string Key { get; }

        /**
         * Returns the state value.
         *
         * @return value as byte array
         */
        byte[] Value { get; }

        /**
         * Returns the state value, decoded as a UTF-8 string.
         *
         * @return value as string
         */
        string StringValue { get; }

    }
}