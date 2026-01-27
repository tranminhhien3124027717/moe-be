using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.Invoice.Request;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/v1/admin/invoices")]
    public class InvoiceController : BaseApiController
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> GetInvoice([FromBody] PrintInvoiceRequest request)
        {
            var invoice = await _invoiceService.GetInvoiceAsync(request);

            return Ok(invoice);
        }

    }
}
