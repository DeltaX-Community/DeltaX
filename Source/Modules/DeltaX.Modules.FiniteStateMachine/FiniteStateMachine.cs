namespace DeltaX.Utilities.FiniteStateMachine
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Maquina de estado finita, facilita el uso de maquina de estados y sus transiciones
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    public class FiniteStateMachine<TState, TEvent> 
        where TState : Enum where TEvent : Enum
    {
        public struct Transition
        {
            public TState State;
            public TEvent Event;
            public Func<TState, TEvent, ValidateTransitionResult> Function;
            public TState NextState;
        } 

        readonly List<Transition> transitionFunctions = new List<Transition>();
         
        protected ILogger logger;
        private TState state; 

        /// <summary>
        /// Estado actual de la FSM
        /// </summary>
        public TState State
        {
            get
            {
                return state;
            }
            set
            {
                if (!state.Equals(value))
                {
                    logger?.LogDebug("Change State {0} >> {1} ", state, value);
                    state = value;

                    OnChangeState?.Invoke(this, state);
                }
            }
        }


        /// <summary>
        /// FSM, instancia con estado inicial
        /// </summary>
        /// <param name="initialState"></param>
        public FiniteStateMachine(TState initialState = default, ILogger logger = null)
        {
            State = initialState;
            this.logger = logger; 
        }

        /// <summary>
        /// Callback que se ejecuta cuando se actualiza el estado
        /// </summary>
        public event EventHandler<TState> OnChangeState;

        /// <summary>
        /// Metodo para configurar las transiciones
        /// </summary>
        /// <param name="state">Estado en el cual se encuentra la maquina para procesar el evento</param>
        /// <param name="event">Evento de llegada</param>
        /// <param name="validateTransition">Funcion a ejecutarse para validar o accionar sobre el estado/evento</param>
        /// <param name="nextState">Siguiente estado si la fucion retorna true</param>
        public void AddTransition(TState state, TEvent @event, Func<TState, TEvent, ValidateTransitionResult> validateTransition, TState nextState)
        {
            transitionFunctions.Add(new Transition { State = state, Event = @event, Function = validateTransition, NextState = nextState });
        }

        /// <summary>
        /// Funcion que se debe ejecutar para avanzar por los estados
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public bool ProcessEvent(TEvent @event)
        {
            var transitions = transitionFunctions
                .FindAll(fun => fun.State.Equals(State) && fun.Event.Equals(@event));

            if (transitions == null || transitions.Count == 0)
            {
                logger?.LogInformation("Not transition available State:{0} Event:{1}", State, @event);
                return false;
            }

            try
            {
                var _state = State;
                var _prev_state = _state;

                foreach (var transition in transitions)
                {
                    logger?.LogDebug("Eval Function:'{2} (State:{0} Event:{1})'", _state, @event, transition.Function.Method.Name);

                    var result = transition.Function.Invoke(_state, @event);

                    logger?.LogDebug("Transition Function:'{2} (State:{0} Event:{1})' Result:{3}",
                        _state, @event, transition.Function.Method.Name, result);

                    if (result == ValidateTransitionResult.ContinueAccept)
                    {
                        _state = transition.NextState;
                        continue;
                    }
                    else if (result == ValidateTransitionResult.ContinueSkip)
                    {
                        // Dont change status
                        continue;
                    }
                    else if (result == ValidateTransitionResult.FinishAccept)
                    {
                        _state = transition.NextState;
                        break;
                    }
                    else if (result == ValidateTransitionResult.FinishSkip)
                    {
                        // Dont change status
                        break;
                    }
                    else if (result == ValidateTransitionResult.FinishRollback)
                    {
                        _state = _prev_state;
                        break;
                    }
                }

                if (!State.Equals(_state))
                {
                    State = _state;
                    return true;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "ProcessEvent Invoke");
                throw;
            }
            return false;
        }
    }
}
