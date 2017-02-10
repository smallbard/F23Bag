namespace F23Bag.Data.DML
{
    public interface IDMLAstVisitor
    {
        void Visit(AliasDefinition aliasDefinition);
        void Visit(BinaryExpression binaryExpression);
        void Visit(ColumnAccess columnAccess);
        void Visit(Constant constant);
        void Visit(Identifier identifier);
        void Visit(In @in);
        void Visit(Join join);
        void Visit(OrderElement orderElement);
        void Visit(NameAs nameAs);
        void Visit(SelectInfo selectInfo);
        void Visit(UpdateOrInsertInfo updateInfo);
        void Visit(Request request);
        void Visit(UnaryExpression unaryExpression);
        void Visit(ConditionalExpression conditionalExpression);
    }
}