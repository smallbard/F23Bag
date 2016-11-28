using F23Bag.AutomaticUI;
using F23Bag.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests
{
    [TestClass]
    public class ValidatorTests
    {
        [TestMethod]
        public void MandatoryProperties()
        {
            var messages = new List<ValidationEventArgs>();

            var obj = new ObjectToValidate();
            obj.ValidationInfoCreated += (s, e) => messages.Add(e);
            
            obj.Validate();
            Assert.AreEqual(3, messages.Count);
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P1" && m.Level == ValidationLevel.Error));
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P2" && m.Level == ValidationLevel.Error));
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P3" && m.Level == ValidationLevel.Error));
            messages.Clear();

            obj.P1 = "";

            obj.Validate();
            Assert.AreEqual(3, messages.Count);
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P1" && m.Level == ValidationLevel.Error));
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P2" && m.Level == ValidationLevel.Error));
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P3" && m.Level == ValidationLevel.Error));
            messages.Clear();

            obj.P1 = "test";
            obj.Validate();
            Assert.AreEqual(2, messages.Count);
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P2" && m.Level == ValidationLevel.Error));
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P3" && m.Level == ValidationLevel.Error));
            messages.Clear();

            obj.P3 = 5;
            obj.Validate();
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P2" && m.Level == ValidationLevel.Error));
            messages.Clear();
        }

        [TestMethod]
        public void MaxLengthProperties()
        {
            var messages = new List<ValidationEventArgs>();

            var obj = new ObjectToValidate()
            {
                P1 = "test",
                P2 = "1",
                P3 = 5
            };
            obj.ValidationInfoCreated += (s, e) => messages.Add(e);

            obj.Validate();
            Assert.AreEqual(0, messages.Count);

            obj.P2 = "12345";

            obj.Validate();
            Assert.AreEqual(0, messages.Count);

            obj.P2 = "123456";

            obj.Validate();
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P2" && m.Level == ValidationLevel.Error));
        }

        [TestMethod]
        public void RegexProperties()
        {
            var messages = new List<ValidationEventArgs>();

            var obj = new ObjectToValidate()
            {
                P1 = "test",
                P2 = "tes1t",
                P3 = 5
            };

            obj.ValidationInfoCreated += (s, e) => messages.Add(e);

            obj.Validate();
            Assert.AreEqual(0, messages.Count);

            obj.P2 = "test";
            obj.Validate();
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(messages.Any(m => m.Property.Name == "P2" && m.Level == ValidationLevel.Error));
            messages.Clear();

            obj.P2 = "123";
            obj.Validate();
            Assert.AreEqual(0, messages.Count);
        }

        public class ObjectToValidate : IHasValidation
        {
            [Mandatory]
            public string P1 { get; set; }

            [Mandatory, MaxLength(5), Regex(@"\d+")]
            public string P2 { get; set; }

            [Mandatory]
            public int? P3 { get; set; }

            public decimal? P4 { get; set; }

            public event EventHandler<ValidationEventArgs> ValidationInfoCreated;

            public void Validate()
            {
                foreach (var vr in new Validator<ObjectToValidate>(new DefaultI18n()).Validate(this)) OnValidationInfoCreated(vr);
            }

            protected virtual void OnValidationInfoCreated(ValidationEventArgs e)
            {
                ValidationInfoCreated?.Invoke(this, e);
            }
        }

        private class DefaultI18n : I18n
        {
            public string GetTranslation(string message)
            {
                return message;
            }
        }
    }
}
