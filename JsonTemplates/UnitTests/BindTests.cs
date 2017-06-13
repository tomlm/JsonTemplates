using System;
using System.Diagnostics;
using System.IO;
using JsonTemplates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnitTests
{
    [TestClass]
    public class BindTests
    {
        public string sourceJson;
        public string expectedJson;
        public JObject source;
        public Source.Rootobject sourcePoco;

        public JObject expected;
        public Expected.Rootobject expectedPoco;

        public JsonTemplate complexTemplate;

        [TestInitialize]
        public void Initialize()
        {

            this.sourceJson = File.ReadAllText(@"..\..\TestData.Json");
            this.source = (JObject)JsonConvert.DeserializeObject(sourceJson);
            this.sourcePoco = JsonConvert.DeserializeObject<Source.Rootobject>(this.sourceJson);

            complexTemplate = new JsonTemplate(File.ReadAllText(@"..\..\TestTemplate.json"));

            this.expectedJson = File.ReadAllText(@"..\..\TestExpected.json");
            this.expected = (JObject)JsonConvert.DeserializeObject(expectedJson);
            this.expectedPoco = JsonConvert.DeserializeObject<Expected.Rootobject>(this.expectedJson);
        }

        [TestMethod]
        public void HotInit()
        {
            var result = complexTemplate.Bind(source);
        }


        [TestMethod]
        public void JObjectToJObject()
        {
            var result = complexTemplate.Bind(source);

            Assert.IsTrue(result["dateBinding"].Type == JTokenType.Date);
            Assert.AreEqual((DateTime)result["dateBinding"], (DateTime)source["dt"]);
            expected["dateBinding"] = source["dt"];
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void PocoToJObject()
        {
            var result = complexTemplate.Bind(sourcePoco);

            Assert.IsTrue(result["dateBinding"].Type == JTokenType.Date);
            Assert.AreEqual((DateTime)result["dateBinding"], sourcePoco.dt);
            expected["dateBinding"] = sourcePoco.dt;
            Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(result));
        }

        [TestMethod]
        public void PocoToPoco()
        {
            var result = complexTemplate.Bind<Expected.Rootobject>(sourcePoco);

            Assert.AreEqual(result.dateBinding, sourcePoco.dt);
            expectedPoco.dateBinding = sourcePoco.dt;
            Assert.AreEqual(JsonConvert.SerializeObject(expectedPoco), JsonConvert.SerializeObject(result));
        }

        [TestMethod]
        public void AdaptiveCard()
        {
            JsonTemplate cardTemplate = new JsonTemplate(File.ReadAllText(@"..\..\cardTemplate.json"));
            var cardData = JsonConvert.DeserializeObject(File.ReadAllText(@"..\..\cardData.json"));
            var result = cardTemplate.Bind(cardData);
            Debug.Print(JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        private static void TestValueProperty<T>(T value)
        {
            dynamic source = new JObject();
            source.value = value;
            JsonTemplate valueTemplate = new JsonTemplate(@"{ 'template':{ 'prop':'{=value}'}}");
            dynamic result = valueTemplate.Bind(source);
            Assert.AreEqual((T)result.prop, value);
        }


        private static void PropertyTestArray<T>(T[] val)
        {
            dynamic source = new JObject();
            source.value = new JArray(val);
            JsonTemplate valueTemplate = new JsonTemplate(@"{ 'template':{ 'prop':'{=value}'}}");
            dynamic result = valueTemplate.Bind(source);
            Assert.AreEqual(result.prop.Type, JTokenType.Array);
            for (int i = 0; i < val.Length; i++)
                Assert.AreEqual((T)result.prop[i], val[i]);
        }

        [TestMethod]
        public void ValueSimpleString()
        {
            TestValueProperty("test");
        }

        [TestMethod]
        public void ValueNumber()
        {
            TestValueProperty(32);
        }

        [TestMethod]
        public void ValueBool()
        {
            TestValueProperty(true);
        }

        [TestMethod]
        public void ValueDateTime()
        {
            TestValueProperty(DateTime.UtcNow);
        }

        [TestMethod]
        public void ValueGuid()
        {
            TestValueProperty(Guid.NewGuid());
        }


        [TestMethod]
        public void ValueStringArray()
        {
            PropertyTestArray(new[] { "one", "two", "three" });
        }


        [TestMethod]
        public void ValueNumberArray()
        {
            PropertyTestArray(new[] { 1, 2, 3 });
        }

        [TestMethod]
        public void ValueComplexString()
        {
            dynamic source = new JObject();
            source.value1 = "value1";
            source.value2 = 2;
            JsonTemplate valueTemplate = new JsonTemplate(@"{'template':{'prop':'One:{value1} Two:{value2}'}}");
            dynamic result = valueTemplate.Bind(source);
            Assert.AreEqual("One:value1 Two:2", (string)result.prop);
        }

        [TestMethod]
        public void ValueComplexStringWithArray()
        {
            dynamic source = new JObject();
            source.items = new JArray(new[] { 1, 2, 3, 4 });
            JsonTemplate valueTemplate = new JsonTemplate(@"{'template':{'prop':'result:{items}.'}}");
            dynamic result = valueTemplate.Bind(source);
            Assert.AreEqual("result:1, 2, 3, 4.", (string)result.prop);
        }

        [TestMethod]
        public void Join()
        {
            dynamic source = new JObject();
            source.items = new JArray(new[] { 1, 2, 3, 4 });
            JsonTemplate valueTemplate = new JsonTemplate(@"{'template':{'prop':'{items}'}}");
            dynamic result = valueTemplate.Bind(source);
            Assert.AreEqual("1, 2, 3, 4", (string)result.prop);
        }


    }
}
