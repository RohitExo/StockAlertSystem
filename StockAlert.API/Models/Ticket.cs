using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;

namespace StockAlert.API.Models
{
    public enum TicketCondition
    {
        Above,
        Below,
        Equal
    }

    public class UserDetails
    {
        public required string UserName { get; init; }
        [EmailAddress]
        public required string EmailAddress { get; init; }
    }

    public class Ticket
    {
        public Guid TicketId { get; init; }
        public required string TicketName { get; init; }
        public double TicketPrice { get; init; }

        public required TicketStatus TicketStatus { get; init; }
    }

    public class TicketStatus
    {
        public double TargetPrice { get; init; }
        public TicketCondition TicketCondition { get; set; }
    }

    public class Alert
    {
        public Guid AlertId { get; init; }
        public Guid CorrelationId { get; init; }
        public required string TicketName { get; init; }
        public double HitPrice { get; init; }
        [EmailAddress]
        public required string UserEmailAddress { get; init; }
        public DateTime TriggeredAt { get; init; }
    }
    
    public class UpdatedTicketDto
    {
        public Guid TicketId { get; init; }
        public required string TicketName { get; init; }
        public double HitPrice { get; init; }
    }
}
