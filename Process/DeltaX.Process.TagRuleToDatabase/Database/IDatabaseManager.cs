using System.Collections.Generic;
using System.Data;

public interface IDatabaseManager
{
    int SaveToDb(string connectionFactory, string commandText, CommandType commandType, Dictionary<string, object> parameters);
}