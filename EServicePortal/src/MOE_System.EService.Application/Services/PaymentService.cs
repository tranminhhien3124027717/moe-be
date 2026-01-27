using Microsoft.EntityFrameworkCore;
using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.Common.Interfaces;
using MOE_System.EService.Application.DTOs.Payment;
using MOE_System.EService.Application.Interfaces;
using MOE_System.EService.Application.Interfaces.Services;
using MOE_System.EService.Domain.Common;
using MOE_System.EService.Domain.Entities;
using MOE_System.EService.Domain.Enums;
using static MOE_System.EService.Domain.Common.BaseException;
using MOE_System.EService.Application.DTOs.PaymentByCreditCard.Request;
using MOE_System.EService.Application.DTOs.PaymentByCreditCard.Response;
using System.Security.Claims;

namespace MOE_System.EService.Application.Services
{
    public class PaymentService : IPaymentService
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripeService _stripeService;
        private readonly ICacheService _cacheService;

        #endregion

        #region Constructor

        public PaymentService(IUnitOfWork unitOfWork, IStripeService stripeService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _stripeService = stripeService;
            _cacheService = cacheService;
        }

        #endregion

        #region Helper Methods

        // Helper method to get account holder with education account
        private async Task<AccountHolder> GetAccountHolderWithEducationAccountAsync(string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var accountHolder = await accountHolderRepo.Entities
                .Include(a => a.EducationAccount)
                .FirstOrDefaultAsync(a => a.Id == accountHolderId);

            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("Account holder not found");
            }

