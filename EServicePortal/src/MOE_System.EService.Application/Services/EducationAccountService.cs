using MOE_System.EService.Application.DTOs;
using MOE_System.EService.Application.DTOs.EducationAccount;
using MOE_System.EService.Application.Interfaces.Services;
using MOE_System.EService.Application.Interfaces;
using MOE_System.EService.Application.Common.Interfaces;
using MOE_System.EService.Domain.Entities;
using MOE_System.EService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MOE_System.EService.Domain.Common;
using System.Net.WebSockets;
using static MOE_System.EService.Domain.Common.BaseException;

namespace MOE_System.EService.Application.Services
{
    public class EducationAccountService : IEducationAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public EducationAccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OutstandingFeeResponse> GetOutstandingFeeAsync(string educationAccountId)
        {
            if (string.IsNullOrWhiteSpace(educationAccountId))
            {
                throw new BaseException.BadRequestException("ID must not be empty or null!");
            }

            var educationAccountRepo = _unitOfWork.GetRepository<EducationAccount>();

            var educationAccount = await educationAccountRepo.GetByIdAsync(educationAccountId);

            if (educationAccount == null)
            {
                throw new BaseException.NotFoundException("This education account is not found!");
            }

            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();

            var activeCourses = await enrollmentRepo.Entities
                .Include(c => c.EducationAccount).ThenInclude(e => e!.AccountHolder)
                .Include(c => c.Course)
                .Include(e => e.Invoices).ThenInclude(i => i.Transactions)
                .Where(e => e.EducationAccountId == educationAccountId)
                .ToListAsync();

            var allInvoices = activeCourses.SelectMany(a => a.Invoices).Where(i => i.Status == InvoiceStatus.Outstanding).ToList();


            var totalInvoicedAmount = allInvoices.Sum(a => a.Amount);

            var totalPaidAmount = allInvoices.SelectMany(i => i.Transactions)
                .Where(t => t.Status == TransactionStatus.Success)
                .Sum(t => t.Amount);

            var outstaningAmount = totalInvoicedAmount - totalPaidAmount;

            var outstandingInvoiceInfos = new List<OutstandingInvoiceInfo>();

            foreach(var invoice in allInvoices)
            {
                var invoiceResponse = new OutstandingInvoiceInfo
                {
                    InvoiceId = invoice.Id,
                    EnrollmentId = invoice.EnrollmentID,
                    CourseName = invoice.Enrollment?.Course?.CourseName ?? "",
                    Amount = invoice.Amount,
                    DueDate = invoice.DueDate!.Value,
                    Status = invoice.Status.ToString(),
                };
                outstandingInvoiceInfos.Add(invoiceResponse);
            }

            var result = new OutstandingFeeResponse
            {
                EducationAccountId = educationAccountId,
                AccountHolderId = educationAccount.AccountHolderId,
                TotalOutstandingFee = outstaningAmount,
                OutstandingInvoices = outstandingInvoiceInfos,
            };
            
            
            return result;
        }

        public async Task<TransactionDetailResponse> GetTransactionDetailAsync(string accountId, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new BaseException.BadRequestException("Account ID must not be empty or null!");
            }

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new BaseException.BadRequestException("Transaction ID must not be empty or null!");
            }

            var historyRepo = _unitOfWork.GetRepository<HistoryOfChange>();

            var transaction = await historyRepo.Entities
                .FirstOrDefaultAsync(h => h.Id == transactionId && h.EducationAccountId == accountId);

            if (transaction == null)
            {
                throw new BaseException.NotFoundException("Transaction not found!");
            }

            var result = new TransactionDetailResponse
            {
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                TransactionType = transaction.Type.ToString(),
                Description = transaction.Description ?? string.Empty,
                Date = transaction.CreatedAt.ToString("dd/MM/yyyy"),
                Time = transaction.CreatedAt.ToString("HH:mm:ss"),
                ReferenceId = transaction.ReferenceId,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter
            };

            return result;
        }
    }
}
