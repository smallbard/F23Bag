using F23Bag.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using F23Bag.Data.DML;

namespace F23Bag.ISeries
{
    internal class ISeriesSQLTranslator : ISQLTranslator, IDMLAstVisitor
    {
        private readonly Stack<StringBuilder> _sqlElements = new Stack<StringBuilder>();
        private readonly Dictionary<AliasDefinition, string> _aliasNames = new Dictionary<AliasDefinition, string>();
        private Request _request;
        private ICollection<Tuple<string, object>> _parameters;

        public string Translate(Request request, ICollection<Tuple<string, object>> parameters)
        {
            _sqlElements.Clear();
            _aliasNames.Clear();
            _request = request;
            _parameters = parameters;
            request.Accept(this);

            return _sqlElements.Pop().ToString();
        }

        public void Visit(Identifier identifier)
        {
            _sqlElements.Push(new StringBuilder(identifier.IdentifierName));
        }

        public void Visit(Request request)
        {
            var sb = new StringBuilder();
            var fromIndex = -1;

            if (request.RequestType == RequestType.Delete)
            {
                sb.Append("DELETE FROM ");
                fromIndex = sb.Length;
            }
            else if (request.RequestType == RequestType.Select)
            {
                sb.Append("SELECT ");
                if (request.HasDistinct) sb.Append("DISTINCT ");

                for (var i = 0; i < request.Select.Count; i++) sb.Append(_sqlElements.Pop()).Append(", ");
                sb.Length -= 2;

                if (request.Take > 0 || request.Skip > 0) AddPagination(request, sb);

                fromIndex = sb.Length;
            }
            else if (request.RequestType == RequestType.Update)
            {
                sb.Append("UPDATE ").Append(((Identifier)request.FromAlias.Definition).IdentifierName).Append(" SET ");
                for (var i = 0; i < request.UpdateOrInsert.Count; i++) sb.Append(_sqlElements.Pop()).Append(", ");
                sb.Length -= 2;
            }
            else if (request.RequestType == RequestType.InsertSelect)
            {
                sb.Append("INSERT INTO ").Append(((Identifier)request.FromAlias.Definition).IdentifierName).Append("(");
                foreach (var insert in _request.UpdateOrInsert) sb.Append(insert.Destination.Column.IdentifierName).Append(", ");
                sb.Length -= 2;
                sb.Append(") SELECT ");
                if (request.HasDistinct) sb.Append("DISTINCT ");
                for (var i = 0; i < request.UpdateOrInsert.Count; i++) sb.Append(_sqlElements.Pop()).Append(", ");
                sb.Length -= 2;
                fromIndex = sb.Length;
            }
            else if (request.RequestType == RequestType.InsertValues)
            {
                sb.Append("INSERT INTO ").Append(((Identifier)request.FromAlias.Definition).IdentifierName).Append("(");
                foreach (var insert in _request.UpdateOrInsert) sb.Append(insert.Destination.Column.IdentifierName).Append(", ");
                sb.Length -= 2;
                sb.Append(") VALUES (");
                for (var i = 0; i < request.UpdateOrInsert.Count; i++) sb.Append(_sqlElements.Pop()).Append(", ");
                sb.Length -= 2;
                sb.Append(")");
            }

            if (request.Where != null) sb.Append(" WHERE ").Append(_sqlElements.Pop());

            if (request.GroupBy.Count > 0)
            {
                sb.Append(" GROUP BY ");
                for (var i = 0; i < request.GroupBy.Count; i++) sb.Append(_sqlElements.Pop()).Append(", ");
                sb.Length -= 2;
            }

            if (request.Having != null) sb.Append(" HAVING ").Append(_sqlElements.Pop());

            if (request.Orders.Count > 0)
            {
                sb.Append(" ORDER BY ");
                for (var i = 0; i < request.Orders.Count; i++) sb.Append(_sqlElements.Pop()).Append(", ");
                sb.Length -= 2;
            }

            if (request.Take > 0 || request.Skip > 0)
            {
                fromIndex += "SELECT * FROM (".Length;
                sb.Insert(0, "SELECT * FROM (").Append(") T_PAGINATED WHERE ");
                if (request.Skip > 0)
                {
                    sb.Append("T_PAGINATED.DSRANK > ").Append(request.Skip);
                    if (request.Take > 0) sb.Append(" AND ");
                }

                if (request.Take > 0)
                {
                    sb.Append("T_PAGINATED.DSRANK <= ").Append(request.Take + request.Skip);
                }
            }

            for (var i = 0; i < request.Joins.Count; i++)
            {
                var join = _sqlElements.Pop();
                sb.Insert(fromIndex, join);
            }

            var fromAlias = _sqlElements.Pop();
            if (request.RequestType == RequestType.Select || request.RequestType == RequestType.Delete || request.RequestType == RequestType.InsertSelect)
            {
                if (request.RequestType == RequestType.Delete)
                    sb.Insert(fromIndex, ((Identifier)request.FromAlias.Definition).IdentifierName);
                else
                    sb.Insert(fromIndex, fromAlias.Insert(0, " FROM "));
            }

            if (request.RequestType == RequestType.InsertValues) sb.Insert(0, "SELECT " + request.IdColumnName + " FROM FINAL TABLE(").Append(')');

            if (request.Parent != null && !(request.Parent is BinaryExpression) && !(request.Parent is UnaryExpression)) sb.Insert(0, '(').Append(')');

            _sqlElements.Push(sb);
        }

