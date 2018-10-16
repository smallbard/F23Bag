using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace F23Bag.Data.DML
{
    public class Request : DMLNode
    {
        private readonly Request _parentRequest;
        private AliasDefinition _fromAlias;
        private DMLNode _where;
        private DMLNode _having;

        public Request()
        {
            Joins = new ObservableCollection<Join>();
            ((ObservableCollection<Join>)Joins).CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null) foreach (var j in e.OldItems.OfType<DMLNode>()) j.Parent = null;
                if (e.NewItems != null) foreach (var j in e.NewItems.OfType<DMLNode>()) j.Parent = this;
            };
            Select = new ObservableCollection<SelectInfo>();
            ((ObservableCollection<SelectInfo>)Select).CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null) foreach (var si in e.OldItems.OfType<SelectInfo>()) si.SelectSql.Parent = null;
                if (e.NewItems != null) foreach (var si in e.NewItems.OfType<SelectInfo>()) si.SelectSql.Parent = this;
            };
            Orders = new ObservableCollection<OrderElement>();
            ((ObservableCollection<OrderElement>)Orders).CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null) foreach (var odb in e.OldItems.OfType<OrderElement>()) odb.OrderOn.Parent = null;
                if (e.NewItems != null) foreach (var odb in e.NewItems.OfType<OrderElement>()) odb.OrderOn.Parent = this;
            };
            GroupBy = new ObservableCollection<DMLNode>();
            ((ObservableCollection<DMLNode>)GroupBy).CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null) foreach (var gb in e.OldItems.OfType<DMLNode>()) gb.Parent = null;
                if (e.NewItems != null) foreach (var gb in e.NewItems.OfType<DMLNode>()) gb.Parent = this;
            };
            UpdateOrInsert = new ObservableCollection<UpdateOrInsertInfo>();
            ((ObservableCollection<UpdateOrInsertInfo>)UpdateOrInsert).CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null) foreach (var ui in e.OldItems.OfType<UpdateOrInsertInfo>()) ui.Value.Parent = null;
                if (e.NewItems != null) foreach (var ui in e.NewItems.OfType<UpdateOrInsertInfo>()) ui.Value.Parent = this;
            };

            RequestType = RequestType.Select;
        }

        public Request(Request parentRequest)
            : this()
        {
            _parentRequest = parentRequest;
        }

        public string IdColumnName { get; set; }

        public IList<SelectInfo> Select { get; private set; }

        public ICollection<UpdateOrInsertInfo> UpdateOrInsert { get; private set; }

        public AliasDefinition FromAlias
        {
            get { return _fromAlias; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                
                _fromAlias = value;
                value.Parent = this;
            }
        }

        public Request ParentRequest
        {
            get
            {
                return _parentRequest;
            }
        }

        public ICollection<Join> Joins { get; private set; }

        public DMLNode Where
        {
            get { return _where; }
            set
            {
                _where = value;
                if (value != null) value.Parent = this;
            }
        }

        public int Skip { get; set; }

        public int Take { get; set; }

        public bool HasDistinct { get; set; }

        public RequestType RequestType { get; set; }

        public Type ProjectionType { get; set; }

        public ICollection<OrderElement> Orders { get; private set; }

        public IList<DMLNode> GroupBy { get; private set; }

        public DMLNode Having
        {
            get { return _having; }
            set
            {
                _having = value;
                if (value != null) value.Parent = this;
            }
        }

        internal Request TopParentRequest
        {
            get
            {
                if (_parentRequest == null) return this;
                return _parentRequest.TopParentRequest;
            }
        }

        public AliasDefinition GetAliasFor(object o)
        {
            var alias = new[] { FromAlias }.Union(Joins.Select(j => j.Alias)).FirstOrDefault(a => a.Equivalents.Contains(o));
            if (alias == null && _parentRequest != null) alias = _parentRequest.GetAliasFor(o);
            return alias;
        }

        public Join GetJoinForAlias(AliasDefinition alias)
        {
            var join = Joins.FirstOrDefault(j => j.Alias == alias);
            if (join == null && _parentRequest != null) join = _parentRequest.GetJoinForAlias(alias);
            return join;
        }

        public override void Accept(IDMLAstVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            var oldRequest = visitor.CurrentRequest ?? this;
            visitor.CurrentRequest = this;

            FromAlias.Accept(visitor);
            foreach (var join in Joins) join.Accept(visitor);

            foreach (var order in Orders.Reverse()) order.Accept(visitor);

            Having?.Accept(visitor);
            foreach (var group in GroupBy.Reverse()) group.Accept(visitor);

            Where?.Accept(visitor);

            foreach (var updateOrInsert in UpdateOrInsert.Reverse()) updateOrInsert.Accept(visitor);
            foreach (var select in Select.Reverse()) select.Accept(visitor);

            visitor.Visit(this);

            visitor.CurrentRequest = oldRequest;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("{ request skip " + Skip + " take " + Take);

            if(Select.Any())
            {
                sb.Append(" select");
                foreach (var select in Select) sb.Append(" ").Append(select.ToString());
            }

            foreach (var join in Joins) sb.Append(" ").Append(join.ToString());
            if (Where != null) sb.Append(" where ").Append(Where.ToString());

            if (GroupBy.Any())
            {
                sb.Append(" group by");
                foreach (var gb in GroupBy) sb.Append(" ").Append(gb.ToString());
            }

            if (Orders.Any())
            {
                sb.Append(" order ");
                foreach (var oe in Orders) sb.Append(oe.OrderOn.ToString()).Append(oe.Ascending ? " asc" : " desc");
            }

            if (Having != null) sb.Append(" having ").Append(Having.ToString());

            return sb.ToString();
        }

        internal override DMLNode Clone(AliasDefinition source, AliasDefinition replace)
        {
            throw new NotImplementedException();
        }

        internal Request ExtractJoinToSubRequest(AliasDefinition alias)
        {
            var join = GetJoinForAlias(alias);
            if (join == null) throw new SQLException("No join found for " + alias.ToString());

            if (--join.Use == 0) Joins.Remove(join);

            var subRequest = new Request(this);
            subRequest.FromAlias = join.Use == 0 ? join.Alias : (AliasDefinition)join.Alias.Clone(null, null);
            subRequest.Where = join.Use == 0 ? join.Condition : join.Condition.Clone(join.Alias, subRequest.FromAlias);
            return subRequest;
        }
    }

    public enum RequestType
    {
        Select,
        InsertValues,
        InsertSelect,
        Update,
        Delete
    }
}
