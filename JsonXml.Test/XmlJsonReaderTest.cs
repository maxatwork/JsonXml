using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonXml.Test
{
	[TestClass]
	public class XmlJsonReaderTest
	{
		[TestMethod]
		public void EmptyElementTest()
		{
			TestXmlJsonReader("<test/>", "{\"test\":null}");
		}

		[TestMethod]
		public void AttributedEmptyElementTest()
		{
			TestXmlJsonReader("<foo bar=\"baz\" qux=\"quux\" />", "{\"foo\":{\"@bar\":\"baz\",\"@qux\":\"quux\"}}");
		}

        [TestMethod]
        public void EscapedAttributedEmptyElementTest()
        {
            TestXmlJsonReader("<foo bar=\"\\\\&lt;&gt;&amp;&quot;'/\"></foo>", "{\"foo\":{\"@bar\":\"\\\\\\\\<>&\\\"'/\"}}");
        }

		[TestMethod]
		public void NonEmptyElementTest()
		{
			TestXmlJsonReader("<foo>bar \"baz\"</foo>", "{\"foo\":\"bar \\\"baz\\\"\"}");
		}

		[TestMethod]
		public void NonEmptyAttributedElementTest()
		{
			TestXmlJsonReader("<foo bar=\"baz\">qux</foo>", "{\"foo\":{\"@bar\":\"baz\",\"#text\":\"qux\"}}");
		}

		[TestMethod]
		public void NestedElementsTest()
		{
			TestXmlJsonReader("<whibble><foo bar=\"baz\">qux</foo><bar baz=\"qux\" /></whibble>", "{\"whibble\":{\"foo\":{\"@bar\":\"baz\",\"#text\":\"qux\"},\"bar\":{\"@baz\":\"qux\"}}}");
		}

		[TestMethod]
		public void ArrayTest()
		{
			TestXmlJsonReader("<foos><foo bar=\"baz\">1</foo><foo>2</foo><foo>3</foo></foos>", "{\"foos\":[{\"@bar\":\"baz\",\"#text\":1},2,3]}");
		}

		[TestMethod]
		public void ValueTypesTest()
		{
			TestXmlJsonReader("<test><numstring>0123456789012345678901234567890</numstring><int>1</int><float>1.2</float><string>foobar</string><bool>true</bool></test>", "{\"test\":{\"numstring\":\"0123456789012345678901234567890\",\"int\":1,\"float\":1.2,\"string\":\"foobar\",\"bool\":true}}");
		}

		[TestMethod]
		public void RootPathTest()
		{
			TestXmlJsonReader("<GetAllAccountTypes><Response><Types><Type>foo</Type><Type>bar</Type></Types></Response></GetAllAccountTypes>", "{\"Types\":[\"foo\",\"bar\"]}", "/GetAllAccountTypes/Response");
			TestXmlJsonReader("<GetAllAccountTypes><Request><Foo>Bar</Foo></Request><Response><Types><Type>foo</Type><Type>bar</Type></Types></Response></GetAllAccountTypes>", "{\"Types\":[\"foo\",\"bar\"]}", "/*/Response");
			TestXmlJsonReader("<GetAllAccountTypes><Foo><Response><Types><Type>foo</Type><Type>bar</Type></Types></Response></Foo></GetAllAccountTypes>", "{}", "/GetAllAccountTypes/Response");
		}

		[TestMethod]
		public void ProcessingInstructionsTest()
		{
			const string xml = @"
				<?statusCode 200?>
				<?header Foo=""bar""?>
				<?header Baz=""qux""?>
				<Foo>
					<Bar>Baz</Bar>
				</Foo>";

			var xmlReader = new XmlTextReader(new StringReader(xml));
			var jsonReader = new XmlJsonReader(xmlReader);
			jsonReader.ReadProcessingInstructions();
			var textReader = new TextJsonReader(jsonReader);
			var result = textReader.ReadToEnd();

			Assert.AreEqual("Foo=\"bar\"", jsonReader.ProcessingInstructions["header"][0]);
			Assert.AreEqual("Baz=\"qux\"", jsonReader.ProcessingInstructions["header"][1]);
			Assert.AreEqual("200", jsonReader.ProcessingInstructions["statusCode"][0]);
			Assert.AreEqual("{\"Foo\":{\"Bar\":\"Baz\"}}", result);
		}
		
		private static void TestXmlJsonReader(string xml, string expected)
		{
			Assert.AreEqual(expected, Convert(xml, null));
		}

		private static void TestXmlJsonReader(string xml, string expected, string rootPath)
		{
			Assert.AreEqual(expected, Convert(xml, rootPath));
		}

		private static string Convert(string xml, string rootPath)
		{
			var xmlReader = new XmlTextReader(new StringReader(xml));
			var jsonReader = new XmlJsonReader(xmlReader, rootPath);
			var textReader = new TextJsonReader(jsonReader);
			return textReader.ReadToEnd();
		} 
	}
}
