using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace F23Bag.Data
{
    internal static class Evaluator
    {
        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name=”expression”>The root of the expression tree.</param>
        /// <param name=”fnCanBeEvaluated”>A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name=”expression”>The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression)
        {
            return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter && (expression.NodeType != ExpressionType.Call || 
                (((MethodCallExpression)expression).Method.ReflectedType != typeof(QueryableExtension) && ((MethodCallExpression)expression).Method.ReflectedType != typeof(UnitOfWork)));
        }

        /// <summary>
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        private class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return Visit(exp);
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == null) return null;
                if (_candidates.Contains(exp)) return Evaluate(exp);
                return base.Visit(exp);
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                if (node == null) throw new ArgumentNullException(nameof(node));

                var n = (NewExpression)VisitNew(node.NewExpression);
                var bindings = VisitBindingList(node.Bindings);
                if (n != node.NewExpression || bindings != node.Bindings)
                {
                    return Expression.MemberInit(n, bindings);
                }
                return node;
            }

            protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
            {
                List<MemberBinding> list = null;
                for (int i = 0, n = original.Count; i < n; i++)
                {
                    MemberBinding b = VisitBinding(original[i]);
                    if (list != null)
                    {
                        list.Add(b);
                    }
                    else if (b != original[i])
                    {
                        list = new List<MemberBinding>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }
                        list.Add(b);
                    }
                }
                if (list != null)
                    return list;
                return original;
            }

            protected virtual MemberBinding VisitBinding(MemberBinding binding)
            {
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        return this.VisitMemberAssignment((MemberAssignment)binding);
                    case MemberBindingType.MemberBinding:
                        return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                    case MemberBindingType.ListBinding:
                        return this.VisitMemberListBinding((MemberListBinding)binding);
                    default:
                        throw new NotSupportedException($"Unhandled binding type '{binding.BindingType}'");
                }
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant) return e;
                var lambda = Expression.Lambda(e);
                var fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        private class Nominator : ExpressionVisitor
        {
            private readonly Func<Expression, bool> _fnCanBeEvaluated;
            private HashSet<Expression> _candidates;
            private bool _cannotBeEvaluated;
            private bool _inQueryableExtensionOrUnitOfWork;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                _fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                _candidates = new HashSet<Expression>();
                Visit(expression);
                return _candidates;
            }

            public override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    var saveCannotBeEvaluated = _cannotBeEvaluated;
                    _cannotBeEvaluated = false;

                    _inQueryableExtensionOrUnitOfWork = _inQueryableExtensionOrUnitOfWork || expression is MethodCallExpression && 
                        (((MethodCallExpression)expression).Method.ReflectedType == typeof(QueryableExtension) || ((MethodCallExpression)expression).Method.ReflectedType == typeof(UnitOfWork));

                    base.Visit(expression);

                    if (!_cannotBeEvaluated)
                    {
                        if (_fnCanBeEvaluated(expression) && (!_inQueryableExtensionOrUnitOfWork || !(expression is NewExpression)))
                            _candidates.Add(expression);
                        else
                            _cannotBeEvaluated = true;
                    }

                    _cannotBeEvaluated |= saveCannotBeEvaluated;
                }

                return expression;
            }
        }
    }
}
