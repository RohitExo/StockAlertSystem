# 📈 Stock Alert System

A highly resilient, asynchronous event-driven microservices architecture built with **.NET 9** and **RabbitMQ**. This system monitors stock ticket prices, evaluates target price criteria, and processes real-time alert notifications using high-availability quorum queues and robust dead-lettering strategies.

---

## 🏗️ Architecture & System Topology

The system splits operational responsibilities into independent runtime units to scale compute and messaging resources horizontally.

                    ┌────────────────────────────────────────┐
                    │          StockAlertSystem API          │
                    └───────────────────┬────────────────────┘
                                        │ (HTTP POST /alert)
                                        ▼
                   ┌──────────────────────────────────────────┐
                   │    RabbitMQ Broker (localhost:5672)     │
                   │                                          │
                   │   ┌──────────────────────────────────┐   │
                   │   │      Exchange: (Default "")      │   │
                   │   └────────────────┬─────────────────┘   │
                   │                    │                     │
                   │                    │ routing-key:        │
                   │                    │ "stock-alert"       │
                   │                    ▼                     │
                   │   ┌──────────────────────────────────┐   │
                   │   │      Queue: stock-alert          │   │
                   │   │      (Quorum / Persistent)       │   │
                   │   └────────────────┬─────────────────┘   │
                   │                    │                     │
                   │                    │ (On Process Error / │
                   │                    │  NACK requeue:false)│
                   │                    ▼                     │
                   │   ┌──────────────────────────────────┐   │
                   │   │   Exchange: stock-alert-dlx      │   │
                   │   └────────────────┬─────────────────┘   │
                   │                    │                     │
                   │                    │ routing-key:        │
                   │                    │ "dead-letter"       │
                   │                    ▼                     │
                   │   ┌──────────────────────────────────┐   │
                   │   │    Queue: stock-alert-dead-letter│   │
                   │   └──────────────────────────────────┘   │
                   └────────────────────┬─────────────────────┘
                                        │
                                        │ (Async Consume & ACK)
                                        ▼
                    ┌────────────────────────────────────────┐
                    │          Notification Service          │
                    └────────────────────────────────────────┐
					
					
---

## 📂 Repository Blueprint
📂 stockAlertSystem/
├── 📂 Src/
│   ├── 📂 StockAlertSystem.Api/             # Web API producer project
│   │   ├── 📂 Controllers/
│   │   │   └── 📄 AlertController.cs        # Evaluates conditions, pushes alerts
│   │   ├── 📂 Services/
│   │   │   ├── 📄 IMessageProducer.cs       # Contract for message distribution
│   │   │   └── 📄 MessageProducer.cs        # RabbitMQ async publishing client
│   │   ├── 📂 Models/
│   │   │   ├── 📄 Alert.cs                  # Structured message contract
│   │   │   ├── 📄 Ticket.cs                 # Domain entities & conditions
│   │   │   └── 📄 UpdatedTicketDto.cs       # Incoming price payload
│   │   └── 📄 Program.cs                    # Web host configuration
│   │
│   └── 📂 StockAlertSystem.Notification/    # Background worker consumer
│       └── 📄 Program.cs                    # Event loop consumer and DLQ error handler
├── 📄 docker-compose.yml                    # Multi-container infrastructure orchestration
└── 📄 StockAlertSystem.sln                  # Root solution file

---

## ⚡ Engineering Deep Dive

### State Machine Condition Reversal
To avoid alert flood storms, conditions flip automatically on trigger events:
* **Above/Equal Hit:** Triggers when `Current >= Target`. Condition changes to **Below**.
* **Below/Equal Hit:** Triggers when `Current <= Target`. Condition changes to **Above**.

### Production-Grade Messaging Reliability
* **Quorum Queues:** Configured via `x-queue-type: quorum` for Raft-based data replication across nodes.
* **Guaranteed Delivery:** Messages are flagged `Persistent = true` to write to disk upon broker ingestion.
* **Correlated Telemetry:** Every payload carries a unique `CorrelationId` tracking state across network boundaries.
* **Dead Letter Routing:** Failed processing outputs a negative acknowledgment (`BasicNackAsync` with `requeue: false`) pushing corrupted payloads directly to `stock-alert-dead-letter` via `stock-alert-dlx`.

