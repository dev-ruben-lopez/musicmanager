public static class OperatorHelper
{
    public static bool Compare<T>(T left, T right, string op) where T : IComparable
    {
        return op switch
        {
            "==" or "=" => EqualityComparer<T>.Default.Equals(left, right),
            "!=" => !EqualityComparer<T>.Default.Equals(left, right),
            ">"  => left.CompareTo(right) > 0,
            ">=" => left.CompareTo(right) >= 0,
            "<"  => left.CompareTo(right) < 0,
            "<=" => left.CompareTo(right) <= 0,
            _ => throw new ArgumentException($"Invalid comparison operator '{op}'"),
        };
    }
}

// Use like this
bool result1 = OperatorHelper.Compare(10, 20, "<");   // true
bool result2 = OperatorHelper.Compare("apple", "banana", "!="); // true
bool result3 = OperatorHelper.Compare(5.0, 5.0, "=="); // true


using System.Text.Json.Nodes;

public static class JsonNodeCaster
{
    public static void ExtractValue(JsonNode node, out string? strValue, out double? numValue)
    {
        strValue = null;
        numValue = null;

        if (node is JsonValue valueNode)
        {
            var val = valueNode.GetValue<object>();

            switch (val)
            {
                case string s:
                    strValue = s;
                    break;

                case int i:
                case long l:
                case float f:
                case double d:
                case decimal m:
                    numValue = Convert.ToDouble(val);
                    break;

                default:
                    // Handle other types if needed
                    break;
            }
        }
    }
}


