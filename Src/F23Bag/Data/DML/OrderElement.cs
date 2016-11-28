namespace F23Bag.Data.DML
{
    public class OrderElement : DMLNode
    {
        public OrderElement(DMLNode orderOn, bool ascending)
        {
            OrderOn = orderOn;
            orderOn.Parent = this;
            Ascending = ascending;
        }

        public DMLNode OrderOn { get; private set; }

        public bool Ascending { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            OrderOn.Accept(visitor);
            visitor.Visit(this);
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new OrderElement(OrderOn.Clone(source, replace), Ascending);
        }
    }
}
