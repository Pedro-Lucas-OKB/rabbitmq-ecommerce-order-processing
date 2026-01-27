# E-commerce Order Processing

Sistema de processamento assíncrono de pedidos de e-commerce utilizando **RabbitMQ** e **.NET 8**.

## Sobre o Projeto

Este projeto implementa uma arquitetura de microsserviços para processamento de pedidos, onde diferentes partes do fluxo (pagamento, estoque, notificação) são processadas de forma **assíncrona** e **independente** através do RabbitMQ.

### Objetivos de Aprendizado

- RabbitMQ (exchanges, queues, routing, acknowledgments)
- Processamento assíncrono com Workers
- Entity Framework Core com PostgreSQL
- CI/CD com GitHub Actions (em breve)

## Arquitetura

```
┌─────────────┐     ┌─────────────┐     ┌─────────────────┐
│   Cliente   │────►│     API     │────►│    RabbitMQ     │
└─────────────┘     └─────────────┘     └────────┬────────┘
                           │                     │
                           ▼                     ▼
                    ┌─────────────┐     ┌─────────────────┐
                    │  PostgreSQL │◄────│  PaymentWorker  │
                    └─────────────┘     └─────────────────┘
```

## Stack Tecnológica

- **.NET 8** - Framework principal
- **ASP.NET Core Minimal APIs** - API REST
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **RabbitMQ** - Message Broker
- **Docker & Docker Compose** - Containerização
- **FluentValidation** - Validação de requests

## Estrutura do Projeto

```
src/
├── OrderProcessing.Api/           # API REST (Minimal APIs)
├── OrderProcessing.Core/          # Domínio (Entities, DTOs, Enums)
├── OrderProcessing.Infrastructure/# Persistência e Messaging
└── OrderProcessing.Workers/       # Workers (em desenvolvimento)
    └── PaymentWorker/
```

## Executando o Projeto

### Pré-requisitos

- .NET 8 SDK
- Docker e Docker Compose

### 1. Subir a infraestrutura

```bash
docker compose up -d
```

Isso inicia:
- **PostgreSQL** (porta 5432)
- **RabbitMQ** (porta 5672, Management UI: 15672)
- **PgAdmin** (porta 5050)

### 2. Aplicar migrations

```bash
dotnet ef database update -p src/OrderProcessing.Infrastructure -s src/OrderProcessing.Api
```

### 3. Rodar a API

```bash
dotnet run --project src/OrderProcessing.Api
```

### 4. Acessar

- **Swagger:** http://localhost:5000/swagger
- **RabbitMQ Management:** http://localhost:15672 (admin/admin123)
- **PgAdmin:** http://localhost:5050 (admin@ecommerce.com/admin123)

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/orders` | Criar pedido |
| `GET` | `/api/orders` | Listar pedidos |
| `GET` | `/api/orders/{id}` | Buscar pedido por ID |

## Status do Projeto

- [x] API REST com Minimal APIs
- [x] Integração com PostgreSQL
- [x] Publisher RabbitMQ
- [ ] PaymentWorker (Consumer)
- [ ] InventoryWorker
- [ ] NotificationWorker
- [ ] CI/CD com GitHub Actions
- [ ] Testes automatizados

## Licença

Este projeto é para fins de aprendizado.
