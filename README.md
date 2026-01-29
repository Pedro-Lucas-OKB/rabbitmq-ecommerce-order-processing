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
┌─────────────┐     ┌─────────────┐     ┌──────────────────────────┐
│   Cliente   │────►│     API     │────►│  order-created-exchange  │
└─────────────┘     └─────────────┘     └────────────┬─────────────┘
                           │                         │
                           │            ┌────────────┴────────────┐
                           │            │                         │
                           │    routing key:              routing key:
                           │    "order.created"           "payment.approved"
                           │            │                         │
                           │            ▼                         ▼
                           │    ┌──────────────┐          ┌─────────────────┐
                           │    │payment-queue │          │ inventory-queue │
                           │    └──────┬───────┘          └────────┬────────┘
                           │           │                           │
                           │           ▼                           ▼
                           │    ┌──────────────┐          ┌─────────────────┐
                           │    │PaymentWorker │─────────►│ InventoryWorker │
                           │    └──────┬───────┘ publica  └────────┬────────┘
                           │           │         se aprovado       │
                           ▼           ▼                           ▼
                    ┌─────────────────────────────────────────────────┐
                    │                   PostgreSQL                    │
                    └─────────────────────────────────────────────────┘
```

### Fluxo de Processamento

1. **Cliente** cria pedido via API
2. **API** salva no PostgreSQL e publica no RabbitMQ (`order.created`)
3. **PaymentWorker** consome, processa pagamento (70% aprovado, 30% rejeitado)
4. Se aprovado, publica na fila de estoque (`payment.approved`)
5. **InventoryWorker** consome e processa estoque (90% reservado, 10% sem estoque)
6. Status final: `Completed` ou `Failed`

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
├── OrderProcessing.Api/            # API REST (Minimal APIs)
├── OrderProcessing.Core/           # Domínio (Entities, DTOs, Enums, Validators)
├── OrderProcessing.Infrastructure/ # Persistência e Messaging
└── OrderProcessing.Workers/
    ├── PaymentWorker/              # Processa pagamentos
    └── InventoryWorker/            # Processa estoque
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

### 3. Rodar a API e Workers

```bash
# Terminal 1 - API
dotnet run --project src/OrderProcessing.Api

# Terminal 2 - PaymentWorker
dotnet run --project src/OrderProcessing.Workers/PaymentWorker

# Terminal 3 - InventoryWorker
dotnet run --project src/OrderProcessing.Workers/InventoryWorker
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
- [x] PaymentWorker (Consumer)
- [x] InventoryWorker
- [ ] NotificationWorker
- [ ] CI/CD com GitHub Actions
- [ ] Testes automatizados

## Licença

Este projeto é para fins de aprendizado.
