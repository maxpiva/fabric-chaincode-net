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
    public class EventKey : IEquatable<EventKey>
    {
        public EventKey(string evnt, string src)
        {
            FSMEvent = evnt;
            Src = src;
        }

        public string FSMEvent { get; }
        public string Src { get; }

        public bool Equals(EventKey other)
        {
            return Equals((object) other);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            EventKey other = (EventKey) obj;
            if (FSMEvent == null)
            {
                if (other.FSMEvent != null)
                    return false;
            }
            else if (!FSMEvent.Equals(other.FSMEvent))
                return false;

            if (Src == null)
            {
                if (other.Src != null)
                    return false;
            }
            else if (!Src.Equals(other.Src))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + (FSMEvent == null ? 0 : FSMEvent.GetHashCode());
            result = prime * result + (Src == null ? 0 : Src.GetHashCode());
            return result;
        }
    }
}