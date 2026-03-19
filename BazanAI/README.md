
# Bazan AI - Agentic Microservices Platform for Coffee Farmers

Bazan AI is a comprehensive platform designed to empower coffee farmers in Vietnam's Central Highlands with AI-driven insights, market data, and agronomy support.

## Architecture

```ascii
[ Clients ] <--> [ API Gateway (YARP) ] <--> [ Microservices ]
                                                   |
                                            +------+------+
                                            |             |
                                      [ Event Bus ] [ Data Stores ]
```

## Tech Stack
- **Backend:** .NET 8, Python 3.12 (FastAPI)
- **Database:** MongoDB (Replica Set), Qdrant, Redis
- **Messaging:** RabbitMQ (MassTransit)
- **AI:** Microsoft Semantic Kernel
- **Observability:** Seq, Prometheus, Grafana, OpenTelemetry

## Setup
1. `make up` - Start infrastructure
2. `make migrate` - Initialize databases
3. `dotnet run` - Start services (or use Docker)
