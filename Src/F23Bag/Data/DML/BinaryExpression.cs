namespace F23Bag.Data.DML
{
    public class BinaryExpression : DMLNode
    {
        public BinaryExpression(BinaryExpressionTypeEnum binaryExpressionType, DMLNode left, DMLNode right)
        {
            BinaryExpressionType = binaryExpressionType;
            Left = left;
            Right = right;
            Left.Parent = this;
            Right.Parent = this;
        }

        public BinaryExpressionTypeEnum BinaryExpressionType { get; private set; }

        public DMLNode Left { get; private set; }

        public DMLNode Right { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            Right.Accept(visitor);
            Left.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ " + BinaryExpressionType.ToString() + " : " + Left.ToString() + " : " + Right.ToString() + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new BinaryExpression(BinaryExpressionType, Left.Clone(source, replace), Right.Clone(source, replace));
        }
    }

    public enum BinaryExpressionTypeEnum
    {
        And,
        Or,
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Concat,
        Like,
        Coalesce,
        Add,
        Subtract,
        Multiply,
        Divide
    }
}
