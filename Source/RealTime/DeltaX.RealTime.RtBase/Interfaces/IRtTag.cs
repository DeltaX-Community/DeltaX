namespace DeltaX.RealTime.Interfaces
{
    using System;


    /// <summary>
    /// Interfaz usada para la cración de RtTags con determinada funcionalidad independiente del conector
    /// </summary>
    public interface IRtTag: IDisposable
    {

        /// <summary>
        /// Evento a ejecutarse cuando se actualiza el tag
        /// </summary>
        event EventHandler<IRtTag> ValueUpdated;

        /// <summary>
        /// Evento a ejecutarse cuando al escribir el tag
        /// </summary>
        event EventHandler<IRtTag> ValueSetted;


        IRtConnector Connector { get; }
         

        /// <summary>
        /// Nombre del tag (Alias)
        /// </summary>
        string TagName { get; }

         
        /// <summary>
        /// The topic used for subscribe and receive value
        /// </summary>
        string Topic { get; }
         

        /// <summary>
        /// Setea a value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Set(IRtValue value);
          

        /// <summary>
        /// Get a value
        /// </summary>
        IRtValue Value { get; }

        /// <summary>
        /// Fecha de ultima actualizacion del tag
        /// </summary>
        DateTime Updated { get; }

        /// <summary>
        /// Estado del tag: false representa una falla de sincronismo con el servidor
        /// </summary>
        bool Status { get; }
    }
}