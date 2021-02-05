using DeltaX.Domain.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaX.Domain.Common.Repositories
{
    public interface IRepository 
    {
        IUnitOfWork UnitOfWork { get; } 
    }
}