            return accountHolder;
        }

        // Helper method to get invoice with validation
        private async Task<Invoice> GetInvoiceWithValidationAsync(string invoiceId, string? educationAccountId = null)
        {
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var invoice = await invoiceRepo.Entities
                .Include(i => i.Enrollment!)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new BaseException.NotFoundException("Invoice not found");
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new BaseException.BadRequestException("Invoice is already fully paid");
            }

            if (educationAccountId != null && invoice.Enrollment?.EducationAccountId != educationAccountId)
            {
                throw new BaseException.UnauthorizedException("You don't have permission to pay this invoice");
            }

            return invoice;
        }

        // Helper method to create external payment transaction with caching
        private async Task UpdateCourseStatusAsync(Enrollment enrollment, Invoice invoice)
        {
            if(enrollment == null)
            {
                throw new BaseException.NotFoundException("Enrollment not found");
            }

            if(invoice == null)
            {
                throw new BaseException.NotFoundException("Invoice not found");
            }

            var course = enrollment.Course;

            if(course == null)
            {
                throw new BaseException.NotFoundException("Course not found");
            }

            switch(course.PaymentType)
            {
                case PaymentType.OneTime:
                    enrollment.Status = PaymentStatus.FullyPaid;
                    break;
                case PaymentType.Recurring:
                    var monthCourseEnd = course.EndDate.Month;
                    var monthInvoice = invoice.BillingDate.Month;

                    if(monthCourseEnd == monthInvoice)
                    {
                        enrollment.Status = PaymentStatus.FullyPaid;
                    }
                    else
                    {
                        enrollment.Status = PaymentStatus.Paid;
                    }

                    break;
                default:
                    throw new BaseException.BadRequestException("Unknown payment type");
            }
        }      
        #endregion

        #region Public Payment Methods

        public async Task<object> CreatePaymentAsync(string accountHolderId, CreatePaymentRequest request)
        {
            // Validate flags
            if (!request.IsUseBalance && !request.IsUseExternal)
            {
                throw new BaseException.BadRequestException("At least one payment method must be selected (IsUseBalance or IsUseExternal)");
            }

            // Validate AmountFromBalance for combined payment
            if (request.IsUseBalance && request.IsUseExternal && !request.AmountFromBalance.HasValue)
            {
                throw new BaseException.BadRequestException("AmountFromBalance is required for combined payment (IsUseBalance + IsUseExternal)");
            }

            // Case 1: Only use balance (full amount)
            if (request.IsUseBalance && !request.IsUseExternal)
            {
                return await CreateBalancePaymentAsync(accountHolderId, request.InvoiceId);
            }

            // Case 2: Only use external payment (full amount)
            if (!request.IsUseBalance && request.IsUseExternal)
            {
                if (!request.PaymentMethod.HasValue)
                {
                    throw new BaseException.BadRequestException("PaymentMethod is required when IsUseExternal is true");
                }
                
                return await CreateExternalPaymentAsync(accountHolderId, request.InvoiceId, request.PaymentMethod.Value);
            }

            // Case 3: Combined payment (balance + external)
            if (request.IsUseBalance && request.IsUseExternal)
            {
                if (!request.PaymentMethod.HasValue)
                {
                    throw new BaseException.BadRequestException("PaymentMethod is required for combined payment");
                }
                
                return await CreateCombinedPaymentAsync(accountHolderId, request);
            }

            throw new BaseException.BadRequestException("Invalid payment configuration");
        }

        public async Task<object> CreateBalancePaymentAsync(string accountHolderId, string invoiceId)
        {
            var accountHolder = await GetAccountHolderWithEducationAccountAsync(accountHolderId);
            var invoice = await GetInvoiceWithValidationAsync(invoiceId, accountHolder.EducationAccount?.Id);

            // Check residential status - only Singapore Citizens can use account balance
            if (accountHolder.ResidentialStatus != ResidentialStatus.SingaporeCitizen.ToString())
            {
                throw new BaseException.UnauthorizedException("Account balance payment is only available for Singapore Citizens. Your residential status: " + accountHolder.ResidentialStatus);
            }

            var currentBalance = accountHolder.EducationAccount?.Balance ?? 0;
            if (currentBalance < invoice.Amount)
            {
                throw new BaseException.BadRequestException($"Insufficient balance. Required: {invoice.Amount}, Available: {currentBalance}");
            }

            // Deduct balance and create transaction
            var transaction = new Transaction
            {
                Amount = invoice.Amount,
                InvoiceId = invoice.Id,
                TransactionAt = DateTime.UtcNow,
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                ExpiresAt = null
            };

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            await transactionRepo.InsertAsync(transaction);

            // Update balance
            var balanceBefore = accountHolder.EducationAccount!.Balance;
            accountHolder.EducationAccount!.Balance -= invoice.Amount;

            if(accountHolder.EducationAccount.Balance < 0)
            {
                throw new BaseException.BadRequestException("Account balance cannot be negative after payment.");
            }

            var balanceAfter = accountHolder.EducationAccount.Balance;

            // Create HistoryOfChange record for account balance payment
            var billingCycleDisplay = GetBillingCycleDisplay(invoice);
            var historyOfChange = new HistoryOfChange
            {
                EducationAccountId = accountHolder.EducationAccount.Id,
                ReferenceId = $"{ChangeType.CoursePayment.GetKey()}-{DateTime.UtcNow.Ticks}",
                Amount = invoice.Amount,
                Type = ChangeType.CoursePayment,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                CreatedAt = DateTime.UtcNow,
                Description = $"Course Payment: {invoice.Enrollment?.Course?.CourseName} - {billingCycleDisplay}"
            };

            var historyOfChangeRepo = _unitOfWork.GetRepository<HistoryOfChange>();
            await historyOfChangeRepo.InsertAsync(historyOfChange);

            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            await accountHolderRepo.UpdateAsync(accountHolder);

            // Update invoice status
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            invoice.Status = InvoiceStatus.Paid;
            await invoiceRepo.UpdateAsync(invoice);

            await UpdateCourseStatusAsync(invoice.Enrollment!, invoice);

            await _unitOfWork.SaveAsync();

            return new ProcessPaymentResponse
            {
                InvoiceId = invoice.Id,
                Amount = transaction.Amount,
                Status = transaction.Status.ToString(),
                TransactionAt = transaction.TransactionAt ?? DateTime.UtcNow
            };
        }

        private async Task<object> CreateExternalPaymentAsync(
           string accountHolderId,
           string invoiceId,
           PaymentMethod paymentMethod,
           decimal? customAmount = null)
        {
            /* Cache check disabled temporarily
            // Check cache first
            var cachePrefix = paymentMethod == PaymentMethod.CreditDebitCard ? "card" : "bank";
            var cacheKey = $"payment:{cachePrefix}:{invoiceId}:{accountHolderId}";
            var cachedPayment = await _cacheService.GetAsync<CachedPaymentResponse>(cacheKey);

            if (cachedPayment != null && cachedPayment.ExpiresAt > DateTime.UtcNow &&
                cachedPayment.PaymentType == paymentMethod.ToString())
            {
                if (paymentMethod == PaymentMethod.CreditDebitCard)
                {
                    return new CreateCardPaymentResponse
                    {
                        InvoiceId = cachedPayment.InvoiceId,
                        TransactionId = cachedPayment.TransactionId,
                        PaymentIntentId = cachedPayment.PaymentIntentId,
                        ClientSecret = cachedPayment.ClientSecret ?? "",
                        Amount = cachedPayment.Amount,
                        Currency = cachedPayment.Currency ?? "sgd",
                        Status = cachedPayment.Status ?? "requires_payment_method",
                        ExpiresAt = cachedPayment.ExpiresAt
                    };
                }
                else
                {
                    return new CreateBankTransferPaymentResponse
                    {
                        InvoiceId = cachedPayment.InvoiceId,
                        TransactionId = cachedPayment.TransactionId,
                        PaymentIntentId = cachedPayment.PaymentIntentId,
                        Amount = cachedPayment.Amount,
                        ExpiresAt = cachedPayment.ExpiresAt,
                        QRCodeUrl = cachedPayment.QRCodeUrl ?? "",
                        QRCodeData = cachedPayment.QRCodeData ?? "",
                        HostedInstructionsUrl = cachedPayment.HostedInstructionsUrl ?? "",
                    };
                }
            }
            */

            // Validate and fetch data
            var accountHolder = await GetAccountHolderWithEducationAccountAsync(accountHolderId);
            var invoice = await GetInvoiceWithValidationAsync(invoiceId, accountHolder.EducationAccount?.Id);

            var amount = customAmount ?? invoice.Amount;
            var expiresAt = paymentMethod == PaymentMethod.CreditDebitCard
                ? DateTime.UtcNow.AddMinutes(30)
                : DateTime.UtcNow.AddHours(24);

            // Create transaction
            var transaction = new Transaction
            {
                Amount = amount,
                InvoiceId = invoice.Id,
                TransactionAt = null,
                PaymentMethod = paymentMethod,
                Status = TransactionStatus.Hold,
                ExpiresAt = expiresAt
            };

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            await transactionRepo.InsertAsync(transaction);
            await _unitOfWork.SaveAsync();

            var stripeRequest = new StripePaymentRequest
            {
                Amount = amount,
                Metadata = new StripePaymentMetadata
                {
                    TransactionId = transaction.Id,
                    InvoiceNumber = invoice.Id,
                    Email = accountHolder.Email,
                    EducationAccountId = accountHolder.EducationAccount?.Id ?? string.Empty
                }
            };

            // Create Stripe payment and cache result
            if (paymentMethod == PaymentMethod.CreditDebitCard)
            {
                var stripeResult = await _stripeService.CreateCardPaymentIntentAsync(stripeRequest);
                transaction.PaymentIntentId = stripeResult.PaymentIntentId;
                await transactionRepo.UpdateAsync(transaction);
                await _unitOfWork.SaveAsync();

                var response = new CreateCardPaymentResponse
                {
                    InvoiceId = invoice.Id,
                    TransactionId = transaction.Id,
                    PaymentIntentId = stripeResult.PaymentIntentId,
                    ClientSecret = stripeResult.ClientSecret,
                    Amount = amount,
                    Currency = stripeResult.Currency,
                    Status = stripeResult.Status,
                    ExpiresAt = expiresAt
                };

                // Cache disabled temporarily
                /*
                var cacheData = new CachedPaymentResponse
                {
                    TransactionId = transaction.Id,
                    PaymentIntentId = stripeResult.PaymentIntentId,
                    InvoiceId = invoice.Id,
                    Amount = amount,
                    ExpiresAt = expiresAt,
                    PaymentType = PaymentMethod.CreditDebitCard.ToString(),
                    ClientSecret = stripeResult.ClientSecret,
                    Currency = stripeResult.Currency,
                    Status = stripeResult.Status
                };
                await _cacheService.SetAsync(cacheKey, cacheData, TimeSpan.FromMinutes(30));
                */

                return response;
            }
            else // BankTransfer
            {
                var stripeResult = await _stripeService.CreatePayNowPaymentIntentAsync(stripeRequest);
                transaction.PaymentIntentId = stripeResult.PaymentIntentId;
                await transactionRepo.UpdateAsync(transaction);
                await _unitOfWork.SaveAsync();


                var response = new CreateBankTransferPaymentResponse
                {
                    InvoiceId = invoice.Id,
                    TransactionId = transaction.Id,
                    PaymentIntentId = stripeResult.PaymentIntentId,
                    Amount = amount,
                    ExpiresAt = expiresAt,
                    QRCodeUrl = stripeResult.QRCodeUrl,
                    QRCodeData = stripeResult.QRCodeData,
                    HostedInstructionsUrl = stripeResult.HostedInstructionsUrl
                };

                // Cache disabled temporarily
                /*
                var cacheData = new CachedPaymentResponse
                {
                    TransactionId = transaction.Id,
                    PaymentIntentId = stripeResult.PaymentIntentId,
                    InvoiceId = invoice.Id,
                    Amount = amount,
                    ExpiresAt = expiresAt,
                    PaymentType = PaymentMethod.BankTransfer.ToString(),
                    QRCodeData = stripeResult.QRCodeData,
                    QRCodeUrl = stripeResult.QRCodeUrl,
                    HostedInstructionsUrl = stripeResult.HostedInstructionsUrl,
                };
                await _cacheService.SetAsync(cacheKey, cacheData, TimeSpan.FromHours(24));
                */

                return response;
            }
        }

        public async Task<object> CreateCombinedPaymentAsync(string accountHolderId, CreatePaymentRequest request)
        {
            var accountHolder = await GetAccountHolderWithEducationAccountAsync(accountHolderId);
            var invoice = await GetInvoiceWithValidationAsync(request.InvoiceId, accountHolder.EducationAccount?.Id);

            // Check residential status - only Singapore Citizens can use account balance
            if (accountHolder.ResidentialStatus != ResidentialStatus.SingaporeCitizen.ToString())
            {
                throw new BaseException.UnauthorizedException("Account balance payment is only available for Singapore Citizens. Your residential status: " + accountHolder.ResidentialStatus);
            }

            var currentBalance = accountHolder.EducationAccount?.Balance ?? 0;
            var amountFromBalance = request.AmountFromBalance!.Value;

            // Validate amount
            if (amountFromBalance <= 0)
            {
                throw new BaseException.BadRequestException("AmountFromBalance must be greater than 0");
            }

            if (amountFromBalance >= invoice.Amount)
            {
                throw new BaseException.BadRequestException($"AmountFromBalance ({amountFromBalance}) must be less than invoice amount ({invoice.Amount}). Use balance-only payment instead.");
            }

            if (currentBalance < amountFromBalance)
            {
                throw new BaseException.BadRequestException($"Insufficient balance. Required: {amountFromBalance}, Available: {currentBalance}");
            }

            var amountFromExternal = invoice.Amount - amountFromBalance;

            // Create balance transaction (Hold - will be processed after external payment success)
            var balanceTransaction = new Transaction
            {
                Amount = amountFromBalance,
                InvoiceId = invoice.Id,
                TransactionAt = null,
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Hold,
                ExpiresAt = null
            };

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            await transactionRepo.InsertAsync(balanceTransaction);
            await _unitOfWork.SaveAsync();

            // Create external payment with custom amount
            var externalResponse = await CreateExternalPaymentAsync(
                accountHolderId,
                request.InvoiceId,
                request.PaymentMethod!.Value,
                amountFromExternal);

            var cacheDuration = request.PaymentMethod.Value == PaymentMethod.CreditDebitCard
                ? TimeSpan.FromMinutes(30)
                : TimeSpan.FromHours(24);

            if (request.PaymentMethod!.Value == PaymentMethod.CreditDebitCard)
            {
                var cardResponse = (CreateCardPaymentResponse)externalResponse;

                // Cache combined payment info - disabled temporarily
                /*
                var cacheKey = $"payment:combined:{cardResponse.TransactionId}";
                var combinedInfo = new CombinedPaymentCacheData
                {
                    BalanceTransactionId = balanceTransaction.Id,
                    ExternalTransactionId = cardResponse.TransactionId,
                    AmountFromBalance = amountFromBalance,
                    InvoiceId = invoice.Id,
                    AccountHolderId = accountHolderId
                };
                await _cacheService.SetAsync(cacheKey, combinedInfo, cacheDuration);
                */

                return new CreateCombinedPaymentResponse
                {
                    InvoiceId = invoice.Id,
                    BalanceTransactionId = balanceTransaction.Id,
                    AmountFromBalance = amountFromBalance,
                    BalanceAfter = currentBalance - amountFromBalance,
                    ExternalTransactionId = cardResponse.TransactionId,
                    AmountFromExternal = amountFromExternal,
                    PaymentMethod = PaymentMethod.Combined.ToString(),
                    PaymentIntentId = cardResponse.PaymentIntentId,
                    ClientSecret = cardResponse.ClientSecret,
                    Currency = cardResponse.Currency,
                    ExpiresAt = cardResponse.ExpiresAt,
                    Status = TransactionStatus.Hold.ToString()
                };
            }
            else // BankTransfer
            {
                var bankResponse = (CreateBankTransferPaymentResponse)externalResponse;

                // Cache combined payment info - disabled temporarily
                /*
                var cacheKey = $"payment:combined:{bankResponse.TransactionId}";
                var combinedInfo = new CombinedPaymentCacheData
                {
                    BalanceTransactionId = balanceTransaction.Id,
                    ExternalTransactionId = bankResponse.TransactionId,
                    AmountFromBalance = amountFromBalance,
                    InvoiceId = invoice.Id,
                    AccountHolderId = accountHolderId
                };
                await _cacheService.SetAsync(cacheKey, combinedInfo, cacheDuration);
                */

                return new CreateCombinedPaymentResponse
                {
                    InvoiceId = invoice.Id,
                    BalanceTransactionId = balanceTransaction.Id,
                    AmountFromBalance = amountFromBalance,
                    BalanceAfter = currentBalance - amountFromBalance,
                    ExternalTransactionId = bankResponse.TransactionId,
                    AmountFromExternal = amountFromExternal,
                    PaymentMethod = PaymentMethod.BankTransfer.ToString(),
                    PaymentIntentId = bankResponse.PaymentIntentId,
                    QRCodeUrl = bankResponse.QRCodeUrl,
                    QRCodeData = bankResponse.QRCodeData,
                    HostedInstructionsUrl = bankResponse.HostedInstructionsUrl,
                    ExpiresAt = bankResponse.ExpiresAt,
                    Status = TransactionStatus.Hold.ToString()
                };
            }
        }

        public async Task<TransactionResponse> GetTransactionByIdAsync(string transactionId)
        {
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var transaction = await transactionRepo.Entities
                .Include(t => t.Invoice!)
                    .ThenInclude(i => i.Enrollment!)
                        .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new BaseException.NotFoundException("Transaction not found");
            }

            return new TransactionResponse
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                InvoiceId = transaction.InvoiceId,
                CreatedAt = transaction.CreatedAt,
                TransactionAt = transaction.TransactionAt,
                PaymentMethod = transaction.PaymentMethod.ToString(),
                Status = transaction.Status.ToString(),
                InvoiceNumber = transaction.Invoice?.Id,
                CourseName = transaction.Invoice?.Enrollment?.Course?.CourseName
            };
        }


        public async Task<PaginatedList<TransactionResponse>> GetTransactionHistoryAsync(string accountHolderId, int pageIndex, int pageSize)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var accountHolder = await accountHolderRepo.Entities
                .Include(a => a.EducationAccount)
                .FirstOrDefaultAsync(a => a.Id == accountHolderId);

            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("Account holder not found");
            }

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var query = transactionRepo.Entities
                .Include(t => t.Invoice!)
                    .ThenInclude(i => i.Enrollment!)
                        .ThenInclude(e => e.Course)
                .Where(t => t.Invoice!.Enrollment!.EducationAccountId == accountHolder.EducationAccount!.Id)
                .OrderByDescending(t => t.TransactionAt);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    InvoiceId = t.InvoiceId,
                    CreatedAt = t.CreatedAt,
                    TransactionAt = t.TransactionAt,
                    PaymentMethod = t.PaymentMethod.ToString(),
                    Status = t.Status.ToString(),
                    InvoiceNumber = t.Invoice!.Id,
                    CourseName = t.Invoice!.Enrollment!.Course!.CourseName
                })
                .ToListAsync();

            return new PaginatedList<TransactionResponse>(transactions, totalCount, pageIndex, pageSize);
        }

        public async Task<PaymentSummaryResponse> GetPaymentSummaryAsync(string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var accountHolder = await accountHolderRepo.Entities
                .Include(a => a.EducationAccount)
                .FirstOrDefaultAsync(a => a.Id == accountHolderId);

            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("Account holder not found");
            }

            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();

            var invoices = await invoiceRepo.Entities
                .Include(i => i.Enrollment)
                .Where(i => i.Enrollment!.EducationAccountId == accountHolder.EducationAccount!.Id)
                .ToListAsync();

            var transactions = await transactionRepo.Entities
                .Include(t => t.Invoice!)
                    .ThenInclude(i => i.Enrollment)
                .Where(t => t.Invoice!.Enrollment!.EducationAccountId == accountHolder.EducationAccount!.Id)
                .ToListAsync();

            return new PaymentSummaryResponse
            {
                TotalPaid = transactions.Where(t => t.Status == TransactionStatus.Success).Sum(t => t.Amount),
                TotalOutstanding = invoices.Where(i => i.Status == InvoiceStatus.Outstanding).Sum(i => i.Amount),
                TotalTransactions = transactions.Count(t => t.Status == TransactionStatus.Success),
                OutstandingInvoices = invoices.Count(i => i.Status == InvoiceStatus.Outstanding)
            };
        }

        public async Task<InvoiceDetailsResponse> GetInvoiceDetails(string invoiceId, string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await accountHolderRepo.Entities
                .Include(a => a.EducationAccount)
                .FirstOrDefaultAsync(a => a.Id == accountHolderId);
            
            var educationAccountId = accountHolder?.EducationAccount?.Id;

            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var invoice = await invoiceRepo.Entities
                .Include(i => i.Enrollment!)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new BaseException.NotFoundException("Invoice not found");
            }

            if (educationAccountId != null && invoice.Enrollment?.EducationAccountId != educationAccountId)
            {
                throw new BaseException.UnauthorizedException("You don't have permission to pay this invoice");
            }

            return new InvoiceDetailsResponse
            {
                InvoiceId = invoice.Id,
                CourseName = invoice.Enrollment!.Course!.CourseName,
                Amount = invoice.Amount,
                Balance = accountHolder?.EducationAccount?.Balance ?? 0,
            };
        }

        public async Task<int> CheckInvoiceStatus(string invoiceId)
        {
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var invoice = await invoiceRepo.Entities
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null)
            {
                throw new BaseException.NotFoundException("Invoice not found");
            }
            return invoice.Status == InvoiceStatus.Paid ? 1 : 0;
        }
        #endregion

        #region Internal Payment Processing

        public async Task<int> TimeoutExpiredPaymentsAsync()
        {
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            
            var expiredTransactions = await transactionRepo.Entities
                .Where(t => t.Status == TransactionStatus.Hold 
                    && t.ExpiresAt.HasValue 
                    && t.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            if (!expiredTransactions.Any())
            {
                return 0;
            }

            foreach (var transaction in expiredTransactions)
            {
                transaction.Status = TransactionStatus.Timeout;
                transactionRepo.Update(transaction);
            }

            await _unitOfWork.SaveAsync();

            return expiredTransactions.Count;
        }

        public async Task<ProcessPaymentResponse> ProcessPaymentInternalAsync(ProcessPaymentRequest request)
        {
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var historyOfChangeRepo = _unitOfWork.GetRepository<HistoryOfChange>();
            var educationAccountRepo = _unitOfWork.GetRepository<EducationAccount>();

            var currentTransaction = await transactionRepo.Entities
                .Include(t => t.Invoice)
                    .ThenInclude(i => i!.Enrollment)
                        .ThenInclude(e => e!.Course)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i!.Enrollment)
                        .ThenInclude(e => e!.EducationAccount)
                .FirstOrDefaultAsync(t => t.Id == request.TransactionId);

            if (currentTransaction == null) throw new BaseException.NotFoundException("Transaction not found");
            
            if (currentTransaction.Status == TransactionStatus.Success)
            {
                return new ProcessPaymentResponse { 
                    InvoiceId = currentTransaction.InvoiceId, 
                    Status = "Success", 
                    Amount = currentTransaction.Amount 
                };
            }

            if (currentTransaction.Status != TransactionStatus.Hold)
                throw new BaseException.BadRequestException($"Transaction is {currentTransaction.Status}");

            var invoice = currentTransaction.Invoice;
            if (invoice == null || invoice.Status == InvoiceStatus.Paid)
                throw new BaseException.BadRequestException("Invoice is invalid or already paid");

            var balanceTransaction = await transactionRepo.Entities
                .FirstOrDefaultAsync(t => t.InvoiceId == currentTransaction.InvoiceId 
                                     && t.PaymentMethod == PaymentMethod.AccountBalance 
                                     && t.Status == TransactionStatus.Hold);

            decimal totalProcessedAmount = currentTransaction.Amount;

            if (balanceTransaction != null)
            {
                var educationAccount = invoice.Enrollment?.EducationAccount;
                if (educationAccount == null) throw new BaseException.NotFoundException("Education account not found");

                // Kiểm tra số dư thực tế
                if (educationAccount.Balance < balanceTransaction.Amount)
                    throw new BaseException.BadRequestException("Insufficient balance in education account");

                // Khấu trừ số dư
                var balanceBefore = educationAccount.Balance;
                educationAccount.Balance -= balanceTransaction.Amount;
                totalProcessedAmount += balanceTransaction.Amount;

                // Lưu lịch sử biến động số dư
                var billingCycleDisplay = GetBillingCycleDisplay(invoice);
                await historyOfChangeRepo.InsertAsync(new HistoryOfChange
                {
                    EducationAccountId = educationAccount.Id,
                    ReferenceId = $"{ChangeType.CoursePayment.GetKey()}-{DateTime.UtcNow.Ticks}",
                    Amount = balanceTransaction.Amount,
                    Type = ChangeType.CoursePayment,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = educationAccount.Balance,
                    CreatedAt = DateTime.UtcNow,
                    Description = $"Course Payment: {invoice.Enrollment?.Course?.CourseName} - {billingCycleDisplay}"
                });

                // Cập nhật trạng thái Balance Transaction
                balanceTransaction.Status = TransactionStatus.Success;
                balanceTransaction.TransactionAt = DateTime.UtcNow;
                await transactionRepo.UpdateAsync(balanceTransaction);
                await educationAccountRepo.UpdateAsync(educationAccount);
            }

            currentTransaction.Status = TransactionStatus.Success;
            currentTransaction.TransactionAt = DateTime.UtcNow;
            await transactionRepo.UpdateAsync(currentTransaction);

            invoice.Status = InvoiceStatus.Paid;
            await _unitOfWork.GetRepository<Invoice>().UpdateAsync(invoice);

            var enrollment = invoice.Enrollment;

            await UpdateCourseStatusAsync(enrollment!, invoice);

            await _unitOfWork.SaveAsync();

            // Cache cleanup disabled temporarily
            // await ClearPaymentRelatedCache(invoice.Id, invoice.Enrollment?.EducationAccount?.AccountHolder?.Id);

            return new ProcessPaymentResponse
            {
                InvoiceId = invoice.Id,
                Amount = totalProcessedAmount,
                Status = TransactionStatus.Success.ToString(),
                TransactionAt = currentTransaction.TransactionAt.Value
            };
        }

        private Task ClearPaymentRelatedCache(string invoiceId, string? accountHolderId)
        {
            // Cache operations disabled temporarily. Method left as no-op.
            return Task.CompletedTask;
        }

        public async Task<bool> CancelPaymentInternalAsync(string transactionId)
        {
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var transaction = await transactionRepo.Entities
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new BaseException.NotFoundException("Transaction not found");
            }

            if (transaction.Status != TransactionStatus.Hold)
            {
                throw new BaseException.BadRequestException($"Cannot cancel transaction with status {transaction.Status}");
            }

            transaction.Status = TransactionStatus.Cancel;
            transactionRepo.Update(transaction);

            await _unitOfWork.SaveAsync();

            return true;
        }
        public async Task<PaymentByCreditCardResponse> PayByCreditCardAsync(Guid educationAccountId, PaymentByCreditCardRequest request, CancellationToken cancellationToken)
        {
            if (request.Amount <= 0)
            {
                throw new BaseException.BadRequestException("Amount must be greater than zero.");
            }
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var invoice = await invoiceRepo.Entities
                .Include(i => i.Enrollment)
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                throw new BaseException.NotFoundException($"Invoice {request.InvoiceId} not found.");
            }
            
            if (invoice.Enrollment != null && invoice.Enrollment.EducationAccountId != educationAccountId.ToString())
            {
                throw new BaseException.UnauthorizedException("This invoice does not belong to the current education account.");
            }
            
            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new BaseException.BadRequestException("Invoice is already fully paid.");
            }
            
            var transactionId = Guid.NewGuid();
            var lastFourDigits = request.CardNumber.Length >= 4 
                ? request.CardNumber.Substring(request.CardNumber.Length - 4) 
                : request.CardNumber;
            
            var transaction = new Transaction
            {
                Id = transactionId.ToString(),
                Amount = request.Amount,
                InvoiceId = invoice.Id,
                TransactionAt = DateTime.UtcNow,
                PaymentMethod = PaymentMethod.CreditDebitCard,
                Status = TransactionStatus.Success
            };
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            await transactionRepo.InsertAsync(transaction);
        
            var totalPaidBefore = invoice.Transactions
                .Where(t => t.Status == TransactionStatus.Success)
                .Sum(t => t.Amount);
                
            var totalPaidPayload = totalPaidBefore + request.Amount;
            if (totalPaidPayload >= invoice.Amount)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else 
            {
                invoice.Status = InvoiceStatus.Paid; 
            }
            await _unitOfWork.SaveAsync();
            
            return new PaymentByCreditCardResponse
            {
                TransactionId = transactionId.ToString(),
                InvoiceId = request.InvoiceId,
                AmountPaid = request.Amount,
                Status = "Success",
                PaymentDate = transaction.TransactionAt ?? DateTime.UtcNow,
                Reference = "REF-" + transactionId.ToString().Substring(0, 8).ToUpper(),
                LastFourDigits = lastFourDigits,
                Message = "Payment processed successfully."
            };
        }
        
        private string GetBillingCycleDisplay(Invoice invoice)
        {
            // Return billing period as "Month Year" format
            if (invoice.BillingDate != default)
            {
                return invoice.BillingDate.ToString("MMMM yyyy");
            }

            return "One Time";
        }
        #endregion

    }
}

