# Ajustes futuros do KotShop

Pontos da revisão de código de 26/10/2026. São melhorias para retomar aos poucos;
não é necessário concluir tudo antes de continuar aprendendo CSS.

## Prioridade: consistência dos dados exibidos

- [ ] **Definir explicitamente a cultura de formatação dos preços.**
  - Arquivos: `KotShop.Web/Features/Produtos/ProdutoViewModel.cs` e, se a configuração for global, `KotShop.Web/Program.cs`.
  - Atualmente, `ToString("C2")` depende da cultura em execução. Definir `pt-BR` explicitamente na formatação ou configurar a cultura da aplicação.
  - O atributo HTML `lang="pt-br"` não configura a cultura do C#.
  - Verificar que o mesmo preço aparece como `R$ 1.999,90` independentemente da cultura padrão do servidor.

- [ ] **Definir a regra de arredondamento das parcelas antes de implementar pagamentos.**
  - Regra em `KotShop.Domain/Produtos/PoliticaPagamento.cs`; apresentação em `KotShop.Web/Features/Produtos/ProdutoViewModel.cs`.
  - Exemplo atual: R$ 1.999,90 é exibido como 12 parcelas de R$ 166,66, cuja soma é R$ 1.999,92.
  - Decidir como distribuir os centavos entre as parcelas e como comunicar eventuais parcelas de valores diferentes na interface.
  - Criar testes para a regra escolhida: a soma das parcelas deve coincidir com o total, incluindo valores que não dividem exatamente por 12.
  - Ao evoluir esse cálculo, considerar manter valores monetários como `decimal` e separar a formatação de exibição da regra de cálculo.

- [x] **Garantir uma ordem estável na listagem de produtos.**
  - Implementado por ID em `KotShop.Data/Produtos/ProdutoRepository.cs`.

## Pequena simplificação

- [x] **Reutilizar a connection string já validada.**
  - Implementado em `KotShop.Data/DependencyInjection.cs` para os dois contextos.

## Organização quando fizer sentido

- [ ] **Extrair um componente `ProdutoCard.razor`.**
  - Origem: markup de cada produto em `KotShop.Web/Features/Produtos/Home.razor`.
  - Receber um `ProdutoViewModel` por parâmetro; manter a consulta e a listagem na Home.
  - Preservar a estrutura semântica, as informações de pagamento e o comportamento responsivo.
  - É um exercício de organização, sem urgência para o tamanho atual da página.

## Direção para o projeto

Manter a organização por feature dentro das camadas descritas no README.
Adicionar abstrações quando servirem aos casos de uso; preservar testes de comportamento
e evitar repositórios genéricos ou camadas vazias sem necessidade concreta.
