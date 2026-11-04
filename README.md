# KotShop

Loja em .NET 10 / Blazor, com PostgreSQL (catálogo e Identity) e armazenamento S3 via LocalStack.

## Estrutura por camada e feature

```text
KotShop.Domain/                  Entidades e regras, sem EF Core, ASP.NET ou AWS
  Produtos/                      Produto e política de pagamento
  Categorias/                    Categoria
KotShop.Application/             Casos de uso e contratos de dependências
  Produtos/
    ConsultarCatalogo/           Consulta da vitrine e DTO de produto
    AtualizarImagem/             Validação e fluxo de atualização da imagem
    IProdutoRepository.cs
  Abstractions/IFileStorage.cs
KotShop.Data/                    Persistência PostgreSQL e Identity
  Produtos/                     Repository e mapeamento do produto
  Categorias/                   Mapeamento da categoria
  Identity/                     Usuário e contexto de autenticação
  Persistence/                  Contexto do catálogo
  Migrations/{Catalogo,Identity}/
KotShop.Infrastructure/          Integrações externas
  Storage/                      Implementação S3 do contrato de armazenamento
KotShop.Web/                     Blazor, HTTP e composição das dependências
  Features/Produtos/            Home, ViewModel e endpoint de upload
  Components/Account/           Feature de autenticação e gestão da conta
KotShop.WebAdmin/                Painel administrativo Blazor com Tailwind CSS 4
  Components/Account/           Login administrativo e autenticação em dois fatores
  Components/Pages/             Visão geral e consulta de produtos
KotShop.Domain.Tests/            Testes das regras do domínio, por feature
KotShop.Application.Tests/       Testes dos casos de uso, por feature
KotShop.Infrastructure.Tests/    Regressões das migrations e do adaptador S3
```

As dependências apontam para dentro: `Application → Domain`; `Data → Application`; `Infrastructure → Application`. A Web registra os serviços com `AddApplication`, `AddData` e `AddInfrastructure`. Domain não possui dependências de pacotes. Application trabalha com streams e contratos próprios, sem `IFormFile`, `DbContext` ou SDK AWS.

A vitrine usa o caso de uso de consulta; somente o repository conhece as consultas EF Core. O endpoint de upload cuida de HTTP/autorização, e o caso de uso valida os bytes reais, limita a leitura a 5 MB e coordena armazenamento e atualização do produto. Se salvar a URL falhar, a compensação tenta remover o objeto com um token independente do cancelamento da requisição. Uma falha na compensação é registrada com a chave do objeto e preserva a exceção original.

Ao adicionar uma funcionalidade, crie sua pasta na camada pertinente e agrupe ali seus casos de uso, contratos e testes. Os mapeamentos EF usam `IEntityTypeConfiguration`, enquanto as entidades protegem alterações com construtores e métodos. Os valores monetários permanecem `decimal`; formatação em moeda pertence à Web. A regra atual de parcelamento é apenas uma apresentação estimada; a distribuição de centavos para cobrança continua no TODO.

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

## Painel administrativo (WebAdmin)

O `KotShop.WebAdmin` é um processo independente da vitrine, com .NET 10, Blazor Server, Flowbite disponível e Tailwind CSS 4 compilado no build. Inicialmente oferece visão geral, consulta de produtos, login/logout e verificação por autenticador ou código de recuperação para contas com 2FA habilitado. Não inclui ainda CRUD, upload ou gestão de usuários.

```sh
docker compose up -d postgres
cd KotShop.WebAdmin
npm ci
dotnet run --launch-profile http
```

O perfil HTTP usa `http://kotshop-admin.dev.localhost:5046`; o HTTPS usa `https://kotshop-admin.dev.localhost:7211`. A vitrine pode continuar em execução nas portas existentes. Para recompilar CSS durante a edição, execute `npm run css:watch` em outro terminal.

O painel reutiliza `ApplicationUser`, `ApplicationDbContext` e as migrations de `KotShop.Data`. Configure `ConnectionStrings:DefaultConnection` com o mesmo banco da vitrine. O WebAdmin não aplica migrations automaticamente: execute a vitrine em Development ou os comandos EF abaixo antes do primeiro acesso. Também pode habilitar `Database__ApplyMigrations=true` explicitamente em um único processo.

As contas são compartilhadas, mas os cookies e o nome da aplicação de Data Protection são próprios do painel. Entrar na loja não inicia uma sessão administrativa. O painel aceita contas confirmadas com a permissão `catalogo:escrever` ou a permissão master `administracao:total`. Não expõe cadastro público nem cria administrador/senha padrão ao iniciar normalmente.

### Criar o primeiro administrador master

Com o PostgreSQL e as migrations de Identity preparados (por exemplo, após iniciar a vitrine uma vez em Development), pare a depuração no Visual Studio. Abra um terminal na raiz da solução e execute:

```powershell
dotnet run --project KotShop.WebAdmin --no-launch-profile -- --environment Development --bootstrap-master
```

O comando pede um e-mail ainda não cadastrado, uma senha e sua confirmação. A senha não aparece durante a digitação: pressione Enter após cada entrada. Use pelo menos 12 caracteres com maiúscula, minúscula, número e símbolo. Não informe sua senha em argumentos de linha de comando, no Git ou no chat.

A execução cria uma conta com e-mail confirmado e as claims `permissao=administracao:total` e `permissao=catalogo:escrever`. Ela não abre um servidor web. Após a mensagem de sucesso, inicie os projetos normalmente pelo F5 e entre em `https://kotshop-admin.dev.localhost:7211` usando as credenciais escolhidas.

