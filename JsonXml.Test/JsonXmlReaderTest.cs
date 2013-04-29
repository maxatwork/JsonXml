using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace JsonXml.Test
{
	[TestClass]
	public class JsonXmlReaderTest
	{
		[TestMethod]
		public void NullReadingTest()
		{
			TestJsonXmlReader("{\"foo\":null}", "<foo />");
		}
		
		[TestMethod]
		public void SimpleJsonReadingTest()
		{
			TestJsonXmlReader("{\"foo\":\"bar\"}", "<foo>bar</foo>");
		}

		[TestMethod]
		public void AttributeReadingTest()
		{
			TestJsonXmlReader("{\"foo\":{\"@bar\":\"baz\",\"@qux\":\"quux\"}}", "<foo bar=\"baz\" qux=\"quux\"></foo>");
		}

        [TestMethod]
        public void EscapeAttributeReadingTest()
        {
            TestJsonXmlReader("{\"foo\":{\"@bar\":\"\\\\\\\\<>&\\\"'/\"}}", "<foo bar=\"\\\\&lt;&gt;&amp;&quot;'/\"></foo>");
        }

		[TestMethod]
		public void NestedElementsReadingTest()
		{
			TestJsonXmlReader(
				"{\"foo\":{\"bar\":{\"qux\":\"quux\"},\"baz\":{\"whibble\":\"whobble\"}}", 
				"<foo><bar><qux>quux</qux></bar><baz><whibble>whobble</whibble></baz></foo>");
		}

		[TestMethod]
		public void NestedElementsWithAttributesReadingTest()
		{
			TestJsonXmlReader(
				"{\"foo\":{\"bar\":{\"@qux\":\"quux\"},\"baz\":{\"@whibble\":\"whobble\"}}", 
				"<foo><bar qux=\"quux\"></bar><baz whibble=\"whobble\"></baz></foo>");
		}

		[TestMethod]
		public void ArrayReadingTest()
		{
			TestJsonXmlReader("{\"foos\":[\"bar\", 1, {\"baz\":\"qux\"}]}", "<foos><foo>bar</foo><foo>1</foo><foo><baz>qux</baz></foo></foos>");
		}

		[TestMethod]
		public void RootXPathTest()
		{
			TestJsonXmlReader(
				"{\"foo\":{\"bar\":{\"@qux\":\"quux\"},\"baz\":{\"@whibble\":\"whobble\"}}",
				"<Test><Request><foo><bar qux=\"quux\"></bar><baz whibble=\"whobble\"></baz></foo></Request></Test>",
				"Test/Request");

			TestJsonXmlReader(
				"{\"Fund\":[{\"@Code\":\"509\",\"@IsMyFund\":true}]}",
				"<MyFundUpdate><Request><Fund><Fund Code=\"509\" IsMyFund=\"true\"></Fund></Fund></Request></MyFundUpdate>",
				"MyFundUpdate/Request");
		}

		private static void TestJsonXmlReader(string json, string expected, string rootXPath = null)
		{
			Assert.AreEqual(expected, Convert(json, rootXPath));
		}

		private static string Convert(string json, string rootXPath = null)
		{
			var jsonReader = new JsonTextReader(new StringReader(json));
			var xmlReader = rootXPath == null 
				? new JsonXmlReader(jsonReader) 
				: new JsonXmlReader(jsonReader, rootXPath);

			var xmlDoc = new XmlDocument();
			xmlDoc.Load(xmlReader);
			return xmlDoc.InnerXml;
		}
	}
}
