using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Subscriptions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Reports.ExportSubscriptionsPdf;

public sealed class ExportSubscriptionsPdfQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<ExportSubscriptionsPdfQuery, ReportFileDto>
{
    private const string PdfContentType = "application/pdf";

    private static readonly string[] Header =
        ["Name", "Provider", "Amount", "Frequency", "Status", "Start Date", "Next Renewal", "Auto Renew"];

    /// <summary>
    /// Accepts QuestPDF's free Community license (see https://www.questpdf.com/license/) once per process. A
    /// static constructor (rather than wiring this into AddApplication) guarantees it runs before the first PDF
    /// is generated regardless of caller - production DI and unit tests that construct this handler directly
    /// both trigger it on first use of the type.
    /// </summary>
    static ExportSubscriptionsPdfQueryHandler()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Result<ReportFileDto>> Handle(ExportSubscriptionsPdfQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Subscriptions.Where(s => s.WorkspaceId == currentUserService.WorkspaceId);
        query = SubscriptionFilters.Apply(query, request.SearchTerm, request.CategoryId, request.TagId, request.Status);

        var subscriptions = await query
            .OrderBy(s => s.Name)
            .Select(SubscriptionProjections.ToDto)
            .ToListAsync(cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Text("Subscriptions Report").FontSize(18).Bold();

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1);
                    });

                    table.Header(headerRow =>
                    {
                        foreach (var title in Header)
                        {
                            headerRow.Cell().BorderBottom(1).PaddingBottom(4).Text(title).Bold();
                        }
                    });

                    foreach (var subscription in subscriptions)
                    {
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3).Text(subscription.Name);
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3).Text(subscription.Provider);
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3)
                            .Text($"{subscription.Amount:0.00} {subscription.CurrencyCode}");
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3).Text(subscription.BillingFrequency.ToString());
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3).Text(subscription.Status.ToString());
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3)
                            .Text(subscription.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3)
                            .Text(subscription.NextRenewalDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-");
                        table.Cell().BorderBottom(0.5f).PaddingVertical(3).Text(subscription.AutoRenewal ? "Yes" : "No");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ").FontSize(8);
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)).FontSize(8);
                    text.Span(" - Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });

        var bytes = document.GeneratePdf();
        var fileName = $"subscriptions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";

        return Result.Success(new ReportFileDto(fileName, PdfContentType, bytes));
    }
}
