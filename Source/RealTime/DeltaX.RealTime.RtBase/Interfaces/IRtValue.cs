namespace DeltaX.RealTime.Interfaces
{ 
    public interface IRtValue
    { 
        /// <summary>
        /// Obtiene el valor en formato binario
        /// </summary>
        byte[] Binary { get; }

        /// <summary>
        /// Obtiene el valor en formato string
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Obtiene el valor en formato numérico
        /// </summary>
        double Numeric { get; } 
    }
}
