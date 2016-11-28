namespace F23Bag.Data.DML
{
    public class UpdateOrInsertInfo : DMLNode
    {
        public UpdateOrInsertInfo(DMLNode value, ColumnAccess destination)
        {
            Value = value;
            Destination = destination;
        }

        public DMLNode Value { get; private set; }

        public ColumnAccess Destination { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            Value.Accept(visitor);
            visitor.Visit(this);
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new UpdateOrInsertInfo(Value.Clone(source, replace), Destination);
        }
    }
}
