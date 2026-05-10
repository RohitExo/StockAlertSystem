# \# 📈 Stock Alert System

# 

# A highly resilient, asynchronous event-driven microservices architecture built with \*\*.NET 9\*\* and \*\*RabbitMQ\*\*. This system monitors stock ticket prices, evaluates target price criteria, and processes real-time alert notifications using high-availability quorum queues and robust dead-lettering strategies.

# 

# ---

# 

# \## 🏗️ Architecture \& System Topology

# 

# The system splits operational responsibilities into independent runtime units to scale compute and messaging resources horizontally.

# 

# Use code with caution.┌────────────────────────────────────────┐│          StockAlertSystem API          │└───────────────────┬────────────────────┘│ (HTTP POST /alert)▼┌──────────────────────────────────────────┐│    RabbitMQ Broker (localhost:5672)     ││                                          ││   ┌──────────────────────────────────┐   ││   │      Exchange: (Default "")      │   ││   └────────────────┬─────────────────┘   ││                    │                     ││                    │ routing-key:        ││                    │ "stock-alert"       ││                    ▼                     ││   ┌──────────────────────────────────┐   ││   │      Queue: stock-alert          │   ││   │      (Quorum / Persistent)       │   ││   └────────────────┬─────────────────┘   ││                    │                     ││                    │ (On Process Error / ││                    │  NACK requeue:false)││                    ▼                     ││   ┌──────────────────────────────────┐   ││   │   Exchange: stock-alert-dlx      │   ││   └────────────────┬─────────────────┘   ││                    │                     ││                    │ routing-key:        ││                    │ "dead-letter"       ││                    ▼                     ││   ┌──────────────────────────────────┐   ││   │    Queue: stock-alert-dead-letter│   ││   └──────────────────────────────────┘   │└────────────────────┬─────────────────────┘││ (Async Consume \& ACK)▼┌────────────────────────────────────────┐│          Notification Service          │└────────────────────────────────────────┘

# ---

# 

# \## 📂 Repository Blueprint

# 

# 📂 stockAlertSystem/├── 📂 Src/│   ├── 📂 StockAlertSystem.Api/             # Web API producer project│   │   ├── 📂 Controllers/│   │   │   └── 📄 AlertController.cs        # Evaluates conditions, pushes alerts│   │   ├── 📂 Services/│   │   │   ├── 📄 IMessageProducer.cs       # Contract for message distribution│   │   │   └── 📄 MessageProducer.cs        # RabbitMQ async publishing client│   │   ├── 📂 Models/│   │   │   ├── 📄 Alert.cs                  # Structured message contract│   │   │   ├── 📄 Ticket.cs                 # Domain entities \& conditions│   │   │   └── 📄 UpdatedTicketDto.cs       # Incoming price payload│   │   └── 📄 Program.cs                    # Web host configuration│   ││   └── 📂 StockAlertSystem.Notification/    # Background background worker consumer│       └── 📄 Program.cs                    # Event loop consumer and DLQ error handler├── 📄 docker-compose.yml                    # Multi-container infrastructure orchestration└── 📄 StockAlertSystem.sln                  # Root solution file

# ---

# 

# \## ⚡ Engineering Deep Dive

# 

# \### State Machine Condition Reversal

# To avoid alert flood storms, conditions flip automatically on trigger events:

# \* \*\*Above/Equal Hit:\*\* Triggers when `Current >= Target`. Condition changes to \*\*Below\*\*.

# \* \*\*Below/Equal Hit:\*\* Triggers when `Current <= Target`. Condition changes to \*\*Above\*\*.

# 

# \### Production-Grade Messaging Reliability

# \* \*\*Quorum Queues:\*\* Configured via `x-queue-type: quorum` for Raft-based data replication across nodes.

# \* \*\*Guaranteed Delivery:\*\* Messages are flagged `Persistent = true` to write to disk upon broker ingestion.

# \* \*\*Correlated Telemetry:\*\* Every payload carries a unique `CorrelationId` tracking state across network boundaries.

# \* \*\*Dead Letter Routing:\*\* Failed processing outputs a negative acknowledgment (`BasicNackAsync` with `requeue: false`) pushing corrupted payloads directly to `stock-alert-dead-letter` via `stock-alert-dlx`.

# 

# ---

# 

# \## 🐳 Infrastructure Orchestration

# 

# The application utilizes Docker Compose to instantly guarantee identical environment states across development, testing, and production environments.

# 

# \### `docker-compose.yml`

# ```yaml

# version: '3.8'

# 

# services:

# &nbsp; rabbitmq:

# &nbsp;   image: rabbitmq:4.0-management

# &nbsp;   container\_name: stock-alert-rabbitmq

# &nbsp;   ports:

# &nbsp;     - "5672:5672"   # AMQP protocol port

# &nbsp;     - "15672:15672" # Management UI dashboard

# &nbsp;   environment:

# &nbsp;     RABBITMQ\_DEFAULT\_USER: guest

# &nbsp;     RABBITMQ\_DEFAULT\_PASS: guest

# &nbsp;     RABBITMQ\_DEFAULT\_VHOST: /

# &nbsp;   volumes:

# &nbsp;     - rabbitmq\_data:/var/lib/rabbitmq

# &nbsp;   healthcheck:

