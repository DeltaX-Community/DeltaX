namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces;
    using DeltaX.RealTime;
    using System;
    using DeltaX.RealTime.Decorators;
    using System.Text.Json;


    /// <summary>
    /// Ayudas utilitarias
    /// </summary>
    public static class RtTagExtensions
    {

        // FIXME move to base config
        public static string DefaultDateTimeFormat = "o";// "yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>
        /// Obtiene DateTime del tag
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="defaultDate"></param> 
        /// <param name="dateFormat"></param>
        /// <returns></returns>
        public static DateTime GetDateTime(this IRtTag tag, DateTime? defaultDate = null, string dateFormat = null)
        {
            try
            {
                return DateTime.ParseExact(tag.Value.Text, dateFormat ?? DefaultDateTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                DateTime result;
                if (string.IsNullOrEmpty(dateFormat) && DateTime.TryParse(tag.Value.Text, out result))
                {
                    return result;
                }

                return defaultDate ?? throw e;
            }
        }

        /// <summary>
        /// Setea un DateTime al tag
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="date"></param>
        /// <param name="dateFormat"></param>
        /// <returns></returns>
        public static bool SetDateTime(this IRtTag tag, DateTime date, string dateFormat = null)
        {
            return tag.SetText(date.ToString(dateFormat ?? DefaultDateTimeFormat));
        }
         
         /// <summary>
         /// Obtiene el Json Objeto del tag
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="tag"></param>
         /// <returns></returns>
         public static T GetJson<T>(this IRtTag tag)
         {
             return JsonSerializer.Deserialize<T>(tag.Value.Text);
         }
         
         /// <summary>
         /// Setea el objecto Json al tag
         /// </summary>
         /// <param name="tag"></param>
         /// <param name="json"></param>
         /// <returns></returns>
         public static bool SetJson<T>(this IRtTag tag, T json)
         {
             return tag.SetText(JsonSerializer.Serialize(json));
         }
       

        public static bool SetNumeric(this IRtTag tag, double value)
         {
             return tag.Set(RtValue.Create(value));
         }
         
         public static bool SetText(this IRtTag tag, string value)
         {
             return tag.Set(RtValue.Create(value));
         }
         
         public static bool SetBinary(this IRtTag tag, byte[] value)
         {
             return tag.Set(RtValue.Create(value));
         }          
    }

}
