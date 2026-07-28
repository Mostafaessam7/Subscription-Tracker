using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Subscriptions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Reports.ExportSubscriptionsExcel;

public sealed class ExportSubscriptionsExcelQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<ExportSubscriptionsExcelQuery, ReportFileDto>
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<ReportFileDto>> Handle(ExportSubscriptionsExcelQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Subscriptions.Where(s => s.WorkspaceId == currentUserService.WorkspaceId);
        query = SubscriptionFilters.Apply(query, request.SearchTerm, request.CategoryId, request.TagId, request.Status);

        var subscriptions = await query
            .OrderBy(s => s.Name)
            .Select(SubscriptionProjections.ToDto)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Subscriptions");

        string[] header =
        [
            "Name", "Provider", "Category", "Amount", "Currency", "Billing Frequency", "Status",
            "Start Date", "Next Renewal Date", "Auto Renewal",
        ];

        for (var i = 0; i < header.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = header[i];
        }

        worksheet.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var subscription in subscriptions)
        {
            worksheet.Cell(row, 1).Value = subscription.Name;
            worksheet.Cell(row, 2).Value = subscription.Provider;
            worksheet.Cell(row, 3).Value = subscription.CategoryId?.ToString() ?? string.Empty;
            worksheet.Cell(row, 4).Value = subscription.Amount;
            worksheet.Cell(row, 5).Value = subscription.CurrencyCode;
            worksheet.Cell(row, 6).Value = subscription.BillingFrequency.ToString();
            worksheet.Cell(row, 7).Value = subscription.Status.ToString();
            worksheet.Cell(row, 8).Value = subscription.StartDate.ToDateTime(TimeOnly.MinValue);
            worksheet.Cell(row, 8).Style.DateFormat.Format = "yyyy-mm-dd";

            if (subscription.NextRenewalDate is not null)
            {
                worksheet.Cell(row, 9).Value = subscription.NextRenewalDate.Value.ToDateTime(TimeOnly.MinValue);
                worksheet.Cell(row, 9).Style.DateFormat.Format = "yyyy-mm-dd";
            }

            worksheet.Cell(row, 10).Value = subscription.AutoRenewal ? "Yes" : "No";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"subscriptions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";

        return Result.Success(new ReportFileDto(fileName, ExcelContentType, stream.ToArray()));
    }
}
