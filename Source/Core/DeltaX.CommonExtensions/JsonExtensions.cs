namespace DeltaX.CommonExtensions
{
    using System; 
    using System.Linq; 
    using System.Text.Json;

    public static class JsonExtensions
    {

        public static object JsonGetValue(this JsonElement element, string patternParser, Type type, object? defaultValue = null)
        {
            var arg = element.JsonGetValue(patternParser);

            if (arg == null)
            {
                return Convert.ChangeType(defaultValue, type);
            }

            if (arg is JsonElement)
            { 
                var json = JsonSerializer.Serialize(arg);
                return JsonSerializer.Deserialize(json, type);
            }

            return Convert.ChangeType(arg, type);
        }

        public static object JsonGetValue(this JsonElement source, string patternParser)
        {
            var element = source;
            string[] members = patternParser.Split(new char[] { '.', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var member in members)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        var x = element.EnumerateObject().First(o => o.Name == member); 
                        element = x.Value;
                        break;
                    case JsonValueKind.Array:
                        element = element.EnumerateArray().ElementAt(int.Parse(member));
                        break;
                    case JsonValueKind.Undefined:
                        throw new Exception(member);
                }
            }

            object result = null;
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    result = null;
                    break;
                case JsonValueKind.Number:
                    result = element.GetDouble();
                    break;
                case JsonValueKind.False:
                    result = false;
                    break;
                case JsonValueKind.True:
                    result = true;
                    break;
                case JsonValueKind.Undefined:
                    result = null;
                    break;
                case JsonValueKind.String:
                    result = element.GetString();
                    break;
                default:
                    result = element;
                    break;
            }

            return result;
        }
    }
}
