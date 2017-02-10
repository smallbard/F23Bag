using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data.DML
{
    public class SelectInfo : DMLNode
    {
        public SelectInfo(DMLNode selectSql, PropertyInfo property)
        {
            SelectSql = selectSql;
            Property = property;

            SetPropertyValue = new PropertyAccessorCompiler(property).SetPropertyValue;
        }

        public DMLNode SelectSql { get; private set; }

        public PropertyInfo Property { get; private set; }

        public Action<object,object> SetPropertyValue { get; private set; }

        public override string ToString()
        {
            return SelectSql.ToString();
        }

        public override void Accept(IDMLAstVisitor visitor)
        {
            SelectSql.Accept(visitor);
            visitor.Visit(this);
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new SelectInfo(SelectSql.Clone(source, replace), Property);
        }
    }
}
