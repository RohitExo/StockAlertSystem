using Microsoft.AspNetCore.Mvc;
using StockAlert.API.Data;
using StockAlert.API.Models;
using StockAlert.API.Services;

namespace StockAlert.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AlertController : Controller
    {
        private readonly IMessageProducer _messageProducer;
        public AlertController(IMessageProducer messageProducer) => this._messageProducer = messageProducer;

        [HttpPost]
        public IActionResult Alert(UpdatedTicketDto updatedTicketDto)
        {
            if (!ModelState.IsValid || updatedTicketDto == null) return BadRequest(ModelState);
            
            var ticket = MockData.GetMockTicketList().FirstOrDefault(x => x.TicketId == updatedTicketDto.TicketId);
            if (ticket == null) return NotFound("Ticket not found.");

            bool isAlertTriggered = ValidateUpdatedTicket(updatedTicketDto,ticket);
            
            if (isAlertTriggered)
            {
                var alert = GenerateMessage(updatedTicketDto,ticket);
                if (alert == null)
                {
                    return StatusCode(500, "Error generating alert message.");
                }
                _messageProducer.SendingMessage<Alert>(alert);
                return Ok(new { Status = "Triggered", Message = $"Alert for {ticket.TicketName} sent to RabbitMQ!" });
            }

            return Ok(new { Status = "Pending", Message = "Price updated, but target not hit yet." });
        }

        private bool ValidateUpdatedTicket(UpdatedTicketDto updatedTicketDto,Ticket ticket)
        {
            if (ticket != null)
            {
                // SCENARIO 1: Alert if price goes ABOVE or is EQUAL
                if (ticket.TicketStatus.TicketCondition == TicketCondition.Above ||
                    ticket.TicketStatus.TicketCondition == TicketCondition.Equal)
                {
                    if (updatedTicketDto.HitPrice >= ticket.TicketStatus.TargetPrice)
                    {
                        ticket.TicketStatus.TicketCondition = TicketCondition.Below;
                        return true;
                    }
                }
                // SCENARIO 2: Alert if price goes BELOW or is EQUAL
                else if (ticket.TicketStatus.TicketCondition == TicketCondition.Below ||
                    ticket.TicketStatus.TicketCondition == TicketCondition.Equal)
                {
                    if (updatedTicketDto.HitPrice <= ticket.TicketStatus.TargetPrice)
                    {
                        ticket.TicketStatus.TicketCondition = TicketCondition.Above;
                        return true;
                    }
                }
            }
            return false;
        }

        private Alert? GenerateMessage(UpdatedTicketDto updatedTicketDto, Ticket ticket)
        {
            var user = MockData.GetMockUser();
            if (ticket != null)
            {
                var alert = new Alert
                {
                    AlertId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881a1"),
                    CorrelationId = Guid.Parse("d3b1b366-0000-4b2a-8c7a-3375c32881c1"),
                    TicketName = ticket.TicketName,
                    HitPrice = updatedTicketDto.HitPrice,
                    UserEmailAddress = user.EmailAddress,
                    TriggeredAt = DateTime.UtcNow
                };
                return alert;
            }
            return null;
        }
    }
}
