namespace DeltaX.RealTime.Decorators
{
    using DeltaX.RealTime.Interfaces;
    using System; 

    public class RtTagType<TValue> : RtTagDecoratorBase, IRtTag
    {
        private TValue parsedValue;
        private DateTime prevUpdate; 

        public RtTagType(IRtTag tag) : base(tag)
        { }

        private bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsBooleanType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsStringType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return true;
                default:
                    return false;
            }
        }

        private TValue GetParseValue()
        {
            var type = typeof(TValue);
            // String
            if (IsStringType(type))
            {
                return (TValue)(object)tag.Value.Text;
            }
            // Byte Array
            if (type.Name == "Byte[]")
            {
                return (TValue)(object)tag.Value.Binary;
            }
            // Numeric 
            if (IsNumericType(type))
            {
                return double.IsNaN(tag.Value.Numeric) ? default :  (TValue)Convert.ChangeType(tag.Value.Numeric, type);
            }
            // Bool
            if (IsBooleanType(type))
            {
                bool resBool = (bool.TryParse(tag.Value.Text, out resBool) && resBool) || (tag.Value.Numeric == 0 ? false : true);
                return (TValue)(object)resBool;
            }
            // DateTimeOffset
            if (type == typeof(DateTimeOffset))
            {
                return (TValue)(object)tag.GetDateTime();
            }
            // JSON
            return tag.GetJson<TValue>();
        }

        public IRtTag Tag => tag;

        public new TValue Value
        {
            get
            {
                if (!Status)
                {
                    return default;
                }
                if (prevUpdate != Updated)
                {
                    parsedValue = GetParseValue();
                    prevUpdate = Updated;
                }
                return parsedValue;
            }
        }

        public bool Set(TValue value)
        {
            var type = typeof(TValue);

            // String
            if (value is string valStr)
            {
                return tag.SetText(valStr);
            }
            // Byte Array
            if (value is byte[] valBA)
            {
                return tag.SetBinary(valBA);
            }
            // DateTimeOffset
            if (value is DateTimeOffset valDtO)
            {
                return tag.SetDateTime(valDtO);
            }
            // Numeric or Bool
            if (IsNumericType(type) || IsBooleanType(type))
            {
                return tag.SetNumeric(Convert.ToDouble(value));
            }
            // JSON
            return tag.SetJson<TValue>(value);
        }
    }

}
