using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DeltaX.Modules.DapperRepository
{
    public interface IDapperRepository
    { 
        IUnitOfWork UnitOfWork { get; } 
    }
}