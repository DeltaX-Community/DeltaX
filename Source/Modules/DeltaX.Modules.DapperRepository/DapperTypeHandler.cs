namespace DeltaX.Modules.DapperRepository
{
    using Dapper;
    using System;
    using System.Data;

    public static class DapperTypeHandler
    {
        public static void SetDapperTypeHandler()
        {
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new GuidHandler());
            SqlMapper.AddTypeHandler(new TimeSpanHandler());
        }
    }


    abstract class BaseTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        // Parameters are converted by Microsoft.Data.Sqlite
        public override void SetValue(IDbDataParameter parameter, T value)
            => parameter.Value = value;
    }

    class DateTimeOffsetHandler : BaseTypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            switch (parameter.DbType)
            {
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.AnsiString: // Seems to be some MySQL type mapping here
                    parameter.Value = value.UtcDateTime;
                    break;
                case DbType.DateTimeOffset:
                    parameter.Value = value;
                    break;
            }
            base.SetValue(parameter, value);
        }

        public override DateTimeOffset Parse(object value)
        {
            switch (value)
            {
                case DateTime time:
                    return new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc), TimeSpan.Zero);
                case DateTimeOffset dto:
                    return dto;
                case string str:
                    return DateTimeOffset.Parse(str);
                default:
                    throw new InvalidOperationException("Must be DateTime or DateTimeOffset object to be mapped.");
            }
        }
    }

    class GuidHandler : BaseTypeHandler<Guid>
    {
        public override Guid Parse(object value)
            => Guid.Parse((string)value);
    }

    class TimeSpanHandler : BaseTypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            switch (value)
            {
                case TimeSpan dto:
                    return dto;
                case string str:
                    return TimeSpan.Parse(str);
                default:
                    return TimeSpan.Parse(value.ToString());
            }
        }
    }
}
