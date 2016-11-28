namespace F23Bag.Data.DML
{
    public class NameAs : DMLNode
    {
        public NameAs(DMLNode sqlNode, string name)
        {
            Name = name;
            SqlNode = sqlNode;
            sqlNode.Parent = this;
        }

        public string Name { get; private set; }

        public DMLNode SqlNode { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            SqlNode.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ " + SqlNode.ToString() + " AS " + Name + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new NameAs(SqlNode.Clone(source, replace), Name);
        }
    }
}
