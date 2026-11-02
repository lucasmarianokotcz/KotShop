# KotShop

Loja em .NET 10 / Blazor, com PostgreSQL (catálogo e Identity) e armazenamento S3 via LocalStack.

## Executar com Docker

Com o Docker Desktop em modo Linux, execute na raiz:

```sh
docker compose up -d --build
```

- Aplicação: http://localhost:8080
- PostgreSQL: `localhost:5432`, banco/usuário/senha: `kotshop`
- LocalStack S3: http://localhost:4566, região `us-east-1`, access key/secret key: `test`
- Bucket criado automaticamente: `kotshop-arquivos`

O Compose aguarda o banco e a criação do bucket/política de leitura antes de iniciar a aplicação. As migrations dos dois contextos são aplicadas automaticamente nesse ambiente. `GET /health` informa se o processo web está respondendo.

```sh
docker compose logs -f web
docker compose down
```

O PostgreSQL usa volume nomeado e mantém os dados ao recriar os contêineres. O LocalStack 4.14.0 Community foi fixado para desenvolvimento sem token; os objetos S3 são efêmeros e devem ser reenviados após recriar o serviço. As versões atuais do LocalStack exigem autenticação; para usá-las, ajuste a imagem, configure `LOCALSTACK_AUTH_TOKEN` fora do Git e habilite persistência conforme o plano utilizado. As credenciais e o HTTP deste Compose são exclusivamente locais.

## Executar a aplicação fora do Docker

```sh
docker compose up -d postgres localstack
cd KotShop.Web
npm ci
dotnet run
```

Em `Development`, a aplicação aplica as migrations ao iniciar. A conexão padrão aponta para PostgreSQL local. Eventuais user-secrets ou variáveis que ainda contenham a conexão SQL Server devem ser atualizados: `ConnectionStrings:DefaultConnection`.

No Compose, o SDK usa `Storage:ServiceUrl=http://localstack:4566`, enquanto `Storage:PublicBaseUrl=http://localhost:4566` gera URLs acessíveis ao navegador. Ao acessar por outra máquina, ajuste a URL pública para o endereço do host Docker. Configurações podem ser sobrescritas por variáveis com `__`, por exemplo `Storage__PublicBaseUrl`.

## Imagens de produtos

`POST /api/produtos/{id}/imagem` recebe `multipart/form-data` com o campo `arquivo`. Aceita PNG, JPEG e WebP, até 5 MB, verificando a assinatura do arquivo. O nome do objeto é gerado pela aplicação em `produtos/{id}/{guid}.{extensao}`.

O endpoint exige login, claim `permissao=catalogo:escrever` e o token antiforgery do formulário ASP.NET. A concessão dessa claim deve ser feita por um fluxo administrativo confiável usando `UserManager<ApplicationUser>.AddClaimAsync`; o cadastro público não concede essa permissão. Ainda não há tela administrativa de upload.

O upload grava no S3, atualiza `Produto.UrlImagem` e retorna `{ id, urlImagem }`. Se a gravação no banco falhar, tenta remover o objeto recém-enviado. Imagens anteriores são preservadas ao substituir a URL. A vitrine mostra a imagem quando a URL está preenchida. Produtos sem imagem continuam válidos.

A política local permite leitura pública somente em `produtos/*`; outros arquivos do bucket permanecem privados. `IFileStorage` também pode ser injetado para armazenar outros arquivos. A classe `S3FileStorage` usa o SDK AWS com endereçamento por caminho.

## Migrations PostgreSQL

As migrations antigas de SQL Server foram substituídas por migrations iniciais PostgreSQL para `ApplicationDbContext` e `DataContext`. Elas criam um banco novo, incluindo os produtos e categorias iniciais, e não transferem dados de uma instalação SQL Server existente. Preserve o banco antigo e planeje a importação separadamente caso existam dados a manter.

Com a ferramenta `dotnet-ef` 10 instalada, as atualizações também podem ser executadas manualmente:

```sh
dotnet ef database update --project KotShop.Web --context ApplicationDbContext
dotnet ef database update --project KotShop.Web --context DataContext
```

Fora de Development, `Database:ApplyMigrations` é `false` por padrão; aplique as migrations na etapa de implantação ou habilite a configuração explicitamente em um único processo.

## Testes

```sh
dotnet test KotShop.Tests/KotShop.Tests.csproj
```

Os testes verificam os scripts PostgreSQL dos dois contextos, incluindo passkeys do Identity e sequências dos dados iniciais, além do envio S3 e da geração de URLs públicas. Não exigem contêineres.

Com o Compose rodando, execute também a verificação ponta a ponta no terminal, na raiz. O script funciona com Windows PowerShell 5.1 (incluído no Windows) e PowerShell 7:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Docker.ps1
```

Ela cria uma conta e um produto temporários, testa login, proteção do endpoint, upload, acesso público à imagem e a URL salva no PostgreSQL, e remove os dados de teste ao terminar.
