using DeltaX.Utilities.FiniteStateMachine;
using NUnit.Framework;
using System;

namespace DeltaX.Modules.FiniteStateMachine.UnitTest
{
    public class Tests
    {
        enum State
        {
            Stoped,
            Starting,
            Running        
        }

        enum Event
        {
            Start,
            Run,
            Stop
        }

        [Test]
        public void Test1()
        {
            var fsm = new FiniteStateMachine<State, Event>(State.Stoped);
            State? eventState = null; 

            Func<State, Event, ValidateTransitionResult> callback = (s, e) =>
            {
                return ValidateTransitionResult.FinishAccept;
            };

            // fsm.AddTransition(State.Disconnected, Event.Connect, callbackTrue, State.Connecting);
            fsm.AddTransition(State.Stoped, Event.Start, callback, State.Starting);
            fsm.AddTransition(State.Starting, Event.Run, callback, State.Running);
            fsm.AddTransition(State.Starting, Event.Stop, callback, State.Stoped);
            fsm.AddTransition(State.Running, Event.Stop, callback, State.Stoped);
             
            fsm.OnChangeState += (s, state)=> {
                eventState = state;
            }; 

            Assert.Null(eventState);
            Assert.AreEqual(State.Stoped, fsm.State);

            fsm.ProcessEvent(Event.Start);
            Assert.AreEqual(State.Starting, fsm.State);
            Assert.AreEqual(State.Starting, eventState); 

            fsm.ProcessEvent(Event.Stop);
            Assert.AreEqual(State.Stoped, fsm.State);
            Assert.AreEqual(State.Stoped, eventState);

            fsm.ProcessEvent(Event.Run);
            Assert.AreEqual(State.Stoped, fsm.State);
            Assert.AreEqual(State.Stoped, eventState);

            fsm.ProcessEvent(Event.Start);
            Assert.AreEqual(State.Starting, fsm.State);
            Assert.AreEqual(State.Starting, eventState);

            fsm.ProcessEvent(Event.Run);
            Assert.AreEqual(State.Running, fsm.State);
            Assert.AreEqual(State.Running, eventState);

            fsm.ProcessEvent(Event.Start);
            Assert.AreEqual(State.Running, fsm.State);
            Assert.AreEqual(State.Running, eventState); 

            fsm.ProcessEvent(Event.Stop);
            Assert.AreEqual(State.Stoped, fsm.State);
            Assert.AreEqual(State.Stoped, eventState);
        }
    }
}