using MOE_System.Application.Interfaces.Services;
using Quartz;

namespace MOE_System.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class AutoCreateInvoiceJob : IJob
{
    private readonly IInvoiceService _invoiceService;

    public AutoCreateInvoiceJob(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var logicalDate = DateTime.UtcNow.Date;
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate, context.CancellationToken);
    }
}