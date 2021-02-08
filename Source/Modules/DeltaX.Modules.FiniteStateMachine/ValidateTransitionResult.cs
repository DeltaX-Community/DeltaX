namespace DeltaX.Utilities.FiniteStateMachine
{
    public enum ValidateTransitionResult
    {
        /// <summary>
        /// Continua con la siguiente transicion aceptando el cambio de estado
        /// </summary>
        ContinueAccept = 1,

        /// <summary>
        /// Continua con la siguiente transicion sin cambiar el estado actual
        /// </summary>
        ContinueSkip,

        /// <summary>
        /// Finaliza la transicion para el State, Event actual aceptando el cambio de estado
        /// </summary>
        FinishAccept,

        /// <summary>
        /// Finaliza la transicion para el State, Event actual rechazando el cambio de estado 
        /// de la transicion actual pero manteniendo los cambios previos
        /// </summary>
        FinishSkip,

        /// <summary>
        /// Finaliza la transicion para el Event Actual rechazando todos los cambios previos
        /// </summary>
        FinishRollback
    }
}
