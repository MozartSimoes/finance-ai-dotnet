# FinanceAI

API de controle financeiro em **.NET 10** que **classifica lançamentos automaticamente com Machine Learning (ML.NET)**.

Envie `"UBER *TRIP HELP.UBER.COM"` sem categoria e a API cadastra o lançamento em **Transporte**, informando que a categoria foi sugerida pelo modelo.

```http
POST /api/transactions
{ "description": "NETFLIX.COM", "amount": 55.90, "date": "2026-11-03" }

201 Created
{ "id": 1, "description": "NETFLIX.COM", "amount": 55.90, "type": "Expense",
  "categoryName": "Assinaturas", "categorySuggestedByAi": true, ... }
```

## Stack

| Área | Tecnologia |
|---|---|
| API | ASP.NET Core 10 (Web API com controllers), Swagger |
| Dados | PostgreSQL + **Dapper**, SQL escrito à mão |
| IA/ML | **ML.NET**: classificação multiclasse de texto (SdcaMaximumEntropy) |
| Testes | xUnit + Moq (26 testes) |
| Entrega | Docker, Docker Compose, GitHub Actions |

## Arquitetura

```
src/
├── FinanceAI.Api             Controllers, tratamento global de erros (ProblemDetails)
├── FinanceAI.Application     Regras de negócio, DTOs e contratos (interfaces)
├── FinanceAI.Domain          Entidades e exceções de domínio
├── FinanceAI.Infrastructure  Repositórios Dapper + PostgreSQL, script do banco
└── FinanceAI.ML              Treino, persistência e inferência do modelo
tests/
└── FinanceAI.UnitTests
```

- **As dependências apontam para dentro.** `Application` define as interfaces (`ITransactionRepository`, `ITransactionClassifier`), e `Infrastructure` e `ML` as implementam. Trocar o banco ou o modelo não muda nenhuma regra de negócio (inversão de dependência, o "D" do SOLID).
- **Controllers sem regra e sem try/catch.** As regras ficam nos services. Exceções de domínio (`NotFoundException`, `ValidationException`, `DuplicatedException`) são convertidas em respostas 404/400/409 por um `IExceptionHandler` central.
- **Dapper.** Escritas recebem entidades; leituras retornam modelos de resposta direto do SQL. A listagem usa `COUNT(*) OVER()` para trazer a página e o total numa única consulta, e um índice em `(date, category_id)` atende o filtro por período e o resumo mensal.

## Como o modelo funciona

1. **Dados:** [`training-data.csv`](src/FinanceAI.ML/Data/training-data.csv) traz 123 descrições no estilo de extrato bancário, rotuladas em 10 categorias.
2. **Pipeline:** `FeaturizeText` (n-gramas de palavras e caracteres, que lidam com códigos como `IFD*` e `UBER *TRIP`) → `SdcaMaximumEntropy`, que devolve uma probabilidade por categoria.
3. **Ciclo de vida:** na primeira execução o modelo é treinado e salvo em `models/transaction-classifier.zip`; nas seguintes ele só é carregado. O carregamento acontece na inicialização da API, não na primeira requisição.
4. **Confiança:** se a probabilidade ficar abaixo de 50%, o lançamento vai para **Outros** em vez de arriscar uma categoria errada. Cada lançamento registra se a categoria veio do usuário ou do modelo (`categorySuggestedByAi`).
5. **Qualidade medida:** a validação cruzada de 5 partes, só com descrições que o modelo nunca viu (principalmente nomes de lojas novas), dá **~39% de acurácia**, contra 10% de um chute aleatório. Com tão poucos exemplos, o limite é a quantidade de dados: testei outros algoritmos e configurações de n-gramas e todos ficaram entre 37% e 41%. O teste automatizado falha se a acurácia cair abaixo de 30%.

**Próximos passos naturais:** usar as correções feitas pelos usuários como novos dados de treino (retreino periódico) e exportar o modelo em ONNX.

## Como rodar

**Com Docker (API + banco):**
```bash
docker compose up --build
```
Swagger em http://localhost:8080

**Localmente** (.NET 10 SDK):
```bash
docker compose up -d postgres
dotnet run --project src/FinanceAI.Api
dotnet test
```

As credenciais do `docker-compose.yml` e do `appsettings.json` servem só para o banco local de desenvolvimento.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET/POST | `/api/categories` | Listar / criar categorias |
| GET/PUT/DELETE | `/api/categories/{id}` | Consultar / alterar / excluir |
| GET | `/api/transactions?from&to&categoryId&page&pageSize` | Busca com filtros e paginação |
| POST | `/api/transactions` | Criar lançamento (sem `categoryId`, o modelo classifica) |
| GET/PUT/DELETE | `/api/transactions/{id}` | Consultar / alterar / excluir |
| POST | `/api/transactions/classify` | Mostra a categoria prevista, sem salvar |
| GET | `/api/reports/monthly-summary/{ano}/{mes}` | Receitas, despesas, saldo e totais por categoria |

## Uso de IA

O projeto foi desenvolvido com o **Claude Code** (Anthropic) como par de programação.

**Onde a IA ajudou:** estrutura da solução, primeira versão do código e dos testes, configuração de Docker e CI, e o rascunho deste README.

**Meu papel:** definir o escopo e as decisões de arquitetura, revisar o código, rodar e depurar localmente no Visual Studio e validar os resultados com o banco real.

**O que foi corrigido ou decidido diferente do que a IA propôs:**
- **Meta de acurácia do modelo:** o primeiro teste exigia acurácia de 50%, e a validação cruzada mostrou ~39%. A IA tentou outro algoritmo (`LbfgsMaximumEntropy`) com n-gramas customizados, e o resultado piorou para ~10%. Comparando várias configurações, todas ficaram entre 37% e 41%: o limite era a quantidade de dados, não o algoritmo. Voltei ao pipeline mais simples e ajustei o teste para 30% (3× o chute aleatório), documentando o motivo em vez de esconder o número.
- **Script do banco:** um ajuste automático quebrou o caminho do recurso embutido (`Persistence/schema.sql` virou `Persistenceschema.sql`), e a API não encontrava o script. Detectado ao rodar e corrigido.
- **Testes de ponta a ponta:** cada endpoint foi exercitado contra o PostgreSQL real (classificação, paginação, resumo mensal, respostas 400/404/409), sem confiar apenas nos testes unitários.

## Autor

**Mozart Mota Simões**, desenvolvedor .NET sênior e AWS Certified (Solutions Architect e Developer, Associate).
[LinkedIn](https://www.linkedin.com/in/mozart-mota-simoes/)
