namespace DeltaX.CommonExtensions
{
    using System;
    using System.Text.RegularExpressions;

    public static class UltraLightExtensions
    {
        public static bool TryUltraLightParse(this string format, out string device, out string command, out string field)
        {
            Regex r = new Regex(@"((?<device>\w+)\@)?((?<command>\w+)\|)?(?<field>[\w]+)",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);


            var res = r.Match(format);
            field = res.Groups["field"].Value;
            command = res.Groups["command"].Value;
            device = res.Groups["device"].Value;
            return res.Success;
        }

        public static string UltraLightGetValue(this string source, string field, string command = null, string device = null)
        {

            if (!string.IsNullOrEmpty(command))
            {
                var arr = source.Split(new[] { '|', '@', '=' });
                var idx = Array.IndexOf(arr, field);

                if (arr.Length > 1 && (string.IsNullOrEmpty(device) || arr[0] == device))
                {
                    if (idx > 0 && arr.Length > idx + 1 && arr[1] == command)
                    {
                        return arr[idx + 1];
                    }
                    if (field == "value" && arr.Length == 3 && arr[1] == command)
                    {
                        return arr[2];
                    }
                }
                return null;
            }
            else
            {
                var arr = source.Split("|");
                var idx = Array.IndexOf(arr, field);
                if (idx >= 0 && arr.Length - idx > 0 && (arr.Length - idx) % 2 == 0)
                {
                    return arr[idx + 1];
                }
                if (field.ToUpper() == "DATETIME" && arr.Length > 2 && arr.Length % 2 == 1)
                {
                    return arr[0];
                }
                return null;
            }
        }
    }
}
