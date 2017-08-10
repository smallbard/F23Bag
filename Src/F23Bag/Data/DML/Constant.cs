using System;
using System.Reflection;

namespace F23Bag.Data.DML
{
    public class Constant : DMLNode
    {
        private static readonly MethodInfo _getDbValueMethodInfo = typeof(Constant).GetMethod("GetDbValue", BindingFlags.NonPublic | BindingFlags.Static);

        public Constant(object value, ISQLMapping sqlMapping)
        {
            if (value != null)
            {
                var valueType = value.GetType();

                if (valueType.IsEntityOrCollection())
                {
                    if (sqlMapping == null) throw new ArgumentNullException(nameof(sqlMapping));
                    value = new PropertyAccessorCompiler(sqlMapping.GetIdProperty(valueType)).GetPropertyValue(value);
                }
                else if (valueType.IsEnum)
                    value = Convert.ToInt32(value);
                else
                {
                    var dbValueType = valueType.GetDbValueType();
                    if (dbValueType != valueType) value = _getDbValueMethodInfo.MakeGenericMethod(dbValueType).Invoke(null, new[] { value });
                }
            }

            DbValue = value;
        }

        public object DbValue { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ constant " + (DbValue == null ? "null" : DbValue.ToString()) + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new Constant(DbValue, null);
        }

        private static TDbType GetDbValue<TDbType>(IDbValueType<TDbType> value)
        {
            return value.GetDbValue();
        }
    }
}