# &nbsp;     test: \["CMD", "rabbitmq-diagnostics", "check\_running"]

# &nbsp;     interval: 10s

# &nbsp;     timeout: 5s

# &nbsp;     retries: 5

# 

# volumes:

# &nbsp; rabbitmq\_data:

# ```

# 

# ---

# 

# \## 🗄️ In-Memory Data Blueprint

# 

# The system initialises state via a static seeding layout mimicking real database models.

# 

# \### `MockData.cs` Seed Topology

# ```json

# {

# &nbsp; "MockUser": {

# &nbsp;   "UserId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",

# &nbsp;   "EmailAddress": "dev-alerts@domain.com"

# &nbsp; },

# &nbsp; "MockTickets": \[

# &nbsp;   {

# &nbsp;     "TicketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",

# &nbsp;     "TicketName": "MSFT",

# &nbsp;     "TicketStatus": {

# &nbsp;       "TargetPrice": 420.00,

# &nbsp;       "TicketCondition": "Above"

# &nbsp;     }

# &nbsp;   },

# &nbsp;   {

# &nbsp;     "TicketId": "e4c2c477-1111-5c3b-9d8b-4486d43992b1",

# &nbsp;     "TicketName": "AAPL",

# &nbsp;     "TicketStatus": {

# &nbsp;       "TargetPrice": 180.00,

# &nbsp;       "TicketCondition": "Below"

# &nbsp;     }

# &nbsp;   }

# &nbsp; ]

# }

# ```

# 

# ---

# 

# \## 🚀 Getting Started

# 

# \### Prerequisites

# \* \[.NET 9 SDK](https://microsoft.com)

# \* \[Docker Desktop](https://docker.com)

# 

# \### Installation \& Launch

# 

# ```bash

# \# 1. Clone the repository

# git clone github.com

# cd stockAlertSystem

# 

# \# 2. Fire up the RabbitMQ broker cluster infrastructure 

# docker-compose up -d

# 

# \# 3. Restore NuGet dependencies

# dotnet restore

# 

# \# 4. Run the Notification Consumer service

# dotnet run --project Src/StockAlertSystem.Notification/StockAlertSystem.Notification.csproj

# 

# \# 5. Run the API Producer service (Open a separate terminal window)

# dotnet run --project Src/StockAlertSystem.Api/StockAlertSystem.Api.csproj

# ```

# 

# ---

# 

# \## 🧪 Interactive API Verification \& Testing

# 

# <details>

# <summary><b>🔥 Case 1: Trigger an Alert (Price Hits Target)</b></summary>

# 

# \### HTTP Request Payload

# ```bash

# curl -X POST http://localhost:5000/api/alert \\

# &nbsp; -H "Content-Type: application/json" \\

# &nbsp; -d '{

# &nbsp;   "ticketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",

# &nbsp;   "hitPrice": 425.50

# &nbsp; }'

# ```

# 

# \### Expected API Response (`200 OK`)

# ```json

# {

# &nbsp; "status": "Triggered",

# &nbsp; "message": "Alert for MSFT sent to RabbitMQ!"

# }

# ```

# 

# \### Notification Service Log Receipt

# ```text

# \[StockAlert] Received: {"AlertId":"d3b1b366-0000-4b2a-8c7a-3375c32881a1","CorrelationId":"d3b1b366-0000-4b2a-8c7a-3375c32881c1","TicketName":"MSFT","HitPrice":425.50,"UserEmailAddress":"dev-alerts@domain.com","TriggeredAt":"2026-05-10T14:38:00Z"}

# ```

# </details>

# 

# <details>

# <summary><b>⏳ Case 2: Target Not Reached (Pending State)</b></summary>

# 

# \### HTTP Request Payload

# ```bash

# curl -X POST http://localhost:5000/api/alert \\

# &nbsp; -H "Content-Type: application/json" \\

# &nbsp; -d '{

# &nbsp;   "ticketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",

# &nbsp;   "hitPrice": 415.00

# &nbsp; }'

# ```

# 

# \### Expected API Response (`200 OK`)

# ```json

# {

# &nbsp; "status": "Pending",

# &nbsp; "message": "Price updated, but target not hit yet."

# }

# ```

# </details>

# 

# <details>

# <summary><b>💥 Case 3: Dead Letter Queue (DLQ) Mitigation Isolation</b></summary>

# 

# \### HTTP Request Payload

# ```bash

# curl -X POST http://localhost:5000/api/alert \\

# &nbsp; -H "Content-Type: application/json" \\

# &nbsp; -d '{

# &nbsp;   "ticketId": "d3b1b366-0000-4b2a-8c7a-3375c32881a0",

# &nbsp;   "hitPrice": 450.00,

# &nbsp;   "customErrorPropertySimulation": "Error Payload Body"

# &nbsp; }'

# ```

# 

# \### Notification Service Exception Tracing

# ```text

# \[DLQ Trigger] Failing message: Simulated processing failure

# ```

# \* \*\*Broker Handling Matrix:\*\* The application issues a negative acknowledgment (`BasicNackAsync` with `requeue: false`). The message is immediately offloaded from the premium `stock-alert` quorum queue and transferred into the `stock-alert-dead-letter` collection for secondary isolation and triage.

# </details>

