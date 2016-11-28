using F23Bag.Data.DML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    public interface IExpresstionToSqlAst
    {
        bool Accept(Expression expression);

        DMLNode Convert(Expression expression, Request request, ISQLMapping sqlMapping);
    }
}
