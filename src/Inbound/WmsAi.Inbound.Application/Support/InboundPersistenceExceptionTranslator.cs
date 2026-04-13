using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace WmsAi.Inbound.Application.Support;

internal static class InboundPersistenceExceptionTranslator
{
    public static Exception Translate(Exception exception, string conflictMessage)
    {
        return exception is DbUpdateException dbUpdateException && IsUniqueConstraintViolation(dbUpdateException)
            ? new InboundConflictException(conflictMessage)
            : exception;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqliteException sqliteException)
        {
            return sqliteException.SqliteErrorCode == 19;
        }

        return exception.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
    }
}
