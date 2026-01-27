using MOE_System.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.DTOs.Invoice.Response
{
    public class PrintInvoiceResponse
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string EnrollmentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime DueDate { get; set; }
    }
}
