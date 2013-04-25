using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace JsonXml.Test
{
	[TestClass]
	public class TextJsonReaderTest
	{
		[TestMethod]
		public void ReadTest()
		{
			const string expected = "{\"foo\":true,\"baz\":null,\"quux\":[1,2,3.34,4,5,{\"foo\":\"bar\"}],\"foobar\":{\"foo\":\"bar\"},\"whibble\":false}";
			JsonReader jsonReader = new JsonTextReader(new StringReader(expected));
			var reader = new TextJsonReader(jsonReader);
			Assert.AreEqual(expected, reader.ReadToEnd());
		}
	}
}
