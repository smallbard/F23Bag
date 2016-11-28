namespace F23Bag.Data.DML
{
    public class Identifier : DMLNode
    {
        public Identifier(string identifierName)
        {
            IdentifierName = identifierName;
        }

        public string IdentifierName { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ identifier " + IdentifierName + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new Identifier(IdentifierName);
        }
    }
}
