using System;

namespace F23Bag.Data.DML
{
    public class OrderElement : DMLNode
    {
        public OrderElement(DMLNode orderOn, bool ascending)
        {
            if (orderOn == null) throw new ArgumentNullException(nameof(orderOn));

            OrderOn = orderOn;
            orderOn.Parent = this;
            Ascending = ascending;
        }

        public DMLNode OrderOn { get; private set; }

        public bool Ascending { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            OrderOn.Accept(visitor);
            visitor.Visit(this);
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new OrderElement(OrderOn.Clone(source, replace), Ascending);
        }
    }
}
