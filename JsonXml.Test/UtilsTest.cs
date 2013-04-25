using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonXml.Test
{
	[TestClass]
	public class UtilsTest
	{
		[TestMethod]
		public void TestPluralize()
		{
			Assert.AreEqual("Properties", Utils.Pluralize("Property"));
			Assert.AreEqual("Statuses", Utils.Pluralize("Status"));
			Assert.AreEqual("Documents", Utils.Pluralize("Document"));
			
			Assert.AreEqual("kisses", Utils.Pluralize("kiss"));
			Assert.AreEqual("phases", Utils.Pluralize("phase"));
			Assert.AreEqual("dishes", Utils.Pluralize("dish"));
			Assert.AreEqual("massages", Utils.Pluralize("massage"));
			Assert.AreEqual("witches", Utils.Pluralize("witch"));
			Assert.AreEqual("judges", Utils.Pluralize("judge"));

			Assert.AreEqual("Roses", Utils.Pluralize("Rose"));
		}

		[TestMethod]
		public void TestSingularize()
		{
			Assert.AreEqual("Property", Utils.Singularize("Properties"));
			Assert.AreEqual("Status", Utils.Singularize("Statuses"));
			Assert.AreEqual("Document", Utils.Singularize("Documents"));

			Assert.AreEqual("Kiss", Utils.Singularize("Kisses"));
			Assert.AreEqual("Phase", Utils.Singularize("Phases"));
			Assert.AreEqual("Dish", Utils.Singularize("Dishes"));
			Assert.AreEqual("Massage", Utils.Singularize("Massages"));
			Assert.AreEqual("Witch", Utils.Singularize("Witches"));
			Assert.AreEqual("Judge", Utils.Singularize("Judges"));

			Assert.AreEqual("Rose", Utils.Singularize("Roses"));
		}
	}
}
