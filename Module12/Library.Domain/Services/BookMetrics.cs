using System.Diagnostics.Metrics;

namespace Library.Domain.Services;

public sealed class BookMetrics
{
    public const string MeterName = "BookService.Metrics";

    private readonly Counter<int> _booksCreated;

    public BookMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _booksCreated = meter.CreateCounter<int>(
            name: "books_created_total",
            unit: "{books}",
            description: "Total number of created books");
    }

    public void BookCreated() => _booksCreated.Add(1);
}