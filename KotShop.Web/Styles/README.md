# CSS do KotShop

O projeto usa os componentes do Flowbite Blazor e os valores padrão do Tailwind CSS v4,
sem uma paleta ou fonte própria. A classe `font-sans` usa a família padrão do sistema.

## Primeiro uso

Com Node.js instalado, execute na pasta `KotShop.Web`:

```powershell
npm ci
dotnet build
```

O build do .NET gera `wwwroot/tailwind.css` automaticamente. Esse arquivo é gerado:
edite `Styles/tailwind.css` ou as classes dos componentes Razor, não o arquivo de saída.

## Durante o aprendizado

Em um terminal na pasta `KotShop.Web`, deixe o compilador acompanhando as alterações:

```powershell
npm run css:watch
```

Em outro terminal, rode a aplicação normalmente (`dotnet watch`). Para compilar só o CSS:

```powershell
npm run css:build
```

- `Styles/tailwind.css`: importação e configuração do Tailwind, sem substituir o tema padrão.
- `Components/Layout/MainLayout.razor`: header e largura compartilhada das páginas.
- `Components/Pages/Home.razor`: grid responsivo e componentes `Card` do Flowbite.
- `Components/App.razor`: classes globais de fundo, texto e fonte no `body`.

Exemplo: `p-6` define padding; `gap-6` separa os cards; `lg:grid-cols-3` cria três
colunas em telas grandes; `text-slate-900` usa a paleta padrão do Tailwind.

O CSS distribuído pelo Flowbite fornece os estilos internos dos componentes; o
Tailwind local gera as classes usadas pelo KotShop. Os componentes atuais são
estáticos. Componentes com eventos, como modais e dropdowns, devem ser usados em
páginas ou componentes com renderização interativa (por exemplo, `InteractiveServer`).

Documentação: https://flowbite-blazor.org/docs/
