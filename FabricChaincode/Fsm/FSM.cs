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
using System.Collections.Generic;
using Hyperledger.Fabric.Shim.Fsm.Exceptions;
using Hyperledger.Fabric.Shim.Helper;

namespace Hyperledger.Fabric.Shim.Fsm
{
    public class FSM
    {
        private readonly HashSet<string> allEvents;
        private readonly HashSet<string> allStates;

        /** Maps events and triggers to callback functions */
        private readonly Dictionary<CallbackKey, Action<FSMEvent>> callbacks;

        /** Calls the FSM's transition function */
        private readonly Transitioner transitioner;


        /** Maps events and sources states to destination states */
        private readonly Dictionary<EventKey, string> transitions;


        // NewFSM constructs a FSM from events and callbacks.
        //
        // The events and transitions are specified as a slice of Event structs
        // specified as Events. Each Event is mapped to one or more internal
        // transitions from Event.Src to Event.Dst.
        //
        // Callbacks are added as a map specified as Callbacks where the key is parsed
        // as the callback event as follows, and called in the same order:
        //
        // 1. before_<EVENT> - called before event named <EVENT>
        //
        // 2. before_event - called before all events
        //
        // 3. leave_<OLD_STATE> - called before leaving <OLD_STATE>
        //
        // 4. leave_state - called before leaving all states
        //
        // 5. enter_<NEW_STATE> - called after eftering <NEW_STATE>
        //
        // 6. enter_state - called after entering all states
        //
        // 7. after_<EVENT> - called after event named <EVENT>
        //
        // 8. after_event - called after all events
        //
        // There are also two short form versions for the most commonly used callbacks.
        // They are simply the name of the event or state:
        //
        // 1. <NEW_STATE> - called after entering <NEW_STATE>
        //
        // 2. <EVENT> - called after event named <EVENT>
        //

        public FSM(string initialState)
        {
            Current = initialState;
            transitioner = new Transitioner();

            transitions = new Dictionary<EventKey, string>();
            callbacks = new Dictionary<CallbackKey, Action<FSMEvent>>();
            allEvents = new HashSet<string>();
            allStates = new HashSet<string>();
        }

        /** The internal transaction function used either directly
         * or when transition is called in an asynchronous state transition. */
        public Action Transition { get; internal set; }

        /** Returns the current state of the FSM */
        /** The current state of the FSM */
        public string Current { get; private set; }


        /** Returns whether or not the given state is the current state */
        public bool IsCurrentState(string state)
        {
            return state.Equals(Current);
        }

        /** Returns whether or not the given event can occur in the current state */
        public bool EventCanOccur(string eventName)
        {
            return transitions.ContainsKey(new EventKey(eventName, Current));
        }

        /** Returns whether or not the given event can occur in the current state */
        public bool EventCannotOccur(string eventName)
        {
            return !EventCanOccur(eventName);
        }

        /** Initiates a state transition with the named event.
         * The call takes a variable number of arguments
         * that will be passed to the callback, if defined.
         * 
         * It  if the state change is ok or one of these errors:
         *  - event X inappropriate because previous transition did not complete
         *  - event X inappropriate in current state Y
         *  - event X does not exist
         *  - internal error on state transition
         * @throws InTrasistionException 
         * @throws InvalidEventException 
         * @throws UnknownEventException 
         * @throws NoTransitionException 
         * @throws AsyncException 
         * @throws CancelledException 
         * @throws NotInTransitionException 
         */
        public void RaiseEvent(string eventName, params object[] args)
        {
            if (Transition != null)
                throw new InTrasistionException(eventName);
            string dst = transitions.GetOrNull(new EventKey(eventName, Current));
            if (dst == null)
            {
                foreach (EventKey key in transitions.Keys)
                {
                    if (key.FSMEvent.Equals(eventName))
                        throw new InvalidEventException(eventName, Current);
                }

                throw new UnknownEventException(eventName);
            }

            FSMEvent evnt = new FSMEvent(this, eventName, Current, dst, null, false, false, args);
            CallCallbacks(evnt, CallbackType.BEFORE_EVENT);

            if (Current.Equals(dst))
            {
                CallCallbacks(evnt, CallbackType.AFTER_EVENT);
                throw new NoTransitionException(evnt.Error);
            }

            // Setup the transition, call it later.
            Transition = () =>
            {
                Current = dst;
                CallCallbacks(evnt, CallbackType.ENTER_STATE);
                CallCallbacks(evnt, CallbackType.AFTER_EVENT);
            };

            CallCallbacks(evnt, CallbackType.LEAVE_STATE);

            // Perform the rest of the transition, if not asynchronous.
            DoTransition();
        }

        // Transition wraps transitioner.transition.
        public void DoTransition()
        {
            transitioner.Transition(this);
        }


        /** Calls the callbacks of type 'type'; first the named then the general version. 
         * @throws CancelledException 
         * @throws AsyncException */
        public void CallCallbacks(FSMEvent evnt, CallbackType type)
        {
            string trigger = evnt.Name;
            if (type == CallbackType.LEAVE_STATE) trigger = evnt.Src;
            else if (type == CallbackType.ENTER_STATE) trigger = evnt.Dst;

            Action<FSMEvent>[] cbks = {
                callbacks.GetOrNull(new CallbackKey(trigger, type)), //Primary
                callbacks.GetOrNull(new CallbackKey("", type)) //General
            };

            foreach (Action<FSMEvent> callback in cbks)
            {
                if (callback != null)
                {
                    callback(evnt);
                    if (type == CallbackType.LEAVE_STATE)
                    {
                        if (evnt.Cancelled)
                        {
                            Transition = null;
                            throw new CancelledException(evnt.Error);
                        }

                        if (evnt.Async)
                        {
                            throw new AsyncException(evnt.Error);
                        }
                    }
                    else if (type == CallbackType.BEFORE_EVENT)
                    {
                        if (evnt.Cancelled)
                        {
                            throw new CancelledException(evnt.Error);
                        }
                    }
                }
            }
        }

        public void AddEvents(params EventDesc[] events)
        {
            // Build transition map and store sets of all events and states.
            foreach (EventDesc evnt in events)
            {
                foreach (string src in evnt.Src)
                {
                    transitions.Add(new EventKey(evnt.Name, src), evnt.Dst);
                    allStates.Add(src);
                }

                allStates.Add(evnt.Dst);
                allEvents.Add(evnt.Name);
            }
        }


        public void AddCallbacks(params CBDesc[] descs)
        {
            foreach (CBDesc desc in descs)
            {
                callbacks.Add(new CallbackKey(desc.Trigger, desc.Type), desc.Callback);
            }
        }
    }
}