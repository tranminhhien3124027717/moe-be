using MOE_System.Application.DTOs.Invoice.Request;
using MOE_System.Application.DTOs.Invoice.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Interfaces.Services
{
    public interface IInvoiceService
    {
        Task<PrintInvoiceResponse> GetInvoiceAsync(PrintInvoiceRequest request);
        Task GenerateInvoiceForEnrollmentAsync(DateTime logicalDate, CancellationToken cancellationToken = default);
    }
}
