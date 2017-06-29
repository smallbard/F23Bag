using F23Bag.Data;
using F23Bag.Data.ChangeTracking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests
{
    [TestClass]
    public class ChangeTrackingTests
    {
        [TestMethod]
        public void StateExtraction()
        {
            var stateExtractor = StateExtractor.GetStateExtractor(new DefaultSqlMapping(null), typeof(StateTest));

            var o = new StateTest()
            {
                IntProperty = 5,
                StrProperty = "test",
                EntityProperty = new StateTest() { IntProperty = 3 },
                CollectionProperty = new List<StateTest>()
                {
                    new StateTest() {StrProperty = "test2" },
                    new StateTest() {EntityProperty = new StateTest() { IntProperty = 4 } }
                }
            };
            var state = stateExtractor.GetAllComponentStates(o)[o];

            Assert.IsNotNull(state);
            Assert.AreEqual(4, state.StateElements.Length);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), state.StateElements[0].Property);
            Assert.AreEqual(5, state.StateElements[0].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), state.StateElements[1].Property);
            Assert.AreEqual("test", state.StateElements[1].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), state.StateElements[2].Property);
            Assert.IsInstanceOfType(state.StateElements[2].Value, typeof(State));

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), state.StateElements[3].Property);
            Assert.IsInstanceOfType(state.StateElements[3].Value, typeof(object[]));
            Assert.AreEqual(2, ((object[])state.StateElements[3].Value).Length);

            var stateEntity = state.StateElements[2].Value as State;
            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), stateEntity.StateElements[0].Property);
            Assert.AreEqual(3, stateEntity.StateElements[0].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), stateEntity.StateElements[1].Property);
            Assert.IsNull(stateEntity.StateElements[1].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), stateEntity.StateElements[2].Property);
            Assert.IsNull(stateEntity.StateElements[2].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), stateEntity.StateElements[3].Property);
            Assert.IsInstanceOfType(stateEntity.StateElements[3].Value, typeof(object[]));
            Assert.AreEqual(0, ((object[])stateEntity.StateElements[3].Value).Length);

            var stateCollectionItem1 = ((object[])state.StateElements[3].Value)[0] as State;
            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), stateCollectionItem1.StateElements[0].Property);
            Assert.AreEqual(0, stateCollectionItem1.StateElements[0].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), stateCollectionItem1.StateElements[1].Property);
            Assert.AreEqual("test2", stateCollectionItem1.StateElements[1].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), stateCollectionItem1.StateElements[2].Property);
            Assert.IsNull(stateCollectionItem1.StateElements[2].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), stateCollectionItem1.StateElements[3].Property);
            Assert.IsInstanceOfType(stateCollectionItem1.StateElements[3].Value, typeof(object[]));
            Assert.AreEqual(0, ((object[])stateCollectionItem1.StateElements[3].Value).Length);

            var stateCollectionItem2 = ((object[])state.StateElements[3].Value)[1] as State;
            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), stateCollectionItem2.StateElements[0].Property);
            Assert.AreEqual(0, stateCollectionItem2.StateElements[0].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), stateCollectionItem2.StateElements[1].Property);
            Assert.IsNull(stateCollectionItem2.StateElements[1].Value);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), stateCollectionItem2.StateElements[2].Property);
            Assert.IsInstanceOfType(stateCollectionItem2.StateElements[2].Value, typeof(State));

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), stateCollectionItem2.StateElements[3].Property);
            Assert.IsInstanceOfType(stateCollectionItem2.StateElements[3].Value, typeof(object[]));
            Assert.AreEqual(0, ((object[])stateCollectionItem2.StateElements[3].Value).Length);
        }

        private class StateTest
        {
            public int IntProperty { get; set; }

            public string StrProperty { get; set; }

            public StateTest EntityProperty { get; set; }

            public List<StateTest> CollectionProperty { get; set; }
        }
    }
}
