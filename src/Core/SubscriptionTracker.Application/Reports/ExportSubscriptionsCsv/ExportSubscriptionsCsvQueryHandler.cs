using System.Buffers;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Subscriptions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Reports.ExportSubscriptionsCsv;

public sealed class ExportSubscriptionsCsvQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<ExportSubscriptionsCsvQuery, ReportFileDto>
{
    private static readonly string[] Header =
    [
        "Name", "Provider", "Category", "Amount", "Currency", "Billing Frequency", "Status",
        "Start Date", "Next Renewal Date", "Auto Renewal",
    ];

    public async Task<Result<ReportFileDto>> Handle(ExportSubscriptionsCsvQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Subscriptions.Where(s => s.WorkspaceId == currentUserService.WorkspaceId);
        query = SubscriptionFilters.Apply(query, request.SearchTerm, request.CategoryId, request.TagId, request.Status);

        var subscriptions = await query
            .OrderBy(s => s.Name)
            .Select(SubscriptionProjections.ToDto)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Header));

        foreach (var subscription in subscriptions)
        {
            var fields = new[]
            {
                subscription.Name,
                subscription.Provider,
                subscription.CategoryId?.ToString() ?? string.Empty,
                subscription.Amount.ToString(CultureInfo.InvariantCulture),
                subscription.CurrencyCode,
                subscription.BillingFrequency.ToString(),
                subscription.Status.ToString(),
                subscription.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                subscription.NextRenewalDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                subscription.AutoRenewal ? "Yes" : "No",
            };

            builder.AppendLine(string.Join(',', fields.Select(EscapeCsvField)));
        }

        var content = Encoding.UTF8.GetBytes(builder.ToString());
        var fileName = $"subscriptions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return Result.Success(new ReportFileDto(fileName, "text/csv", content));
    }

    // Characters that make a spreadsheet app (Excel/Sheets/LibreOffice) treat a CSV cell as a formula rather
    // than literal text on open - CSV Injection / "Formula Injection" (OWASP, CWE-1236). Name/Provider are
    // free-text user input (see CreateSubscriptionCommandValidator), so a subscription named e.g.
    // "=HYPERLINK(...)" would otherwise execute as a formula for whoever opens the exported file.
    private static readonly SearchValues<char> FormulaInjectionPrefixes = SearchValues.Create(['=', '+', '-', '@', '\t', '\r']);

    private static string EscapeCsvField(string field)
    {
        if (field.Length == 0)
        {
            return field;
        }

        // Per OWASP's mitigation: prefix with a single quote so spreadsheet apps render it as text. It stays
        // visible in the raw CSV (unlike Excel's own UI convention of hiding a leading ' on manual entry), but
        // that's an acceptable, clearly-flagged trade-off for not silently executing arbitrary formulas.
        if (field.IndexOfAny(FormulaInjectionPrefixes) == 0)
        {
            field = "'" + field;
        }

        return field.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }
}
