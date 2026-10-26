# Ajustes futuros do KotShop

Pontos da revisão de código de 26/10/2026. São melhorias para retomar aos poucos;
não é necessário concluir tudo antes de continuar aprendendo CSS.

## Prioridade: consistência dos dados exibidos

- [ ] **Definir explicitamente a cultura de formatação dos preços.**
  - Arquivos: `KotShop.Web/Data/ViewModels/Produto/ProdutoViewModel.cs` e, se a configuração for global, `KotShop.Web/Program.cs`.
  - Atualmente, `ToString("C2")` depende da cultura em execução. Definir `pt-BR` explicitamente na formatação ou configurar a cultura da aplicação.
  - O atributo HTML `lang="pt-br"` não configura a cultura do C#.
  - Verificar que o mesmo preço aparece como `R$ 1.999,90` independentemente da cultura padrão do servidor.

- [ ] **Definir a regra de arredondamento das parcelas antes de implementar pagamentos.**
  - Arquivo: `KotShop.Web/Data/ViewModels/Produto/ProdutoViewModel.cs`.
  - Exemplo atual: R$ 1.999,90 é exibido como 12 parcelas de R$ 166,66, cuja soma é R$ 1.999,92.
  - Decidir como distribuir os centavos entre as parcelas e como comunicar eventuais parcelas de valores diferentes na interface.
  - Criar testes para a regra escolhida: a soma das parcelas deve coincidir com o total, incluindo valores que não dividem exatamente por 12.
  - Ao evoluir esse cálculo, considerar manter valores monetários como `decimal` e separar a formatação de exibição da regra de cálculo.

- [ ] **Garantir uma ordem estável na listagem de produtos.**
  - Arquivo: `KotShop.Web/Components/Pages/Home.razor`.
  - Adicionar `OrderBy` à consulta, por nome ou ID, antes de materializar a lista.
  - Caso a ordenação seja por nome, usar o ID como desempate com `ThenBy`.

## Pequena simplificação

- [ ] **Reutilizar a connection string já validada.**
  - Arquivo: `KotShop.Web/Program.cs`.
  - Usar a variável `connectionString` tanto no registro de `ApplicationDbContext` quanto no de `IDbContextFactory<DataContext>`, evitando buscar a mesma configuração novamente.

## Organização quando fizer sentido

- [ ] **Extrair um componente `ProdutoCard.razor`.**
  - Origem: markup de cada produto em `KotShop.Web/Components/Pages/Home.razor`.
  - Receber um `ProdutoViewModel` por parâmetro; manter a consulta e a listagem na Home.
  - Preservar a estrutura semântica, as informações de pagamento e o comportamento responsivo.
  - É um exercício de organização, sem urgência para o tamanho atual da página.

## Direção para o projeto

Manter a estrutura simples enquanto o projeto é pequeno. Introduzir serviços,
repositórios ou novas camadas quando aparecer uma necessidade concreta, como
reutilizar consultas ou regras em várias funcionalidades.
