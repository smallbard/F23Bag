using System.Collections.Generic;

namespace F23Bag.Data.DML
{
    public class AliasDefinition : DMLNode
    {
        public AliasDefinition(DMLNode definition)
        {
            Definition = definition;
            Definition.Parent = this;
            Equivalents = new List<object>();
        }

        public DMLNode Definition { get; private set; }

        public ICollection<object> Equivalents { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            Definition.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{ alias " + Definition.ToString() + " }";
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            if (source == this) return replace;
            var alias = new AliasDefinition(Definition.Clone(source, replace));
            foreach (var eq in Equivalents) alias.Equivalents.Add(eq);
            return alias;
        }
    }
}
