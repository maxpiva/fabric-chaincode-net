/*
Copyright DTCC 2016 All Rights Reserved.

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

namespace Hyperledger.Fabric.Shim.Fsm
{

    public class CallbackKey : IEquatable<CallbackKey>
    {
        public string Target { get; set; }
        public CallbackType? Type { get; set; }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType()!=obj.GetType())
                return false;
            CallbackKey other = (CallbackKey)obj;
            if (Target == null)
            {
                if (other.Target != null)
                    return false;
            }
            else if (!Target.Equals(other.Target))
                return false;
            if (Type != other.Type)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((Target == null) ? 0 : Target.GetHashCode());
            result = prime * result + ((Type == null) ? 0 : Type.GetHashCode());
            return result;
        }

        public CallbackKey(string target, CallbackType type)
        {
            Target = target;
            Type = type;
        }

        public bool Equals(CallbackKey other)
        {
            return Equals((object) other);
        }
    }
}