namespace DeltaX.Process.RealTimeHistoricDB.Repositories
{
    public class QueriesSqlite
    {
        public const string sqlInsertHistoricTagValue = @"
INSERT INTO HistoricTagValue(TagId, Updated, Value) 
VALUES (@TagId, @Updated, @Value) ";

        public const string sqlInsertHistoricTag = @"
INSERT INTO HistoricTag (TagName, Enable) 
VALUES(@TagName, @Enable) ";

        public const string sqlSelectHistoricTagValue = @" 
SELECT Updated, Value 
FROM (
    SELECT tv.Updated, tv.Value 
    FROM HistoricTagValue tv 
    WHERE tv.TagId = @tagId 
    AND tv.Updated BETWEEN (@beginDateTime - 86400) AND @beginDateTime
    ORDER BY tv.Updated DESC LIMIT 1) t
UNION 
SELECT tv.Updated, tv.Value 
FROM HistoricTagValue tv 
WHERE tv.tagid=@tagId 
AND tv.Updated BETWEEN @beginDateTime AND @endDateTime
ORDER BY tv.Updated ASC ";

        public const string sqlSelectHistoricTagByName = @"
SELECT 
    Id, 
    TagName, 
    CreatedAt,
    Enable
FROM HistoricTag
WHERE TagName = @tagName;";

        public const string sqlSelectHistoricTag = @"
SELECT 
    id as Id, 
    TagName, 
    CreatedAt,
    Enable
FROM HistoricTag;";
         
            
        public const string sqlDeleteHistoricTagValue = @" 
DELETE FROM HistoricTagValue  
WHERE tagId in (select id from HistoricTag) 
AND Updated < (strftime('%s','now') - (@daysPresistence * 24 * 60 * 60));
";

        public const string sqlCreateTables = @"
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS HistoricTag (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TagName     TEXT    UNIQUE
                        NOT NULL, 
    CreatedAt   DATE    DEFAULT (datetime('now', 'localtime') ),
    Enable      BOOLEAN DEFAULT (1)
);

CREATE TABLE IF NOT EXISTS HistoricTagValue (
    TagId   INTEGER REFERENCES HistoricTag (Id),
    Updated DOUBLE  NOT NULL,
    Value   TEXT
);

-- Create Index 
CREATE INDEX IF NOT EXISTS idxTagAndUpdate ON HistoricTagValue (TagId, Updated ASC );  

COMMIT TRANSACTION;
PRAGMA foreign_keys = on;  
";
    }
}
