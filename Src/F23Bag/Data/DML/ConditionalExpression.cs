using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data.DML
{
    public class ConditionalExpression : DMLNode
    {
        public ConditionalExpression(DMLNode conditionExpression, DMLNode thenExpression, DMLNode elseExpression)
        {
            ConditionExpression = conditionExpression;
            ThenExpression = thenExpression;
            ElseExpression = elseExpression;
        }

        public DMLNode ConditionExpression { get; private set; }

        public DMLNode ThenExpression { get; private set; }

        public DMLNode ElseExpression { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            ConditionExpression.Accept(visitor);
            ThenExpression.Accept(visitor);
            ElseExpression.Accept(visitor);
            visitor.Visit(this);
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new ConditionalExpression(ConditionExpression.Clone(source, replace), ThenExpression.Clone(source, replace), ElseExpression.Clone(source, replace));
        }
    }
}
