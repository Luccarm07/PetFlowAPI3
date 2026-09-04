# 🐾 PetFlow API

**C# · .NET 8 · ASP.NET Core · Oracle Database · Entity Framework Core · Swagger/OpenAPI · Serilog · OpenTelemetry · xUnit**

---

## 📋 Sumário

- [Sobre o Projeto](#sobre-o-projeto)
- [Equipe](#equipe)
- [Objetivo do Challenge](#objetivo-do-challenge)
- [Arquitetura do Projeto](#arquitetura-do-projeto)
- [Visão de Domínio](#visão-de-domínio)
- [Documentação das Rotas](#documentação-das-rotas)
- [Monitoramento e Observabilidade](#monitoramento-e-observabilidade)
- [Banco de Dados](#banco-de-dados)
- [Instruções de Instalação e Execução](#instruções-de-instalação-e-execução)
- [Testes Automatizados](#testes-automatizados)
- [Testes Manuais da API](#testes-manuais-da-api)
- [Autenticação (JWT) — infraestrutura opcional](#autenticação-jwt--infraestrutura-opcional)
- [Observações Finais](#observações-finais)

---

## 📌 Sobre o Projeto

O **PetFlow** é uma API REST desenvolvida em **C# com ASP.NET Core (.NET 8)** para gerenciamento de saúde preventiva de pets. A proposta central é a **gamificação do cuidado animal**: toda vez que um tutor registra um evento de saúde concluído para seu pet (vacina, consulta, banho medicado, etc.), ele acumula pontos que podem ser trocados por cupons de desconto em clínicas parceiras.

O sistema gerencia o ciclo completo: cadastro de tutores e pets, vínculo com clínicas e planos de saúde, assinatura de planos, histórico de eventos clínicos, acúmulo de pontos de recompensa, emissão de cupons e registro de resgates.

Na **Sprint 3**, o projeto evoluiu para incorporar monitoramento, observabilidade e testes automatizados:

- **observabilidade completa**: health checks, logging estruturado e distributed tracing/métricas
- **testes automatizados** unitários e de integração seguindo o padrão AAA (Arrange-Act-Assert)
- infraestrutura opcional de **login e emissão de token JWT**. Nesta versão, o token é emitido e validado pelo serviço, mas **as rotas de negócio ainda não exigem `[Authorize]`** (ver seção [Autenticação (JWT)](#autenticação-jwt--infraestrutura-opcional))

O projeto demonstra uma aplicação backend robusta com:
- arquitetura em camadas bem definida
- persistência relacional via Oracle Database
- mapeamento objeto-relacional com Entity Framework Core
- transformação de dados com AutoMapper e DTOs
- documentação automática e interativa via Swagger
- tratamento global de exceções com mensagens amigáveis
- monitoramento de saúde da aplicação, do banco e de serviços externos
- logging correlacionado e tracing distribuído via OpenTelemetry
- testes com xUnit, Moq e `WebApplicationFactory`, com `coverlet.collector` disponível para coleta opcional de cobertura

---

## 👥 Equipe

| Nome | RM |
|------|----|
| Lucas Grillo Alcântara | 561413 |
| Pietro Ferreira Gomes Abrahamian | 561469 |
| Pedro Peres Benitez | 561792 |
| Lucca Ramos Mussumecci | 562027 |

**Turma:** 2TDSPX

---

## 🎯 Objetivo do Challenge

Desenvolver uma solução utilizando C# e ASP.NET Core capaz de:

- persistir dados em banco relacional Oracle
- gerenciar informações de saúde pet com lógica de gamificação por pontos
- aplicar Programação Orientada a Objetos com entidades bem definidas
- utilizar Entity Framework Core com relacionamentos entre entidades
- garantir validações e tratamento de exceções para erros Oracle
- respeitar os fundamentos de APIs REST (verbos HTTP, códigos de status, recursos)
- disponibilizar documentação interativa via Swagger/OpenAPI
- expor **health checks**, **logging estruturado** e **tracing/métricas** para monitoramento
- garantir qualidade com **testes automatizados** (unitários e de integração)
- prover infraestrutura de login e emissão de token JWT (funcionalidade complementar; não obrigatória para a Sprint 3)
- atender aos requisitos técnicos da disciplina

---

## 🧱 Arquitetura do Projeto

O projeto segue uma arquitetura em camadas com separação clara de responsabilidades:

```
PetFlowAPI3/
├── PetFlowAPI/
│   ├── Controllers/              # Camada de apresentação — endpoints REST
│   │   ├── AuthController.cs     # Login e emissão de token JWT
│   │   ├── TutorController.cs
│   │   ├── PetController.cs
│   │   ├── ClinicController.cs
│   │   ├── PlanController.cs
│   │   ├── SubscriptionController.cs
│   │   ├── HealthEventController.cs
│   │   ├── CouponController.cs
│   │   └── RedeemController.cs
│   │
│   ├── Data/
│   │   └── PetFlowContext.cs         # DbContext do Entity Framework Core
│   │
│   ├── Domain/
│   │   └── RewardPointCalculator.cs  # Regra de negócio: cálculo de pontos por evento
│   │
│   ├── DTOs/
│   │   └── PetFlowDTOs.cs            # Objetos de entrada e saída (Request/Response)
│   │
│   ├── Enums/
│   │   └── PetFlowEnums.cs           # CouponStatus, HealthEventStatus, SubscriptionStatus
│   │
│   ├── HealthChecks/
│   │   └── HealthChecks.cs           # Health check de serviços externos + response writer
│   │
│   ├── Mappings/
│   │   └── MappingProfile.cs         # Configurações do AutoMapper
│   │
│   ├── Migrations/                   # Migrações geradas pelo EF Core
│   │
│   ├── Models/
│   │   └── PetFlowModels.cs          # Entidades mapeadas para as tabelas Oracle
│   │
│   ├── Security/
│   │   └── AuthServices.cs           # PasswordService (hash) e TokenService (JWT)
│   │
│   ├── appsettings.json              # Configuração com placeholders; sem credenciais reais
│   └── Program.cs                    # Bootstrap, middlewares, observabilidade e DI
│
├── PetFlowAPI.Tests/                 # Projeto de testes (xUnit)
│   ├── ApiFactory.cs                 # WebApplicationFactory com banco InMemory
│   ├── ApiIntegrationTests.cs        # Testes de integração (health checks, rotas, 404)
│   ├── AuthTests.cs                  # Testes de autenticação (unitários + integração)
│   ├── HealthChecksTests.cs          # Testes unitários dos health checks (Moq)
│   └── RewardPointCalculatorTests.cs # Testes unitários da regra de pontuação
│
├── Database/
│   └── 2TDSPX_CodigoSql_PetFlow.sql  # Script Oracle: DDL, cargas, procedures, triggers
│
├── .gitignore
├── .gitattributes
└── PetFlowAPI.sln
```

---

## 🧠 Visão de Domínio

| Entidade | Descrição |
|----------|-----------|
| 👤 **Tutor** | Responsável pelos pets. Possui endereços, pontos de recompensa, histórico de resgates e credenciais de acesso (login). |
| 🐾 **Pet** | Animal vinculado a um tutor e a uma espécie. Possui eventos de saúde, assinaturas de planos e score de risco. |
| 🏥 **Clínica** | Veterinária parceira do sistema. Oferece planos e registra eventos de saúde. |
| 📄 **Plano** | Plano de saúde vinculado a uma clínica, com duração em dias e multiplicador de pontos por evento. |
| 📅 **Assinatura** | Contratação de um plano por um pet. Controla período e status. |
| ❤️ **Evento de Saúde** | Registro clínico e preventivo do pet (vacina, consulta, banho, etc.). |
| 🎟️ **Cupom** | Cupom de desconto gerado via template, com código único, status e validade. |
| 🎫 **Resgate** | Uso de um cupom pelo tutor, consumindo pontos acumulados. |
| 🔢 **Ponto de Recompensa** | Pontos acumulados pelo tutor por ações realizadas (calculados via `RewardPointCalculator`). |
| ⚠️ **Score de Risco** | Pontuação de risco calculada por pet, classificada em faixas de nível de risco. |

## 📦 Documentação das Rotas

A API roda em `http://localhost:5000`. Todos os recursos suportam paginação via `?page=0&size=10`.

> Acesse a documentação interativa completa em: **`http://localhost:5000/swagger`**

---

### 🔐 Autenticação — `/auth`

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/auth/login` | Autentica um tutor e retorna o token JWT |

---

### 👤 Tutores — `/tutors`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/tutors` | Lista tutores com paginação e filtro por nome | `name`, `page`, `size` |
| `GET` | `/tutors/{id}` | Busca tutor pelo ID | — |
| `POST` | `/tutors` | Cadastra novo tutor (rota pública) | — |
| `PUT` | `/tutors/{id}` | Atualiza dados do tutor | — |
| `DELETE` | `/tutors/{id}` | Remove tutor e todos os dependentes em cascata | — |

**Request body (POST / PUT):**
```json
{
  "name": "Maria Silva",
  "email": "maria@email.com",
  "phone": "11999999999",
  "password": "senha123"
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "name": "Maria Silva",
  "email": "maria@email.com",
  "phone": "11999999999",
  "createdAt": "2026-05-17T10:00:00"
}
```

> O DELETE remove em cascata: resgates, pontos de recompensa, endereços, pets e todos os filhos dos pets (eventos, assinaturas, scores de risco).

---

### 🐾 Pets — `/pets`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/pets` | Lista pets com paginação e filtro por nome | `name`, `page`, `size` |
| `GET` | `/pets/{id}` | Busca pet pelo ID | — |
| `POST` | `/pets` | Cadastra novo pet | — |
| `PUT` | `/pets/{id}` | Atualiza dados do pet | — |
| `DELETE` | `/pets/{id}` | Remove pet e seus eventos, assinaturas e scores de risco | — |

**Request body (POST / PUT):**
```json
{
  "name": "Rex",
  "breed": "Labrador",
  "birthDate": "2020-03-15",
  "weight": 28.5,
  "speciesId": 1,
  "tutorId": 1
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "name": "Rex",
  "breed": "Labrador",
  "birthDate": "2020-03-15T00:00:00",
  "weight": 28.5,
  "speciesId": 1,
  "tutorId": 1,
  "createdAt": "2026-05-17T10:00:00"
}
```

---

### 🏥 Clínicas — `/clinics`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/clinics` | Lista clínicas com paginação e filtro por nome | `name`, `page`, `size` |
| `GET` | `/clinics/{id}` | Busca clínica pelo ID | — |
| `POST` | `/clinics` | Cadastra nova clínica | — |
| `PUT` | `/clinics/{id}` | Atualiza dados da clínica | — |
| `DELETE` | `/clinics/{id}` | Remove clínica e todos os dependentes em cascata | — |

**Request body (POST / PUT):**
```json
{
  "name": "Clínica Vet Saúde",
  "address": "Rua das Flores, 100 - São Paulo/SP",
  "phone": "1133334444",
  "cnpj": "12.345.678/0001-99"
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "name": "Clínica Vet Saúde",
  "address": "Rua das Flores, 100 - São Paulo/SP",
  "phone": "1133334444",
  "cnpj": "12.345.678/0001-99",
  "createdAt": "2026-05-17T10:00:00"
}
```

> O DELETE remove em cascata: planos, assinaturas, eventos de saúde, descontos de parceiros, templates de cupom, cupons e resgates.

---

### 📄 Planos — `/plans`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/plans` | Lista planos com paginação e filtro por nome | `name`, `page`, `size` |
| `GET` | `/plans/{id}` | Busca plano pelo ID | — |
| `POST` | `/plans` | Cadastra novo plano | — |
| `PUT` | `/plans/{id}` | Atualiza dados do plano | — |
| `DELETE` | `/plans/{id}` | Remove plano e suas assinaturas vinculadas | — |

**Request body (POST / PUT):**
```json
{
  "name": "Plano Premium",
  "description": "Cobertura completa preventiva",
  "price": 89.90,
  "durationDays": 365,
  "pointsPerEvent": 2,
  "clinicId": 1
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "name": "Plano Premium",
  "description": "Cobertura completa preventiva",
  "price": 89.90,
  "durationDays": 365,
  "pointsPerEvent": 2,
  "clinicId": 1
}
```

---

### 📅 Assinaturas — `/subscriptions`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/subscriptions` | Lista assinaturas com paginação e filtro por pet e status | `petId`, `status`, `page`, `size` |
| `GET` | `/subscriptions/{id}` | Busca assinatura pelo ID | — |
| `POST` | `/subscriptions` | Cria nova assinatura | — |
| `PUT` | `/subscriptions/{id}` | Atualiza dados da assinatura | — |
| `PUT` | `/subscriptions/{id}/status` | Atualiza apenas o status da assinatura | `status` (query param) |
| `DELETE` | `/subscriptions/{id}` | Remove assinatura | — |

**Status disponíveis:** `ATIVO` · `ENCERRADO` · `CANCELADO` · `EXPIRADO`

**Request body (POST / PUT):**
```json
{
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "status": "ATIVO",
  "petId": 1,
  "planId": 1
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-12-31T00:00:00",
  "status": "ATIVO",
  "petId": 1,
  "planId": 1,
  "createdAt": "2026-05-17T10:00:00"
}
```

**Atualizar status:**
```
PUT /subscriptions/1/status?status=ENCERRADO
```

---

### ❤️ Eventos de Saúde — `/health-events`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/health-events` | Lista eventos com paginação e filtro por pet e status | `petId`, `status`, `page`, `size` |
| `GET` | `/health-events/{id}` | Busca evento pelo ID | — |
| `POST` | `/health-events` | Registra novo evento de saúde | — |
| `PUT` | `/health-events/{id}` | Atualiza dados do evento | — |
| `DELETE` | `/health-events/{id}` | Remove evento de saúde | — |

**Status disponíveis:** `PENDENTE` · `REALIZADO` · `CANCELADO`

**Request body (POST / PUT):**
```json
{
  "description": "Vacinação antirrábica anual",
  "eventDate": "2026-06-10T10:00:00",
  "status": "PENDENTE",
  "eventTypeId": 1,
  "petId": 1,
  "clinicId": 1
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "description": "Vacinação antirrábica anual",
  "eventDate": "2026-06-10T10:00:00",
  "status": "PENDENTE",
  "eventTypeId": 1,
  "petId": 1,
  "clinicId": 1,
  "createdAt": "2026-05-17T10:00:00"
}
```

> Ao marcar um evento como `REALIZADO`, os pontos de recompensa do tutor são calculados via `RewardPointCalculator`, multiplicando os pontos base do `event_type` pelo multiplicador do `plan` vigente.

---

### 🎟️ Cupons — `/coupons`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/coupons` | Lista cupons com paginação e filtro por código | `code`, `page`, `size` |
| `GET` | `/coupons/{id}` | Busca cupom pelo ID | — |
| `POST` | `/coupons` | Cadastra novo cupom (valida `expirationDate`) | — |
| `PUT` | `/coupons/{id}` | Atualiza dados do cupom (valida `expirationDate`) | — |
| `PUT` | `/coupons/{id}/status` | Atualiza apenas o status do cupom | `status` (query param) |
| `DELETE` | `/coupons/{id}` | Remove cupom e seus resgates vinculados | — |

**Status disponíveis:** `DISPONIVEL` · `RESGATADO` · `EXPIRADO`

> A API valida que `expirationDate` não pode ser uma data no passado. Retorna `400` caso contrário.

**Request body (POST / PUT):**
```json
{
  "code": "PETFLOW2026",
  "status": "DISPONIVEL",
  "expirationDate": "2026-12-31",
  "templateId": 1
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "code": "PETFLOW2026",
  "status": "DISPONIVEL",
  "expirationDate": "2026-12-31T00:00:00",
  "templateId": 1,
  "createdAt": "2026-05-17T10:00:00"
}
```

**Atualizar status:**
```
PUT /coupons/1/status?status=RESGATADO
```

---

### 🎫 Resgates — `/redeems`

| Método | Rota | Descrição | Parâmetros de Query |
|--------|------|-----------|---------------------|
| `GET` | `/redeems` | Lista resgates com paginação e filtro por tutor | `tutorId`, `page`, `size` |
| `GET` | `/redeems/{id}` | Busca resgate pelo ID | — |
| `POST` | `/redeems` | Registra novo resgate de cupom | — |
| `DELETE` | `/redeems/{id}` | Remove resgate | — |

**Request body (POST):**
```json
{
  "pointsUsed": 150,
  "tutorId": 1,
  "couponId": 1
}
```

**Response (200 / 201):**
```json
{
  "id": 1,
  "pointsUsed": 150,
  "tutorId": 1,
  "couponId": 1,
  "createdAt": "2026-05-17T10:00:00"
}
```

---

### ⚠️ Respostas de Erro

A API trata globalmente os erros Oracle com respostas padronizadas, sempre incluindo o `correlationId` da requisição:

| Código HTTP | Situação |
|-------------|----------|
| `400` | Campo obrigatório ausente (`ORA-01400`) ou valor fora do limite (`ORA-01438`) |
| `400` | `expirationDate` no passado (validação de negócio nos cupons) |
| `401` | Credenciais inválidas em `POST /auth/login` |
| `404` | Recurso não encontrado |
| `409` | Registro duplicado — email ou código já existente (`ORA-00001`) |
| `409` | Bloqueio por dependência de FK (`ORA-02291` / `ORA-02292`) |
| `500` | Erro interno inesperado |

**Formato padrão de erro:**
```json
{
  "statusCode": 400,
  "erro": "Campo obrigatório ausente.",
  "detalhe": "mensagem detalhada (apenas em Development)",
  "correlationId": "a1b2c3d4e5f6..."
}
```

---

## 🩺 Monitoramento e Observabilidade

A partir da Sprint 3, a API expõe endpoints e instrumentação dedicados a observabilidade.

### Health Checks

| Endpoint | Descrição |
|----------|-----------|
| `GET /health` | Status geral da aplicação (todos os checks registrados) |
| `GET /health/ready` | Readiness — verifica dependências críticas (banco Oracle e serviços externos) |
| `GET /health/live` | Liveness — confirma apenas que o processo está rodando, sem depender do banco |

**Exemplo de resposta (`GET /health`):**
```json
{
  "status": "Healthy",
  "totalDuration": 42.5,
  "checks": [
    {
      "name": "oracle-database",
      "status": "Healthy",
      "duration": 30.1,
      "description": null,
      "error": null,
      "data": {}
    },
    {
      "name": "external-services",
      "status": "Healthy",
      "duration": 5.2,
      "description": "Nenhum serviço externo configurado.",
      "error": null,
      "data": {}
    }
  ]
}
```

Serviços externos a serem verificados podem ser configurados em `appsettings.json`:
```json
"HealthChecks": {
  "ExternalServices": [ "https://exemplo.com/health" ]
}
```

### Logging Estruturado

- **Serilog** grava logs no console com nível, timestamp, `TraceId` e `CorrelationId`.
- Cada requisição recebe (ou propaga, se enviado pelo cliente) um header `X-Correlation-ID`, usado para correlacionar logs de uma mesma chamada ponta a ponta.
- Exceções não tratadas são logadas com `Log.Error`, incluindo o `CorrelationId` da requisição que falhou.

### Tracing e Métricas

- **OpenTelemetry** instrumenta automaticamente ASP.NET Core, `HttpClient` e o runtime .NET, exportando traces e métricas via console exporter.
- Uma métrica customizada (`petflow.http.request.duration`) registra a duração de cada requisição HTTP, com o `status_code` como dimensão.

---

## 🗄️ Banco de Dados

### Executar o script no Oracle

O script SQL completo está em [`Database/2TDSPX_CodigoSql_PetFlow.sql`](./Database/2TDSPX_CodigoSql_PetFlow.sql) e contém, na ordem de execução:

1. DDL — criação de todas as tabelas
2. Tabela de log de erros (`log_erros`)
3. Procedures de carga inicial de dados
4. Chamadas das procedures (carga inicial)
5. Consultas de exemplo (joins, group by, order by)
6. Blocos com `LAG`/`LEAD`
7. Relatórios com cursor explícito
8. Tabela e trigger de auditoria (`log_auditoria_health_event`)

Execute-o em sua instância Oracle antes de iniciar a aplicação.

### Tabelas

| Tabela | Descrição |
|--------|-----------|
| `TUTOR` | Tutores cadastrados (inclui hash de senha para login) |
| `ADDRESS` | Endereços dos tutores |
| `PET` | Pets vinculados a tutores e espécies |
| `SPECIES` | Espécies disponíveis (cão, gato, etc.) |
| `CLINIC` | Clínicas veterinárias parceiras |
| `PLAN` | Planos de saúde das clínicas |
| `SUBSCRIPTION` | Assinaturas de planos por pets |
| `HEALTH_EVENT` | Eventos de saúde registrados |
| `EVENT_TYPE` | Tipos de evento com pontuação |
| `COUPON` | Cupons de desconto emitidos |
| `COUPON_TEMPLATE` | Templates de cupom vinculados a parceiros |
| `PARTNER_DISCOUNT` | Descontos de clínicas parceiras |
| `REDEEM` | Resgates de cupons por tutores |
| `REWARD_POINT` | Pontos acumulados pelos tutores |
| `REWARD_ACTION` | Tipos de ação que geram pontos |
| `RISK_SCORE` | Score de risco calculado por pet |
| `RISK_LEVEL` | Faixas de classificação de risco |
| `LOG_ERROS` | Log de erros das procedures |
| `LOG_AUDITORIA_HEALTH_EVENT` | Auditoria de INSERT/UPDATE/DELETE em `HEALTH_EVENT` (via trigger) |

---

## 🚀 Instruções de Instalação e Execução

### Pré-requisitos

Antes de iniciar, certifique-se de ter instalado:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Acesso a uma instância Oracle (ex: `oracle.fiap.com.br` ou Oracle XE local)

---

### Passo 1 — Clonar o repositório

```bash
git clone https://github.com/Luccarm07/PetFlowAPI3.git
cd PetFlowAPI3
```

> Ajuste a URL acima caso o repositório tenha sido renomeado/migrado nesta sprint.

---

### Passo 2 — Configurar a string de conexão e o JWT

O repositório contém apenas `PetFlowAPI/appsettings.json`, com **placeholders** para as credenciais. Não é necessário copiar nenhum arquivo de exemplo.

Edite `PetFlowAPI/appsettings.json` (ou forneça os mesmos valores por variáveis de ambiente/User Secrets) com as credenciais do seu Oracle e uma chave JWT de desenvolvimento:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=SEU_HOST)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=SEU_USUARIO;Password=SUA_SENHA;"
  },
  "Jwt": {
    "Key": "substitua-por-uma-chave-secreta-com-no-minimo-32-caracteres",
    "Issuer": "PetFlowAPI",
    "Audience": "PetFlowClients",
    "ExpirationMinutes": 60
  }
}
```

> Para Oracle XE local, o `Data Source` geralmente é `localhost:1521/XEPDB1`.
> **Não utilize credenciais reais no repositório.** O `appsettings.json` versionado contém apenas placeholders (`SEU_USUARIO`/`SUA_SENHA`).

---

### Passo 3 — Executar o script SQL

Rode o script [`Database/2TDSPX_CodigoSql_PetFlow.sql`](./Database/2TDSPX_CodigoSql_PetFlow.sql) na sua instância Oracle antes de subir a aplicação.

---

### Passo 4 — Executar a aplicação

```bash
dotnet restore
dotnet run --project PetFlowAPI
```

A API estará disponível em:

```
http://localhost:5000
```

A documentação Swagger estará disponível em:

```
http://localhost:5000/swagger
```

Os endpoints de health check estarão disponíveis em:

```
http://localhost:5000/health
http://localhost:5000/health/ready
http://localhost:5000/health/live
```

---

### Resumo rápido

```bash
git clone https://github.com/Luccarm07/PetFlowAPI3.git
cd PetFlowAPI3
# Edite PetFlowAPI/appsettings.json e substitua os placeholders pelas configurações locais
# Execute o script Database/2TDSPX_CodigoSql_PetFlow.sql no Oracle
dotnet restore
dotnet run --project PetFlowAPI
# Acesse: http://localhost:5000/swagger
```

---

## 🧪 Testes Automatizados

O projeto `PetFlowAPI.Tests` (xUnit) cobre a aplicação com testes unitários e de integração, seguindo o padrão **AAA (Arrange-Act-Assert)**.

### Executar os testes

```bash
dotnet test
```

### O que é testado

| Arquivo | Tipo | Cobre |
|---------|------|-------|
| `RewardPointCalculatorTests.cs` | Unitário | Regra de cálculo de pontos de recompensa, incluindo casos de erro (multiplicador/pontos negativos) |
| `AuthTests.cs` | Unitário + Integração | Hash/verificação de senha; cadastro → login → emissão de token JWT; erros 400/401; acesso às rotas continua público nesta versão |
| `HealthChecksTests.cs` | Unitário | `ExternalServicesHealthCheck` com dependências mockadas (Moq) |
| `ApiIntegrationTests.cs` | Integração | Health checks via HTTP real, propagação de `X-Correlation-ID`, resposta 404 para rota inexistente |

Os testes de integração usam `WebApplicationFactory<Program>` com um banco **InMemory** (`ApiFactory.cs`), isolando os testes de uma instância Oracle real.

A suíte atual possui **17 testes automatizados**. Na execução validada para esta Sprint, o resultado foi **17 aprovados, 0 falhas e 0 ignorados**.

### Coleta opcional de cobertura

O projeto inclui `coverlet.collector`. Para gerar o arquivo de cobertura no diretório `TestResults`, execute:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

A rubrica desta Sprint não define um percentual mínimo de cobertura; o comando acima é disponibilizado como evidência complementar da abrangência dos testes.

---

## 🖐️ Testes Manuais da API

Os endpoints também podem ser testados via **Swagger UI**, **Postman** ou **Insomnia**.

Fluxo sugerido respeitando as dependências de FK:

1. `POST /clinics` — cadastrar uma clínica
2. `POST /plans` — criar um plano vinculado à clínica
3. `POST /tutors` — cadastrar um tutor (rota pública)
4. `POST /auth/login` — autenticar o tutor e verificar a emissão do token JWT
5. `POST /pets` — cadastrar um pet vinculado ao tutor
6. `POST /subscriptions` — assinar um plano para o pet
7. `POST /health-events` — registrar eventos de saúde para o pet
8. `POST /coupons` — criar um cupom
9. `POST /redeems` — resgatar o cupom usando pontos do tutor
10. Testar filtros: `GET /health-events?petId=1&status=REALIZADO`
11. Testar paginação: `GET /tutors?page=0&size=5`
12. Testar atualização de status: `PUT /subscriptions/1/status?status=ENCERRADO`
13. Testar observabilidade: `GET /health`, `GET /health/ready`, `GET /health/live`

---

## 🔐 Autenticação (JWT) — infraestrutura opcional

O projeto possui infraestrutura de autenticação JWT como funcionalidade complementar: cadastro de tutor com senha protegida por hash, login e emissão de `accessToken`. **Nesta versão da Sprint 3, as rotas de negócio não estão protegidas por `[Authorize]`**; portanto, possuir um token não é requisito para acessar os endpoints de negócio.

Isso é intencional nesta versão: a autenticação foi preparada para evolução futura, mas a proteção das rotas não faz parte do escopo obrigatório desta Sprint.

### Componentes

- `PasswordService` — gera e valida o hash da senha.
- `TokenService` — gera tokens JWT.
- `AuthController` — disponibiliza `POST /auth/login`.
- `Microsoft.AspNetCore.Authentication.JwtBearer` — configura a validação JWT no container de serviços.

### Fluxo atual

1. `POST /tutors` — cadastra o tutor e armazena a senha em formato hasheado.
2. `POST /auth/login` — valida e-mail e senha.
3. Em caso de sucesso, retorna um `accessToken` JWT e os dados básicos do tutor.
4. As rotas de negócio permanecem públicas nesta versão.

**Exemplo de login:**
```json
{
  "email": "maria@email.com",
  "password": "senha123"
}
```

**Resposta de sucesso:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-09-01T12:00:00Z",
  "tutorId": 1,
  "name": "Maria Silva",
  "email": "maria@email.com"
}
```

A configuração fica em `appsettings.json`:
```json
"Jwt": {
  "Key": "substitua-por-uma-chave-secreta-com-no-minimo-32-caracteres",
  "Issuer": "PetFlowAPI",
  "Audience": "PetFlowClients",
  "ExpirationMinutes": 60
}
```

> **Importante:** não confundir a emissão de JWT com autorização de endpoints. A aplicação consegue emitir o token, mas a proteção efetiva das rotas com `[Authorize]` fica para uma evolução futura.

## ✅ Checklist da Sprint 3

Esta versão contempla os requisitos descritos para **Advanced Business Development with .NET**:

| Requisito | Implementação no projeto |
|---|---|
| **Health Checks** | `/health`, `/health/ready`, `/health/live`; banco Oracle via `AddDbContextCheck`; serviços externos via `ExternalServicesHealthCheck` |
| **Logging estruturado** | Serilog, níveis configurados, `TraceId`, `CorrelationId` e `X-Correlation-ID` |
| **Tracing e métricas** | OpenTelemetry para ASP.NET Core, `HttpClient`, runtime e métrica customizada `petflow.http.request.duration` |
| **Testes unitários** | xUnit + Moq para regras de domínio, segurança e health checks |
| **Testes de integração** | `WebApplicationFactory<Program>` + banco InMemory, cobrindo HTTP, autenticação/login, validação e erros |
| **AAA e organização** | Testes separados em `PetFlowAPI.Tests`, nomenclatura descritiva e fixture `ApiFactory` |
| **README** | Endpoints de monitoramento, execução da API, execução dos testes e funcionalidades da Sprint 3 documentados neste arquivo |

### Comandos principais para avaliação

```bash
# Restaurar dependências
dotnet restore

# Executar a suíte de testes
dotnet test

# Executar testes e coletar cobertura opcional
dotnet test --collect:"XPlat Code Coverage"

# Executar a API
dotnet run --project PetFlowAPI
```

## 🧭 Observações Finais

O PetFlow foi desenvolvido com foco em:

- **Arquitetura em camadas** clara: Controllers → DTOs → AutoMapper → Models → EF Core → Oracle
- **Tratamento de exceções Oracle** com mensagens amigáveis ao cliente (sem expor stack trace em produção)
- **Deleção em cascata manual** respeitando as restrições de FK do Oracle
- **Documentação automática** completa via Swagger/OpenAPI
- **Padrão REST** com verbos HTTP semânticos e códigos de status corretos
- **Observabilidade de ponta a ponta**: health checks, logging estruturado com correlação e tracing/métricas via OpenTelemetry
- **Qualidade garantida por testes automatizados** unitários e de integração, seguindo o padrão AAA
- **Infraestrutura de login e token JWT** já disponível, pronta para restringir rotas em sprints futuras (item opcional, fora do escopo desta sprint)

Desenvolvido como parte do **Challenge 2TDSPX — FIAP 2026**
