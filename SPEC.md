# Spec: ComicBookReader

Status: especificação proposta para revisão. O mapa de módulos foi aprovado; este documento não representa implementação ou teste do aplicativo.

## Objetivo

Criar um leitor de HQs para Windows que permita ler arquivos locais CBZ/CBR em tela cheia com rolagem contínua pelo teclado e tamanho de leitura consistente. Imagens duplas devem preservar a escala de uma página individual. Um comando temporário de visão completa permite conferir a composição e retornar ao ponto de leitura.

Nome público confirmado pelo usuário: **ComicBookReader**. Raiz documental: `C:\Users\ingos\Documents\LeitorHQ`. Os nomes dos projetos de código usarão `ComicBookReader`.

## Requisitos confirmados e premissas

| Origem | Decisão |
| --- | --- |
| Usuário | Ler habitualmente em tela cheia. |
| Usuário | Segurar ↑/↓ deve produzir movimento suave. Atingir uma borda mantém a mesma página, inclusive se a seta for pressionada novamente. |
| Usuário | Trocar de página somente por comandos separados dos de rolagem. |
| Usuário | Adaptar o tamanho de leitura a diferentes arquivos e scans, reduzindo ajustes repetidos. |
| Usuário | Considerar paisagem um indício de imagem dupla e compensar sua escala, mantendo a imagem inteira. |
| Usuário | Oferecer um atalho para ver a imagem completa e retornar ao zoom anterior. |
| Proposta técnica | C#/.NET 10, WPF, Windows 11 x64, interface em português. |
| Proposta de interação | Page Down/Page Up para navegação; V para visão completa; F11 para tela cheia; atalhos configuráveis. |
| Proposta de interação | Largura inicial de uma página igual à área de leitura; ajuste manual relativo à largura da tela, salvo por livro. |

## Escopo da primeira versão

Abrir CBZ/CBR por seletor ou arrastar e soltar, ordenar páginas naturalmente, decodificar JPEG/PNG/WebP, navegar, rolar, ajustar o tamanho, corrigir a classificação simples/dupla, consultar visão completa e recuperar preferências/progresso. BMP/GIF/TIFF e animação não são requisitos desta primeira versão.

Biblioteca de coleções, serviços online, PDF/DjVu, extensão do Explorer, impressão, filtros avançados de cor, modo manga e apresentação de duas imagens distintas lado a lado ficam fora deste primeiro escopo. Uma imagem dupla continua sendo uma única página do índice.

## Módulos e documentos

| ID | Spec | Responsabilidade |
| --- | --- | --- |
| `comic-files` | [SPEC-comic-files.md](SPEC-comic-files.md) | Arquivos, imagens, índice e recursos de leitura. |
| `reading-preferences` | [SPEC-reading-preferences.md](SPEC-reading-preferences.md) | Preferências, ajustes e progresso local. |
| `page-layout` | [SPEC-page-layout.md](SPEC-page-layout.md) | Classificação, escala, enquadramento e limites. |
| `reader-ui` | [SPEC-reader-ui.md](SPEC-reader-ui.md) | Janela, entrada, rolagem e comandos. |

Dependências e ordem dos módulos seguem o [mapa aprovado](CAPABILITY-MAP.md): arquivos e preferências → layout → interface. Isso é a ordem de dependências, não um plano de tarefas de implementação.

## Tecnologia e ambiente

| Componente | Base proposta | Motivo |
| --- | --- | --- |
| Plataforma | Windows 11 x64 | Corresponde ao computador examinado. |
| Linguagem/runtime | C# 14 / .NET 10 LTS | Base suportada para um aplicativo Windows novo. |
| Interface | WPF, alvo `net10.0-windows` | Entrada de teclado, imagens, comandos e layout adaptável. |
| Arquivos | SharpCompress 0.50.5 | Leitura ZIP/RAR, com estratégia própria para RAR sólido. |
| Imagens | SkiaSharp 4.153.1 | Decoder isolado da interface; validar JPEG/PNG/WebP com arquivos reais. |
| Persistência | System.Text.Json do .NET 10 | Arquivos locais versionados, sem servidor ou banco adicional. |
| Testes | xunit.v3 4.0.2, projetos executáveis | Testes de regras e integração executados pela CLI. |

