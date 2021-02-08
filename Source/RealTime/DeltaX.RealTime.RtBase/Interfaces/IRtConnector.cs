namespace DeltaX.RealTime.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRtConnector : IDisposable
    { 
        public Task ConnectAsync(CancellationToken? cancellationToken =null);

        public bool Disconnect();

        /// <summary>
        /// Create a tag
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="topic"></param> 
        /// <param name="options"></param> 
        /// <returns></returns>
        IRtTag AddTag(string tagName, string topic, IRtTagOptions options = null);

        /// <summary>
        /// Get reusable tag
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        IRtTag GetTag(string tagName);

        /// <summary>
        /// Indica si esta conectado con el servidor
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Permite escribir un valor al servidor
        /// </summary>
        /// <param name="tagRaw"></param>
        /// <param name="data"></param> 
        /// <returns></returns>
        bool WriteValue(IRtTag tag, IRtValue value );

        bool WriteValue(string topic, IRtValue value, IRtTagOptions options = null); 

        /// <summary>
        /// Evento que se ejecuta al recibir una actualización del tag
        /// </summary>
        event EventHandler<IRtTag> ValueUpdated;

        /// <summary>
        /// Evento que se ejecuta al publicar un nuevo valor
        /// </summary>
        event EventHandler<IRtTag> ValueSetted;
          
        /// <summary>
        /// Evento al recibir un mensaje
        /// </summary>
        public event EventHandler<IRtMessage> MessageReceived;

        /// <summary>
        /// Evento Al conectar
        /// </summary>
        public event EventHandler<bool> Connected;

        /// <summary>
        /// Evento Al desconectar
        /// </summary>
        public event EventHandler<bool> Disconnected;
    }
}