        public void Visit(Join join)
        {
            var condition = _sqlElements.Pop();
            var alias = _sqlElements.Pop();
            _sqlElements.Push(alias.Insert(0, join.JoinType == JoinTypeEnum.Inner ? " INNER JOIN " : " LEFT JOIN ").Append(" ON ").Append(condition));
        }

        public void Visit(OrderElement orderElement)
        {
            _sqlElements.Push(_sqlElements.Pop().Append(orderElement.Ascending ? " ASC" : " DESC"));
        }

        public void Visit(UpdateOrInsertInfo updateInfo)
        {
            if (_request.RequestType == RequestType.InsertSelect || _request.RequestType == RequestType.InsertValues)
                _sqlElements.Push(_sqlElements.Pop());
            else
                _sqlElements.Push(_sqlElements.Pop().Insert(0, " = ").Insert(0, updateInfo.Destination.Column.IdentifierName));
        }

        public void Visit(SelectInfo selectInfo) { }

        public void Visit(UnaryExpression unaryExpression)
        {
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
            }
        }

        public void Visit(In @in)
        {
            var sb = _sqlElements.Pop().Append(" IN (");
            for (var i = 0; i < @in.Right.Count(); i++) sb.Append(_sqlElements.Pop()).Append(',');
            sb.Length--;
            sb.Append(')');
            _sqlElements.Push(sb);
        }

        public void Visit(Constant constant)
        {
            if (constant.Value == null)
                _sqlElements.Push(new StringBuilder("NULL"));
            else
            {
                var parameterName = new StringBuilder("@p").Append(_parameters.Count);
                _sqlElements.Push(parameterName);
                _parameters.Add(Tuple.Create(parameterName.ToString(), constant.Value));
            }
        }

        public void Visit(ColumnAccess columnAccess)
        {
            if ((_request.RequestType == RequestType.InsertSelect || _request.RequestType == RequestType.InsertValues || _request.RequestType == RequestType.Update || _request.RequestType == RequestType.Delete) && columnAccess.Parent.GetRequest() == _request)
                _sqlElements.Pop();
            else
                _sqlElements.Push(_sqlElements.Pop().Append('.').Append(_sqlElements.Pop()));
        }

        public void Visit(BinaryExpression binaryExpression)
        {
            switch (binaryExpression.BinaryExpressionType)
            {
                case BinaryExpressionTypeEnum.And:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") AND (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Coalesce:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, "COALESCE(").Append(",").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Concat:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") || (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Equal:
                    var leftEqual = _sqlElements.Pop();
                    var rightEqual = _sqlElements.Pop();

                    if (rightEqual.ToString() == "NULL")
                        _sqlElements.Push(leftEqual.Insert(0, '(').Append(") IS NULL"));
                    else
                        _sqlElements.Push(leftEqual.Insert(0, '(').Append(") = (").Append(rightEqual).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.GreaterThan:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") > (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.GreaterThanOrEqual:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") >= (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.LessThan:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") < (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.LessThanOrEqual:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") <= (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Like:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") LIKE (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.NotEqual:
                    var leftNotEqual = _sqlElements.Pop();
                    var rightNotEqual = _sqlElements.Pop();

                    if (rightNotEqual.ToString() == "NULL")
                        _sqlElements.Push(leftNotEqual.Insert(0, '(').Append(") IS NOT NULL"));
                    else
                        _sqlElements.Push(leftNotEqual.Insert(0, '(').Append(") <> (").Append(rightNotEqual).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Or:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") OR (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Add:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") + (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Subtract:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") - (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Multiply:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") * (").Append(_sqlElements.Pop()).Append(')'));
                    break;
                case BinaryExpressionTypeEnum.Divide:
                    _sqlElements.Push(_sqlElements.Pop().Insert(0, '(').Append(") / (").Append(_sqlElements.Pop()).Append(')'));
                    break;
            }
        }

        public void Visit(AliasDefinition aliasDefinition)
        {
            var definition = _sqlElements.Pop();

            if (_aliasNames.ContainsKey(aliasDefinition))
                _sqlElements.Push(new StringBuilder(_aliasNames[aliasDefinition]));
            else
            {
                var alias = new StringBuilder("a").Append(_aliasNames.Count);
                _aliasNames[aliasDefinition] = alias.ToString();
                alias.Insert(0, ' ').Insert(0, definition);
                _sqlElements.Push(alias);
            }
        }

        public void Visit(NameAs nameAs)
        {
            _sqlElements.Push(_sqlElements.Pop().Append(" AS ").Append(nameAs.Name));
        }

        public void Visit(ConditionalExpression conditionalExpression)
        {
            _sqlElements.Push(_sqlElements.Pop().Insert(0, "CASE WHEN ").Append(" THEN ").Append(_sqlElements.Pop()).Append(" ELSE ").Append(_sqlElements.Pop()).Append(" END"));
        }

        private void AddPagination(Request request, StringBuilder sb)
        {
            sb.Append(", DENSE_RANK() OVER( ORDER BY ");

            foreach (var order in request.Orders)
            {
                var columnAccess = order.OrderOn as ColumnAccess;
                if (columnAccess == null) throw new NotSupportedException("Only order by on column are supported with pagination.");

                sb.Append(_aliasNames[columnAccess.Owner]).Append(".").Append(columnAccess.Column.IdentifierName);
                if (!order.Ascending) sb.Append(" DESC ");
                sb.Append(',');
            }
            sb.Length -= 1;
            sb.Append(") AS DSRANK");
        }
    }
}