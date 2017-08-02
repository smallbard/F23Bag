using F23Bag.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using F23Bag.Data.DML;

namespace F23Bag.SQLite
{
    internal class SQLiteSQLTranslator : StandardSQLTranslator
    {
        private const string cstDenseRankPlaceHolder = "[[DENSE_RANK]]";
        private const string cstDenseRankFilterPlaceHolder = "[[DENSE_RANK_FILTER]]";
        private readonly string _aliasSuffix;

        public SQLiteSQLTranslator()
            : this("")
        { }

        private SQLiteSQLTranslator(string aliasSuffix)
        {
            _aliasSuffix = aliasSuffix;
        }

        public override void Visit(Request request)
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

                if (request.Take > 0 || request.Skip > 0) sb.Append(cstDenseRankPlaceHolder);

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

            if (request.Take > 0 || request.Skip > 0) sb.Append(cstDenseRankFilterPlaceHolder);

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

            if (request.RequestType == RequestType.InsertSelect || request.RequestType == RequestType.Update || request.RequestType == RequestType.Delete)
                sb.Append(";SELECT changes()");
            else if (request.RequestType == RequestType.InsertValues)
                sb.Append(";SELECT last_insert_rowid()");

            if (request.Parent != null && !(request.Parent is BinaryExpression) && !(request.Parent is UnaryExpression)) sb.Insert(0, '(').Append(')');

            if (request.Take > 0 || request.Skip > 0) AddPagination(request, sb, fromIndex);

            _sqlElements.Push(sb);
        }

        public override void Visit(Constant constant)
        {
            if (constant.Value == null)
                _sqlElements.Push(new StringBuilder("NULL"));
            else
            {
                var parameterName = new StringBuilder("@p").Append(_parameters.Count);
                _sqlElements.Push(parameterName);

                var value = constant.Value;
                if (value.GetType().IsClass && value.GetType() != typeof(string))
                    value = value.GetType().GetProperty("Id").GetValue(value);
                else if (value.GetType().IsEnum)
                    value = Convert.ToInt32(value);

                _parameters.Add(Tuple.Create(parameterName.ToString(), value));
            }
        }

        public override void Visit(BinaryExpression binaryExpression)
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

        public override void Visit(AliasDefinition aliasDefinition)
        {
            var definition = _sqlElements.Pop();

            if (_aliasNames.ContainsKey(aliasDefinition))
                _sqlElements.Push(new StringBuilder(_aliasNames[aliasDefinition]));
            else
            {
                var alias = new StringBuilder("a").Append(_aliasNames.Count).Append(_aliasSuffix);
                _aliasNames[aliasDefinition] = alias.ToString();
                alias.Insert(0, ' ').Insert(0, definition);
                _sqlElements.Push(alias);
            }
        }

        private void AddPagination(Request request, StringBuilder sb, int fromIndex)
        {
            var oldHasDistinct = request.HasDistinct;
            var oldTake = request.Take;
            var oldSkip = request.Skip;
            var oldOrder = request.Orders.ToList();

            request.HasDistinct = true;
            request.Take = 0;
            request.Skip = 0;
            request.Orders.Clear();

            var translator = new SQLiteSQLTranslator("ds");
            foreach (var alias in _aliasNames.Keys) translator._aliasNames[alias] = _aliasNames[alias] + "bis";
            var denseRankExpression = new StringBuilder(", (SELECT COUNT() FROM (").Append(translator.Translate(request, _parameters));

            if (request.Where == null)
                denseRankExpression.Append(" WHERE ");
            else
                denseRankExpression.Append(" AND ");

            foreach (var order in oldOrder)
            {
                var columnAccess = order.OrderOn as ColumnAccess;
                if (columnAccess == null) throw new NotSupportedException("Only order by on column are supported with pagination.");

                denseRankExpression.Append(translator._aliasNames[columnAccess.Owner]).Append(".").Append(columnAccess.Column.IdentifierName);
                if (order.Ascending)
                    denseRankExpression.Append(" > ");
                else
                    denseRankExpression.Append(" < ");
                denseRankExpression.Append(_aliasNames[columnAccess.Owner]).Append(".").Append(columnAccess.Column.IdentifierName).Append(" AND ");
            }
            denseRankExpression.Length -= 5;
            denseRankExpression.Append(")) AS DSRANK");

            request.HasDistinct = oldHasDistinct;
            request.Take = oldTake;
            request.Skip = oldSkip;
            foreach (var oi in oldOrder) request.Orders.Add(oi);

            sb.Replace(cstDenseRankPlaceHolder, denseRankExpression.ToString());

            var filterDenseRank = new StringBuilder();
            if (request.Where == null)
                filterDenseRank.Append(" WHERE ");
            else
                filterDenseRank.Append(" AND ");

            if (request.Skip > 0)
            {
                filterDenseRank.Append("DSRANK > ").Append(request.Skip);
                if (request.Take > 0) filterDenseRank.Append(" AND ");
            }

            if (request.Take > 0)
            {
                filterDenseRank.Append("DSRANK <= ").Append(request.Take + request.Skip);
            }

            sb.Replace(cstDenseRankFilterPlaceHolder, filterDenseRank.ToString());
        }
    }
}
