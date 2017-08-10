using System;

namespace F23Bag.Data.DML
{
    public class Join : DMLNode
    {
        public Join(JoinTypeEnum joinType, AliasDefinition alias, DMLNode condition)
        {
            if (alias == null) throw new ArgumentNullException(nameof(alias));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            JoinType = joinType;
            Alias = alias;
            alias.Parent = this;
            Condition = condition;
            condition.Parent = this;
            Use = 1;
        }

        public JoinTypeEnum JoinType { get; private set; }

        public AliasDefinition Alias { get; private set; }

        public DMLNode Condition { get; set; }

        internal int Use { get; set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            Alias.Accept(visitor);
            Condition.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ " + JoinType.ToString() + " join " + Alias + " on " + Condition.ToString() + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new Join(JoinType, (AliasDefinition)Alias.Clone(source, replace), Condition.Clone(source, replace));
        }
    }

    public enum JoinTypeEnum
    {
        Inner,
        Left
    }
}
