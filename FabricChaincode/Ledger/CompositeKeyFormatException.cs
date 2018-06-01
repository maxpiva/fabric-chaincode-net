/*
Copyright IBM 2017 All Rights Reserved.

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
    }
}