Esse procedimento pressupõe acesso confiável ao terminal e ao banco do ambiente. Ele não depende do envio de e-mail e não promove contas existentes. Caso o e-mail já esteja cadastrado, escolha outro. Se já houver um master, o comando recusa a operação sem criar outra conta ou redefinir senhas. Usuário e permissões são gravados na mesma transação; tentativas simultâneas no PostgreSQL são serializadas.

O master tem acesso a todas as funções administrativas atuais. Novas funcionalidades devem usar políticas que aceitem `administracao:total`; para áreas exclusivas do master, use `AdminAccess.MasterPolicy`. Uma nova política não herda automaticamente essa permissão.

### Conceder acesso limitado ao catálogo

Para outro administrador com acesso apenas ao catálogo, crie e confirme a conta pelo fluxo existente da loja e conceda a claim em uma rotina confiável com `UserManager<ApplicationUser>` (nunca em um endpoint público):

```csharp
var user = await userManager.FindByEmailAsync(email)
    ?? throw new InvalidOperationException("Usuário não encontrado.");
var result = await userManager.AddClaimAsync(user,
    new System.Security.Claims.Claim("permissao", "catalogo:escrever"));
if (!result.Succeeded)
    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
```

A rotina deve ser executada com acesso administrativo ao ambiente. Nenhuma conta existente recebe essa permissão automaticamente. A confirmação de e-mail dos cadastros comuns segue o fluxo/configuração atual da loja. Para revogar acesso, remova as permissões administrativas da conta (incluindo `administracao:total` e `catalogo:escrever` no caso do master) e atualize o security stamp com `UpdateSecurityStampAsync`; cookies e circuitos existentes passam pela revalidação periódica do Identity (até 30 minutos por padrão).

`KotShop.WebAdmin.Tests` verifica o redirecionamento de visitantes, bloqueio de contas comuns, acesso de administradores, renderização do catálogo, rotas públicas, ausência de cadastro e proteção antiforgery do logout, sem exigir PostgreSQL.

```sh
dotnet test KotShop.WebAdmin.Tests/KotShop.WebAdmin.Tests.csproj
```

O Dockerfile e o Compose existentes continuam executando a vitrine; o novo painel é iniciado separadamente com os comandos acima.

## Imagens de produtos

`POST /api/produtos/{id}/imagem` recebe `multipart/form-data` com o campo `arquivo`. Aceita PNG, JPEG e WebP, até 5 MB, verificando a assinatura do arquivo. O nome do objeto é gerado pela aplicação em `produtos/{id}/{guid}.{extensao}`.

O endpoint exige login, claim `permissao=catalogo:escrever` e o token antiforgery do formulário ASP.NET. A concessão dessa claim deve ser feita por um fluxo administrativo confiável usando `UserManager<ApplicationUser>.AddClaimAsync`; o cadastro público não concede essa permissão. Ainda não há tela administrativa de upload.

O upload grava no S3, atualiza `Produto.UrlImagem` e retorna `{ id, urlImagem }`. Se a gravação no banco falhar, tenta remover o objeto recém-enviado. Imagens anteriores são preservadas ao substituir a URL. A vitrine mostra a imagem quando a URL está preenchida. Produtos sem imagem continuam válidos.

A política local permite leitura pública somente em `produtos/*`; outros arquivos do bucket permanecem privados. `IFileStorage` também pode ser injetado para armazenar outros arquivos. A classe `S3FileStorage` usa o SDK AWS com endereçamento por caminho.

## Migrations PostgreSQL

As migrations PostgreSQL estão em `KotShop.Data`, separadas por contexto: `ApplicationDbContext` (Identity) e `CatalogoDbContext` (catálogo). Seus IDs foram preservados na reestruturação, permitindo iniciar com o volume PostgreSQL existente sem recriar o banco. Em uma instalação vazia, criam as tabelas e os produtos/categorias iniciais. Elas não transferem dados de SQL Server; preserve o banco antigo e planeje a importação separadamente caso existam dados a manter.

Com a ferramenta `dotnet-ef` 10 instalada, as atualizações também podem ser executadas manualmente:

```sh
dotnet ef database update --project KotShop.Data --startup-project KotShop.Web --context ApplicationDbContext
dotnet ef database update --project KotShop.Data --startup-project KotShop.Web --context CatalogoDbContext
```

Fora de Development, `Database:ApplyMigrations` é `false` por padrão; aplique as migrations na etapa de implantação ou habilite a configuração explicitamente em um único processo.

## Testes

```sh
dotnet test KotShop.slnx
```

Os testes de Domain e Application não usam banco, Docker ou rede. Cobrem nomes/preços inválidos, preservação da imagem diante de URL inválida, política de desconto e parcelamento, os três formatos de imagem, tamanho real do stream, produto inexistente, falhas no storage/banco, exclusão concorrente, cancelamento e falha na compensação. Os testes de Infrastructure verificam os scripts PostgreSQL (incluindo snapshots, passkeys e sequências iniciais) e o contrato do adaptador S3 com um cliente substituto, também sem contêineres.

Com o Compose rodando, execute também a verificação ponta a ponta no terminal, na raiz. O script funciona com Windows PowerShell 5.1 (incluído no Windows) e PowerShell 7:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Docker.ps1
```

Ela cria uma conta e um produto temporários, testa login, proteção do endpoint, upload, acesso público à imagem e a URL salva no PostgreSQL, e remove os dados de teste ao terminar.
