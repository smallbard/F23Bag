using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F23Bag.Data.DML
{
    public class In : DMLNode
    {
        public In(DMLNode left, IEnumerable<DMLNode> right)
        {
            Left = left;
            Left.Parent = this;
            Right = right;
            foreach (var nd in right) nd.Parent = this;
        }

        public DMLNode Left { get; private set; }

        public IEnumerable<DMLNode> Right { get; private set; }

        public override void Accept(IDMLAstVisitor visitor)
        {
            foreach (var r in Right.Reverse()) r.Accept(visitor);
            Left.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("{ in");
            foreach (var r in Right) sb.Append(" ").Append(r.ToString());
            return sb.Append(" }").ToString();
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            return new In(Left.Clone(source, replace), Right.Select(r => r.Clone(source, replace)).ToList());
        }
    }
}