---

## 🐳 Infrastructure Orchestration

The application utilizes Docker Compose to instantly guarantee identical environment states across development, testing, and production environments.

### `docker-compose.yml`
```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:4.0-management
    container_name: stock-alert-rabbitmq
    ports:
      - "5672:5672"   # AMQP protocol port
      - "15672:15672" # Management UI dashboard
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
      RABBITMQ_DEFAULT_VHOST: /
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_running"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  rabbitmq_data:
```

---

## 🗄️ In-Memory Data Blueprint

The system initialises state via a static seeding layout mimicking real database models.

### `MockData.cs` Seed Topology
```json
{
  "MockUser": {
    "UserId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "EmailAddress": "dev-alerts@domain.com"
  },
  "MockTickets": [
    {
      "TicketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",
      "TicketName": "MSFT",
      "TicketStatus": {
        "TargetPrice": 420.00,
        "TicketCondition": "Above"
      }
    },
    {
      "TicketId": "e4c2c477-1111-5c3b-9d8b-4486d43992b1",
      "TicketName": "AAPL",
      "TicketStatus": {
        "TargetPrice": 180.00,
        "TicketCondition": "Below"
      }
    }
  ]
}
```

---

## 🚀 Getting Started

### Prerequisites
* [.NET 9 SDK](https://microsoft.com)
* [Docker Desktop](https://docker.com)

### Installation & Launch

```bash
# 1. Clone the repository
git clone github.com
cd stockAlertSystem

# 2. Fire up the RabbitMQ broker cluster infrastructure 
docker-compose up -d

# 3. Restore NuGet dependencies
dotnet restore

# 4. Run the Notification Consumer service
dotnet run --project Src/StockAlertSystem.Notification/StockAlertSystem.Notification.csproj

# 5. Run the API Producer service (Open a separate terminal window)
dotnet run --project Src/StockAlertSystem.Api/StockAlertSystem.Api.csproj
```

---

## 🧪 Interactive API Verification & Testing

<details>
<summary><b>🔥 Case 1: Trigger an Alert (Price Hits Target)</b></summary>

### HTTP Request Payload
```bash
curl -X POST http://localhost:5000/api/alert \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",
    "hitPrice": 425.50
  }'
```

### Expected API Response (`200 OK`)
```json
{
  "status": "Triggered",
  "message": "Alert for MSFT sent to RabbitMQ!"
}
```

### Notification Service Log Receipt
```text
[StockAlert] Received: {"AlertId":"d3b1b366-0000-4b2a-8c7a-3375c32881a1","CorrelationId":"d3b1b366-0000-4b2a-8c7a-3375c32881c1","TicketName":"MSFT","HitPrice":425.50,"UserEmailAddress":"dev-alerts@domain.com","TriggeredAt":"2026-05-10T14:38:00Z"}
```
</details>

<details>
<summary><b>⏳ Case 2: Target Not Reached (Pending State)</b></summary>

### HTTP Request Payload
```bash
curl -X POST http://localhost:5000/api/alert \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",
    "hitPrice": 415.00
  }'
```

### Expected API Response (`200 OK`)
```json
{
  "status": "Pending",
  "message": "Price updated, but target not hit yet."
}
```
</details>

<details>
<summary><b>💥 Case 3: Dead Letter Queue (DLQ) Mitigation Isolation</b></summary>

### HTTP Request Payload
```bash
curl -X POST http://localhost:5000/api/alert \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",
    "hitPrice": 450.00,
    "customErrorPropertySimulation": "Error Payload Body"
  }'
```

### Notification Service Exception Tracing
```text
[DLQ Trigger] Failing message: Simulated processing failure
```
* **Broker Handling Matrix:** The application issues a negative acknowledgment (`BasicNackAsync` with `requeue: false`). The message is immediately offloaded from the premium `stock-alert` quorum queue and transferred into the `stock-alert-dead-letter` collection for secondary isolation and triage.
</details>