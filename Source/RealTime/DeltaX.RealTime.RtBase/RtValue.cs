namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces;
    using System.Text;

    public class RtValue : IRtValue
    {
        public static IRtValue Create(byte[] value)
        {
            return new RtValue(value);
        }

        public static IRtValue Create(string value)
        {
            return new RtValue(value);
        }

        public static IRtValue Create(double value)
        {
            return new RtValue(value);
        }

        public byte[] Binary { get; private set; }

        public string Text { get; private set; }

        public double Numeric { get; private set; }


        private RtValue(byte[] value)
        {
            Binary = value ?? new byte[] { };
            Text = Encoding.ASCII.GetString(Binary);

            double number;
            Numeric = double.TryParse(Text, out number) ? number : double.NaN;
        }

        private RtValue(string value)
        {
            Text = value ?? string.Empty;
            Binary = Encoding.ASCII.GetBytes(Text);

            double number;
            Numeric = double.TryParse(Text, out number) ? number : double.NaN;
        }

        private RtValue(double value)
        {
            Numeric = value;
            Text = double.IsNaN(value) ? string.Empty : Numeric.ToString();
            Binary = Encoding.ASCII.GetBytes(Text);
        }
    }
}
