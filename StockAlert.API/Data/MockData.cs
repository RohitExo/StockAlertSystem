using StockAlert.API.Models;
using System.ComponentModel.DataAnnotations;

namespace StockAlert.API.Data
{
    public static class MockData
    {
        public static UserDetails GetMockUser() => new UserDetails
        {
            UserName = "MarketWhale_99",
            EmailAddress = "investor.pro@example.com"
        };

        public static Alert GetMockAlert() => new Alert
        {
            AlertId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881a1"),
            CorrelationId = Guid.NewGuid(),
            TicketName = "MSFT",
            HitPrice = 426.15,
            UserEmailAddress = "investor.pro@example.com",
            TriggeredAt = DateTime.UtcNow
        };

        public static List<Ticket> GetMockTicketList() => new List<Ticket>
        {
            new Ticket
            {
                TicketId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881b2"),
                TicketName = "MSFT",
                TicketPrice = 420.50,
                TicketStatus = new TicketStatus { TargetPrice = 431.00, TicketCondition = TicketCondition.Above }
            },
            new Ticket
            {
                TicketId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881b3"),
                TicketName = "AAPL",
                TicketPrice = 175.00,
                TicketStatus = new TicketStatus { TargetPrice = 170.00, TicketCondition = TicketCondition.Below }
            },
            new Ticket
            {
                TicketId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881b5"),
                TicketName = "TSLA",
                TicketPrice = 160.10,
                TicketStatus = new TicketStatus { TargetPrice = 173.05, TicketCondition = TicketCondition.Above }
            },
            new Ticket
            {
                TicketId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881b6"),
                TicketName = "NVDA",
                TicketPrice = 850.00,
                TicketStatus = new TicketStatus { TargetPrice = 800.00, TicketCondition = TicketCondition.Below }
            }
        };
    }
}
