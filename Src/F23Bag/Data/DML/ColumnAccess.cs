namespace F23Bag.Data.DML
{
    public class ColumnAccess : DMLNode
    {
        public ColumnAccess(AliasDefinition owner, Identifier column)
        {
            Owner = owner;
            Column = column;
        }

        public AliasDefinition Owner { get; private set; }

        public Identifier Column { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            Column.Accept(visitor);
            Owner.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ column " + Owner.ToString() + " . " + Column.ToString() + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new ColumnAccess((AliasDefinition)Owner.Clone(source, replace), (Identifier)Column.Clone(source, replace));
        }
    }
}
