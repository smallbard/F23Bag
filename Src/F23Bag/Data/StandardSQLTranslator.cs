using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using F23Bag.Data.DML;

namespace F23Bag.Data
{
    public abstract class StandardSQLTranslator : ISQLTranslator, IDMLAstVisitor
    {
        protected readonly Stack<StringBuilder> _sqlElements = new Stack<StringBuilder>();
        protected readonly Dictionary<AliasDefinition, string> _aliasNames = new Dictionary<AliasDefinition, string>();
        protected Request _request;
        protected ICollection<Tuple<string, object>> _parameters;

        public string Translate(Request request, ICollection<Tuple<string, object>> parameters)
        {
            _sqlElements.Clear();
            _aliasNames.Clear();
            _request = request;
            _parameters = parameters;
            request.Accept(this);

            return _sqlElements.Pop().ToString();
        }

        public virtual void Visit(Join join)
        {
            if (join == null) throw new ArgumentNullException(nameof(join));

            var condition = _sqlElements.Pop();
            var alias = _sqlElements.Pop();
            _sqlElements.Push(alias.Insert(0, join.JoinType == JoinTypeEnum.Inner ? " INNER JOIN " : " LEFT JOIN ").Append(" ON ").Append(condition));
        }

        public virtual void Visit(OrderElement orderElement)
        {
            if (orderElement == null) throw new ArgumentNullException(nameof(orderElement));

            _sqlElements.Push(_sqlElements.Pop().Append(orderElement.Ascending ? " ASC" : " DESC"));
        }

        public virtual void Visit(UpdateOrInsertInfo updateInfo)
        {
            if (updateInfo == null) throw new ArgumentNullException(nameof(updateInfo));

            if (_request.RequestType == RequestType.InsertSelect || _request.RequestType == RequestType.InsertValues)
                _sqlElements.Push(_sqlElements.Pop());
            else
                _sqlElements.Push(_sqlElements.Pop().Insert(0, " = ").Insert(0, updateInfo.Destination.Column.IdentifierName));
        }

        public virtual void Visit(SelectInfo selectInfo) { }

        public virtual void Visit(UnaryExpression unaryExpression)
        {
            if (unaryExpression == null) throw new ArgumentNullException(nameof(unaryExpression));

            switch (unaryExpression.UnaryExpressionType)
            {
                case UnaryExpressionTypeEnum.Average:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "AVG(").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Count:
                    if (unaryExpression.Operand != null)
                        _sqlElements.Push(_sqlElements.Pop().Insert(0, "COUNT(").Append(')'));
                    else
                        _sqlElements.Push(new StringBuilder("COUNT(*)"));
                    break;
                case UnaryExpressionTypeEnum.Lower:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "LOWER(").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Max:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "MAX(").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Min:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "MIN(").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Not:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "NOT (").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Sum:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "SUM(").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Upper:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "UPPER(").Append(')'));
                    break;
                case UnaryExpressionTypeEnum.Exists:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "EXISTS(").Append(')'));
                    break;
            }
        }

        public virtual void Visit(In @in)
        {
            if (@in == null) throw new ArgumentNullException(nameof(@in));

            var sb = _sqlElements.Pop().Append(" IN (");
            for (var i = 0; i < @in.Right.Count(); i++) sb.Append(_sqlElements.Pop()).Append(',');
            sb.Length--;
            sb.Append(')');
            _sqlElements.Push(sb);
        }

        public virtual void Visit(ColumnAccess columnAccess)
        {
            if (columnAccess == null) throw new ArgumentNullException(nameof(columnAccess));

            if ((_request.RequestType == RequestType.InsertSelect || _request.RequestType == RequestType.InsertValues || _request.RequestType == RequestType.Update || _request.RequestType == RequestType.Delete) && columnAccess.Parent.Request == _request)
                _sqlElements.Pop();
            else
                _sqlElements.Push(_sqlElements.Pop().Append('.').Append(_sqlElements.Pop()));
        }

        public virtual void Visit(Identifier identifier)
        {
            if (identifier == null) throw new ArgumentNullException(nameof(identifier));

            _sqlElements.Push(new StringBuilder(identifier.IdentifierName));
        }

        public virtual void Visit(NameAs nameAs)
        {
            if (nameAs == null) throw new ArgumentNullException(nameof(nameAs));

            _sqlElements.Push(_sqlElements.Pop().Append(" AS ").Append(nameAs.Name));
        }

        public virtual void Visit(ConditionalExpression conditionalExpression)
        {
            if (conditionalExpression == null) throw new ArgumentNullException(nameof(conditionalExpression));

            _sqlElements.Push(_sqlElements.Pop().Insert(0, "CASE WHEN ").Append(" THEN ").Append(_sqlElements.Pop()).Append(" ELSE ").Append(_sqlElements.Pop()).Append(" END"));
        }

        public abstract void Visit(Request request);

        public abstract void Visit(AliasDefinition aliasDefinition);

        public abstract void Visit(BinaryExpression binaryExpression);

        public abstract void Visit(Constant constant);
    }
}
