namespace DeltaX.Process.RealTimeHistoricDB.Repositories
{
    using DeltaX.Process.RealTimeHistoricDB.Records;
    using System.Collections.Generic;

    public interface IHistoricRepository
    {
        List<HistoricTagRecord> GetInsertTags(
            IEnumerable<string> tagsToAdd);

        List<HistoricTagRecord> GetListHistoricTags();

        int SaveHistoricTagValues(
            List<HistoricTagValueRecord> tagsValues);

        int CreateTables();

        List<HistoricTagValueDto> GetTagHistory(
            string tagName,
            double beginDateTime,
            double endDateTime,
            int maxPoints = 10000,
            string prevValue = null);

        int DeleteOldsHistoricTagValues(
            int daysPresistence);
    }
}
