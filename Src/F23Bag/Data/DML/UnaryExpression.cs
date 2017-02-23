namespace F23Bag.Data.DML
{
    public class UnaryExpression : DMLNode
    {
        public UnaryExpression(UnaryExpressionTypeEnum unaryExpressionType, DMLNode operand)
        {
            UnaryExpressionType = unaryExpressionType;
            Operand = operand;
            if (operand != null) operand.Parent = this;
        }

        public UnaryExpressionTypeEnum UnaryExpressionType { get; private set; }

        public DMLNode Operand { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            if (Operand != null) Operand.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ " + UnaryExpressionType.ToString() + (Operand != null ? " " + Operand.ToString() : "") + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new UnaryExpression(UnaryExpressionType, Operand?.Clone(source, replace));
        }
    }

    public enum UnaryExpressionTypeEnum
    {
        Not,
        Max,
        Min,
        Sum,
        Average,
        Lower,
        Upper,
        Count,
        Exists
    }
}
