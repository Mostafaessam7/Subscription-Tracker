using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>
/// Creates the QRTZ_* tables Quartz's persistent AdoJobStore needs (see DependencyInjection.AddBackgroundJobs),
/// if they don't already exist. Quartz doesn't create its own schema - unlike EF Core migrations, there's no
/// framework-level "apply on startup" story for it, so this mirrors that same startup step by hand: check for
/// QRTZ_JOB_DETAILS, and if it's missing, run the embedded official schema script
/// (BackgroundJobs/QuartzSchema/tables_sqlServer.sql) batch-by-batch (ADO.NET has no notion of the "GO" batch
/// separator T-SQL tooling understands, so the script is split on it manually).
/// </summary>
public static partial class QuartzSchemaInitializer
{
    private const string ResourceName = "SubscriptionTracker.Infrastructure.BackgroundJobs.QuartzSchema.tables_sqlServer.sql";

    public static async Task EnsureSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (await SchemaExistsAsync(connection, cancellationToken))
        {
            return;
        }

        var script = await ReadEmbeddedScriptAsync(cancellationToken);
        foreach (var batch in GoSeparator().Split(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 60;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<bool> SchemaExistsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT OBJECT_ID(N'dbo.QRTZ_JOB_DETAILS', N'U')";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    private static async Task<string> ReadEmbeddedScriptAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    // Matches a line containing only "GO" (T-SQL's batch separator, not a real statement), same convention
    // every SQL Server tool splits scripts on.
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
