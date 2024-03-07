using MediatR;

namespace Application.Notifications
{
    // INotification is IRequest equivalent
    // Notification don't return values. They work on the fire and forget principle? like PUBLISHERS
    public sealed record CompanyDeletedNotification(Guid Id, bool TrackChanges):INotification;
}
