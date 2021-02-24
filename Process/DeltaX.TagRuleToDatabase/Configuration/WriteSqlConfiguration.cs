using System.Data;

public class WriteSqlConfiguration
{
    public string ConnectionFactory { get; set; }
    public CommandType CommandType { get; set; } = CommandType.Text;
    public string[] ReadTags { get; set; } 
    public string[] FormatSql { get; set; }     
}
