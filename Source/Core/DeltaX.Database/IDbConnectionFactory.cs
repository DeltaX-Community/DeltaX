using System.Data;

namespace DeltaX.Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection Connect(string connectionString);
        IDbConnection GetConnection();
    }
}