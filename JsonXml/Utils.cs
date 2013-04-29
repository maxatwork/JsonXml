using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace JsonXml
{
	public class Utils
	{
		private static readonly NameValueCollection PluralExceptions = new NameValueCollection
		                                                 	{
																{ "roses", "rose" },
																{ "children", "child" },
																{ "dates", "date" },
																{ "phases", "phase" }
		                                                 	};

		public static string Pluralize(string s)
		{
			if (s.ToLower().EndsWith("y"))
				return string.Format("{0}ies", s.Substring(0, s.Length - 1));

			if (s.ToLower().EndsWith("s") || s.ToLower().EndsWith("x") || s.ToLower().EndsWith("sh") || s.ToLower().EndsWith("ch"))
				return string.Format("{0}es", s);
			
			return string.Format("{0}s", s);
		}

		public static string Singularize(string s)
		{
			if (PluralExceptions[s.ToLower()] != null) return s.Substring(0, PluralExceptions[s.ToLower()].Length);
			if (s.EndsWith("ies")) return string.Format("{0}y", s.Substring(0, s.Length - 3));
			if (s.EndsWith("ses")) return s.Substring(0, s.Length - 2);
			if (s.EndsWith("xes")) return s.Substring(0, s.Length - 2);
			if (s.EndsWith("shes")) return s.Substring(0, s.Length - 2);
			if (s.EndsWith("ches")) return s.Substring(0, s.Length - 2);
			if (s.EndsWith("s")) return s.Substring(0, s.Length - 1);
			return s;
		}

		public static string UnparseValue(object value)
		{
			var type = value.GetType();
			if (type == typeof(bool)) return ((bool)value) ? "true" : "false";
			if (type == typeof(double)) return ((double)value).ToString(CultureInfo.InvariantCulture.NumberFormat);
            return value.ToString();
		}

		public static object ParseValue(string value)
		{
			if (value == "true") return true;
			if (value == "false") return false;

			var intRegex = new Regex("^[1-9][0-9]*$");
			if (intRegex.IsMatch(value))
			{
				int result;
				return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out result)
				       	? (object) result
				       	: value;
			}

			var decimalRegex = new Regex("^[1-9][0-9]*(\\.[0-9]*)?$");
			if (decimalRegex.IsMatch(value))
			{
				double result;
				return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out result)
				       	? (object) result
				       	: value;
			}

			return Escape(value);
		}

		public static JsonToken GetValueType(string value)
		{
			if (value == "true" || value == "false") return JsonToken.Boolean;

			var intRegex = new Regex("^[1-9][0-9]*$");
			if (intRegex.IsMatch(value)) return JsonToken.Integer;

			var decimalRegex = new Regex("^[1-9][0-9]*(\\.[0-9]*)?$");
			if (decimalRegex.IsMatch(value)) return JsonToken.Float;

			return JsonToken.String;
		}

        public static string Escape(string s)
        {
            return s.Replace("\\", "\\\\");
        }
	}
}