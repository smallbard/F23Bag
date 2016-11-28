namespace F23Bag.Data.DML
{
    public class Constant : DMLNode
    {
        public Constant(object value)
        {
            Value = value;
        }

        public object Value { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ constant " + (Value == null ? "null" : Value.ToString()) + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new Constant(Value);
        }
    }
}
