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
    /** Holds the info that get passed as a reference in the callbacks */
    public class FSMEvent
    {
        public FSMEvent(FSM fsm, string name, string src, string dst, Exception error, bool cancelled, bool async, params object[] args)
        {
            FSM = fsm;
            Name = name;
            Src = src;
            Dst = dst;
            Error = error;
            Cancelled = cancelled;
            Async = async;
            Args = args;
        }

        // A reference to the parent FSM.
        public FSM FSM { get; }

        // The event name.
        public string Name { get; }

        // The state before the transition.
        public string Src { get; }

        // The state after the transition.
        public string Dst { get; }

        // An optional error that can be returned from a callback.
        public Exception Error { get; private set; }

        // An internal flag set if the transition is canceled.
        public bool Cancelled { get; private set; }

        // An internal flag set if the transition should be asynchronous
        /**
         * Can be called in leave_<STATE> to do an asynchronous state transition.
         * The current state transition will be on hold in the old state until a final
         * call to Transition is made. This will complete the transition and possibly
         * call the other callbacks.
         */
        public bool Async { get; set; }

        // An optional list of arguments passed to the callback.
        public object[] Args { get; }

        /**
         * Can be called in before_<EVENT> or leave_<STATE> to cancel the 
         * current transition before it happens. It takes an optional error,
         * which will overwrite the event's error if it had already been set.
         */
        public Exception Cancel(Exception err)
        {
            Cancelled = true;
            if (err != null)
            {
                Error = err;
            }

            return err;
        }
    }
}