As versões são a base consultada para esta spec, não prova de compatibilidade integrada. Fixar o patch do SDK no `global.json` e os pacotes/lockfiles ao preparar os projetos. Não adotar versões preliminares automaticamente.

O .NET 10 é LTS na [política oficial](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). WPF funciona em Windows e oferece gráficos, entrada e layout, conforme a [documentação oficial](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/). As versões de pacotes foram conferidas nas páginas dos publicadores: [SharpCompress](https://www.nuget.org/packages/SharpCompress), [SkiaSharp](https://www.nuget.org/packages/SkiaSharp) e [xUnit](https://www.nuget.org/packages/xunit.v3).

Verificação local: Windows 11 Home Single Language de 64 bits. O `dotnet` do PATH aponta para o host x86 7.0.9; também existe um host x64, mas nenhum SDK foi encontrado nos dois locais examinados. O SDK .NET 10 x64 será um pré-requisito da implementação; nenhum SDK ou pacote foi instalado nesta etapa.

## Estrutura proposta do projeto

```text
ComicBookReader.slnx
global.json
Directory.Build.props
Directory.Packages.props
.editorconfig
src/
  ComicBookReader.Core/
    Contracts/                 # erros comuns, sem dependências de terceiros
    ComicFiles/                 # contratos e índice de páginas
    Preferences/                # modelos de preferências e progresso
    Layout/                     # geometria sem objetos WPF
    Reading/                    # estado de comandos e rolagem testável
  ComicBookReader.Infrastructure/
    Archives/                   # adaptadores SharpCompress
    Imaging/                    # adapter SkiaSharp
    Storage/                    # persistência JSON
  ComicBookReader.App/           # WPF, janela e composição dos serviços
tests/
  ComicBookReader.Core.Tests/
  ComicBookReader.Integration.Tests/
  fixtures/                     # HQs sintéticas e fixtures redistribuíveis
docs/
  decisions/                    # decisões de implementação quando necessárias
  manual-validation.md          # procedimento e resultados de testes reais
SPEC.md
SPEC-*.md
CAPABILITY-MAP.md
```

Os quatro módulos lógicos não exigem quatro assemblies separados. Core concentra contratos/regras, Infrastructure seus adaptadores, e App a interface. Os projetos e diretórios de código acima ainda não existem.

## Comandos

Comandos previstos para executar na raiz do projeto, após disponibilizar o SDK x64 e criar os projetos. O caminho explícito evita usar o host x86 encontrado no PATH.

```powershell
rtk proxy "C:\Program Files\dotnet\dotnet.exe" --list-sdks
rtk proxy "C:\Program Files\dotnet\dotnet.exe" restore ComicBookReader.slnx
rtk proxy "C:\Program Files\dotnet\dotnet.exe" build ComicBookReader.slnx --configuration Release --no-restore -warnaserror
rtk proxy "C:\Program Files\dotnet\dotnet.exe" format ComicBookReader.slnx --verify-no-changes --no-restore
rtk proxy "C:\Program Files\dotnet\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
rtk proxy "C:\Program Files\dotnet\dotnet.exe" run --project tests/ComicBookReader.Integration.Tests/ComicBookReader.Integration.Tests.csproj --configuration Release
rtk proxy "C:\Program Files\dotnet\dotnet.exe" run --project src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Debug
rtk proxy "C:\Program Files\dotnet\dotnet.exe" publish src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/win-x64
```

Executar projetos de teste pela CLI é documentado pelo [xUnit](https://xunit.net/docs/getting-started/v3/getting-started). Validar esses comandos na preparação dos projetos; não foram executados aqui porque há somente documentos.

## Estilo de código

Nullable habilitado, quatro espaços, namespaces por arquivo, PascalCase em tipos/métodos e camelCase em parâmetros/variáveis. Usar tipos explícitos quando esclarecer unidades. Geometria testável usa `double`, distinguindo pixels do scan de DIPs da interface. Strings de interface em português; identificadores de código em inglês.

Exemplo de estilo e contrato, não uma implementação já criada:

```csharp
using ComicBookReader.Core.ComicFiles;

namespace ComicBookReader.Core.Layout;

public readonly record struct ViewportSize(double WidthDip, double HeightDip);
public enum PageKind { Single, Spread }

public interface IPageLayoutCalculator
{
    PageLayout CalculateReading(
        PixelSize image,
        ViewportSize viewport,
        PageKind kind,
        double preferredPageWidthRatio);
}
```

Descompactação/decodificação não ocorrem no thread da interface. Cancelamento usa `CancellationToken`; cancelar não é uma mensagem de falha. Exceções de entrada/IO são convertidas na borda em `ReaderException` com um `ReaderErrorCode` estável. Não expor tipos de bibliotecas de terceiros nos contratos Core.

## Estratégia de testes

Testar regras reais: ordenação, precedência de preferências, classificação, escala, limites, rolagem independente da repetição de teclado e restauração. Fixtures de integração devem incluir CBZ, CBR RAR4/RAR5, RAR sólido, JPEG/PNG/WebP, arquivos sem imagens e entradas inválidas. Fixtures RAR devem ter procedência conhecida; não presumir que uma biblioteca de leitura cria RAR para os testes.

Validar a janela WPF em execução no computador do usuário: tela cheia, foco, teclas pressionadas, diálogo aberto, alteração de monitor/DPI, páginas simples/duplas e alternância de visão completa. Testes de cálculo não comprovam suavidade visual. Registrar hardware, DPI, dimensões dos scans e resultados em `docs/manual-validation.md`.

Não há meta percentual de cobertura nesta spec. Todos os critérios de aceitação dos módulos precisam de evidência automática ou teste manual indicado. Não substituir testes de arquivos/estado por testes que apenas repetem a fórmula da implementação.

## Critérios globais de sucesso

| ID | Condição verificável |
| --- | --- |
| G-01 | Abrir CBZ e CBR, incluindo RAR5 e RAR sólido dos fixtures, com ordem de páginas correta. |
| G-02 | ↑/↓ mantidas movem a imagem continuamente; soltar ou perder foco encerra o movimento. Nenhuma seta troca a página. |
| G-03 | Page Up/Page Down mudam de página por comandos separados, sem avançar automaticamente ao chegar à borda. |
| G-04 | Com a mesma área de leitura/preferência, 1000×1500 aparece a 900×1350; 2000×1500 e 4000×3000 classificados como duplos aparecem a 1800×1350. |
| G-05 | V mostra a imagem inteira; V novamente, na mesma imagem e viewport, recupera escala e offsets com erro máximo de 1 DIP. |
| G-06 | Reiniciar recupera preferências, ajuste do livro e última página válida. Consultar visão completa não altera o ajuste salvo. |
| G-07 | Alterar DPI/monitor não distorce a imagem nem deixa offsets fora dos limites. |
| G-08 | Navegar rapidamente não permite que um carregamento antigo substitua a página escolhida depois. |
| G-09 | O CDisplayEx instalado, suas configurações e os arquivos de HQ permanecem intactos. |

## Limites de trabalho

- Sempre: seguir o mapa aprovado, preservar arquivos do usuário, manter critérios verificáveis e atualizar a spec quando o comportamento mudar.
- Revisar com o usuário antes: acrescentar funcionalidades fora do escopo ou mudar uma preferência confirmada de leitura.
- Nunca: editar o executável/configurações do CDisplayEx ou alterar uma HQ para simular o ajuste de visualização.

As dependências listadas são propostas nesta spec; sua adoção entra na revisão deste documento. Não há autorização implícita para publicar o software ou enviar arquivos pessoais a serviços externos.

## Perguntas abertas e revisão

Nenhuma preferência essencial de leitura está pendente. Defaults de velocidade e atalhos são propostas ajustáveis. A validação técnica de pacotes, memória e compatibilidade de arquivos ocorrerá durante a implementação. Confirmar o patch do SDK ao preparar o ambiente.

Esta spec deve ser revisada antes da fase de planejamento. As fases posteriores da skill produzem plano e tarefas; não foram iniciadas neste pedido de especificação.

## Base da investigação

Os detalhes do aplicativo legado e as preferências coletadas estão no [relatório de investigação](../CDisplayEx-investigacao/2026-10-09/RELATORIO.md). Não houve decompilação dos handlers nem medição da leitura original. As regras desta spec são decisões do produto novo, não afirmações de equivalência com os algoritmos do CDisplayEx.
