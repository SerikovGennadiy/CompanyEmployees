using Application.Notifications;
using Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Companies.Handlers
{
    public sealed class EmailHandler : INotificationHandler<CompanyDeletedNotification>
    {
        private readonly ILoggerManager _logger;

       public EmailHandler(ILoggerManager loggerManager)
        {
            _logger = loggerManager;
        }

        // Мы не будем отправлять сообщение по почте
        // Для демонстрации выполним логирование чз LoggerService!
        // чтобы обработать сообщение
        public async Task Handle(CompanyDeletedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogWarn($"Delete action for the company with id: {notification.Id} has occured.");

            await Task.CompletedTask;
        }
    }
}
