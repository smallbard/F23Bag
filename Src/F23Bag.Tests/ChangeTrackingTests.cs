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

            var state = stateExtractor.GetState(new StateTest()
            {
                IntProperty = 5,
                StrProperty = "test",
                EntityProperty = new StateTest() { IntProperty = 3 },
                CollectionProperty = new List<StateTest>()
                {
                    new StateTest() {StrProperty = "test2" },
                    new StateTest() {EntityProperty = new StateTest() { IntProperty = 4 } }
                }
            });

            Assert.IsNotNull(state);
            Assert.AreEqual(4, state.Length);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), state[0].Item1);
            Assert.AreEqual(5, state[0].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), state[1].Item1);
            Assert.AreEqual("test", state[1].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), state[2].Item1);
            Assert.IsInstanceOfType(state[2].Item2, typeof(Tuple<PropertyInfo, object>[]));

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), state[3].Item1);
            Assert.IsInstanceOfType(state[3].Item2, typeof(object[]));
            Assert.AreEqual(2, ((object[])state[3].Item2).Length);

            var stateEntity = state[2].Item2 as Tuple<PropertyInfo, object>[];
            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), stateEntity[0].Item1);
            Assert.AreEqual(3, stateEntity[0].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), stateEntity[1].Item1);
            Assert.IsNull(stateEntity[1].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), stateEntity[2].Item1);
            Assert.IsNull(stateEntity[2].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), stateEntity[3].Item1);
            Assert.IsInstanceOfType(stateEntity[3].Item2, typeof(object[]));
            Assert.AreEqual(0, ((object[])stateEntity[3].Item2).Length);

            var stateCollectionItem1 = ((object[])state[3].Item2)[0] as Tuple<PropertyInfo, object>[];
            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), stateCollectionItem1[0].Item1);
            Assert.AreEqual(0, stateCollectionItem1[0].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), stateCollectionItem1[1].Item1);
            Assert.AreEqual("test2", stateCollectionItem1[1].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), stateCollectionItem1[2].Item1);
            Assert.IsNull(stateCollectionItem1[2].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), stateCollectionItem1[3].Item1);
            Assert.IsInstanceOfType(stateCollectionItem1[3].Item2, typeof(object[]));
            Assert.AreEqual(0, ((object[])stateCollectionItem1[3].Item2).Length);

            var stateCollectionItem2 = ((object[])state[3].Item2)[1] as Tuple<PropertyInfo, object>[];
            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.IntProperty)), stateCollectionItem2[0].Item1);
            Assert.AreEqual(0, stateCollectionItem2[0].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.StrProperty)), stateCollectionItem2[1].Item1);
            Assert.IsNull(stateCollectionItem2[1].Item2);

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.EntityProperty)), stateCollectionItem2[2].Item1);
            Assert.IsInstanceOfType(stateCollectionItem2[2].Item2, typeof(Tuple<PropertyInfo, object>[]));

            Assert.AreEqual(typeof(StateTest).GetProperty(nameof(StateTest.CollectionProperty)), stateCollectionItem2[3].Item1);
            Assert.IsInstanceOfType(stateCollectionItem2[3].Item2, typeof(object[]));
            Assert.AreEqual(0, ((object[])stateCollectionItem2[3].Item2).Length);
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
