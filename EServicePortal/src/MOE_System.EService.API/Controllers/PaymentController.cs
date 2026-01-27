using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.DTOs.Payment;
using MOE_System.EService.Application.Interfaces.Services;
using MOE_System.EService.API.Controllers;
using System.Security.Claims;
using Stripe;
using MOE_System.EService.Application.DTOs.PaymentByCreditCard.Request;
using MOE_System.EService.Application.DTOs.PaymentByCreditCard.Response;
using System.Threading;
using System.Threading.Tasks;

namespace MOE_System.EService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/payments")]
public class PaymentController : BaseApiController
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }


    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var response = await _paymentService.CreatePaymentAsync(accountHolderId, request);
        return Success(response, "Payment created successfully");
    }

    [AllowAnonymous]
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment()
    {
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            
        if (string.IsNullOrEmpty(stripeSignature))
        {
            return BadRequest(new { message = "This endpoint is only for Stripe webhook. Use GET /transactions/{id} to check status." });
        }
            
        return await HandleStripeWebhook(stripeSignature);
    }

    private async Task<IActionResult> HandleStripeWebhook(string stripeSignature)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["StripeSettings:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                webhookSecret
            );

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    
                if (paymentIntent != null)
                {
                    var transactionId = paymentIntent.Metadata.ContainsKey("TransactionId") 
                        ? paymentIntent.Metadata["TransactionId"] 
                        : null;
                    var invoiceNumber = paymentIntent.Metadata.ContainsKey("InvoiceNumber") 
                        ? paymentIntent.Metadata["InvoiceNumber"] 
                        : null;
                if (!string.IsNullOrEmpty(transactionId) && !string.IsNullOrEmpty(invoiceNumber))
                    {
                        var response = await _paymentService.ProcessPaymentInternalAsync(new ProcessPaymentRequest
                        {
                            TransactionId = transactionId,
                            PaymentIntentId = paymentIntent.Id,
                            InvoiceId = invoiceNumber
                        });
                            
                        return Ok(new { received = true});
                    }
                }
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    
                if (paymentIntent != null)
                {
                    var transactionId = paymentIntent.Metadata.ContainsKey("TransactionId") 
                        ? paymentIntent.Metadata["TransactionId"] 
                        : null;

                    if (!string.IsNullOrEmpty(transactionId))
                    {
                        // Cancel transaction
                        await _paymentService.CancelPaymentInternalAsync(transactionId);
                    }
                }
            }

            return Ok(new { received = true });
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("transactions/{transactionId}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> GetTransaction([FromRoute] string transactionId)
    {
        var response = await _paymentService.GetTransactionByIdAsync(transactionId);
        return Success(response, "Transaction retrieved successfully");
    }


    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<TransactionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedList<TransactionResponse>>>> GetTransactionHistory(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var response = await _paymentService.GetTransactionHistoryAsync(accountHolderId, pageIndex, pageSize);
        return Success(response, "Transaction history retrieved successfully");
    }


    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<PaymentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaymentSummaryResponse>>> GetPaymentSummary()
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var response = await _paymentService.GetPaymentSummaryAsync(accountHolderId);
        return Success(response, "Payment summary retrieved successfully");
    }

    [Authorize]
    [HttpPost("pay-by-credit-card")]
    public async Task<ActionResult<PaymentByCreditCardResponse>> PayByCreditCard(
        [FromBody] PaymentByCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        // Get educationAccountId from authenticated user claims
        var educationAccountIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EducationAccountId")?.Value;
            
        if (string.IsNullOrEmpty(educationAccountIdClaim) || !Guid.TryParse(educationAccountIdClaim, out var educationAccountId))
        {
            return Unauthorized(new { message = "Invalid or missing Education Account ID in authentication token." });
        }
            
        try
        {
            var result = await _paymentService.PayByCreditCardAsync(educationAccountId, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invoice-details")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailsResponse>>> GetInvoiceDetails([FromQuery] string invoiceId)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }
        var response = await _paymentService.GetInvoiceDetails(invoiceId, accountHolderId);
        return Success(response, "Invoice details retrieved successfully");
    }

    [AllowAnonymous]
    [HttpGet("check-invoice-status")]
    public async Task<ActionResult<ApiResponse<int>>> CheckInvoiceStatus([FromQuery] string invoiceId)
    {
        var response = await _paymentService.CheckInvoiceStatus(invoiceId);
        return Success(response, "Invoice status checked successfully");
    }
}

