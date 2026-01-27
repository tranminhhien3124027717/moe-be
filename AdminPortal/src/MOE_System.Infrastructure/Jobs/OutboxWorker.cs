using MediatR;
using Microsoft.EntityFrameworkCore; // Để dùng .ToListAsync()
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Domain.Entities;
using System.Text.Json;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OutboxWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Log lỗi worker để không bị chết luồng
                Console.WriteLine($"Worker Error: {ex.Message}");
            }

            // Nghỉ 2 giây rồi quét tiếp
            await Task.Delay(2000, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken token)
    {
        // 1. Tạo Scope mới (quan trọng!)
        using var scope = _serviceProvider.CreateScope();

        // 2. Lấy UnitOfWork từ Scope này
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var outboxRepo = uow.GetRepository<OutBoxMessage>();

        // 3. Lấy tin nhắn chưa xử lý
        var messages = await outboxRepo.Entities
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(20)
            .ToListAsync(token);

        if (!messages.Any()) return;

        foreach (var msg in messages)
        {
            try
            {
                // 4. Deserialize Event
                // Dùng Type.GetType để tìm lại class CheckUserStatusEvent từ chuỗi string
                Type eventType = Type.GetType(msg.Type);

                if (eventType != null)
                {
                    var domainEvent = JsonSerializer.Deserialize(msg.Content, eventType);

                    // 5. Bắn Event đi (Handler bên dưới sẽ bắt được)
                    if (domainEvent != null)
                    {
                        await publisher.Publish(domainEvent, token);
                    }
                }

                // 6. Đánh dấu đã xong
                msg.ProcessedOn = DateTime.UtcNow;

                // Update trạng thái (Tùy Repo của bạn có cần gọi Update() ko hay EF tự track)
                // outboxRepo.Update(msg); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message {msg.Id}: {ex.Message}");
                // Có thể lưu lỗi vào cột Error của msg nếu muốn
            }
        }

        await uow.SaveAsync();
    }
}