# Spec: page-layout

Produto: ComicBookReader. Status: proposta para revisão. Módulo do [mapa aprovado](CAPABILITY-MAP.md); requisitos comuns em [SPEC.md](SPEC.md).

## Objetivo

Manter uma largura confortável de página em arquivos de resoluções diferentes, compensar imagens duplas e calcular enquadramentos sem alterar os pixels da HQ. Consumidor: `reader-ui`. Entradas: dimensões de `comic-files` e preferência/classificação resolvidas por `reading-preferences`.

## Classificação

Dimensões são as da imagem após normalizar sua orientação. Com classificação Auto, largura maior que altura significa Spread; retrato ou quadrado significa Single. Essa é a heurística inicial solicitada pelo usuário, não análise dos quadros nem garantia de que toda imagem horizontal representa duas páginas.

Override manual Single/Spread prevalece sobre a heurística e fica salvo por livro/PageKey. Auto retoma a heurística. Não dividir, recortar, duplicar a entrada ou gerar arquivos novos para uma imagem dupla. Inferência por proporção predominante do livro fica para uma evolução posterior, caso as correções manuais indiquem necessidade.

## Escala e dimensões

Pixels pertencem ao scan. Dimensões do viewport e offsets usam DIPs da interface. O consumidor fornece a área efetivamente disponível para leitura, descontadas margens fixas e controles que ocupam espaço permanente. Não depender da DPI metadata do arquivo para o tamanho visual de leitura.

Para largura/altura do scan `Iw/Ih`, viewport útil `Aw/Ah`, preferência `r` e número de páginas da imagem `n` (1 Single, 2 Spread):

```text
referenceWidth = Iw / n
scale = (r × Aw) / referenceWidth
displayWidth = Iw × scale
displayHeight = Ih × scale
maxOffsetX = max(0, displayWidth − Aw)
maxOffsetY = max(0, displayHeight − Ah)
```

Escala é DIPs por pixel. Valores de entrada precisam ser positivos/finitos, e `r` estar na faixa da preferência validada. O ajuste inicial `r=1` faz cada página lógica ocupar uma largura útil. A altura não limita a escala no modo de leitura. Centralizar a imagem em cada eixo sem overflow; nos eixos com overflow, os offsets medem a distância a partir da borda inicial.

| Imagem | Classificação | Aw | r | Exibição esperada |
| --- | --- | --- | --- | --- |
| 1000×1500 | Single | 900 | 1 | 900×1350 DIPs |
| 2000×1500 | Spread | 900 | 1 | 1800×1350 DIPs |
| 4000×3000 | Spread | 900 | 1 | 1800×1350 DIPs |
| 2000×1500 | Single manual | 900 | 1 | 900×675 DIPs |
| 1000×1500 | Single | 900 | 0,8 | 720×1080 DIPs |

Isso mantém o tamanho geométrico aproximado de cada página, não mede tamanho de letras. Margens e proporções editoriais diferentes continuam aceitando ajuste manual.

## Visão completa e mudanças de viewport

Calcular `overviewScale = min(Aw/Iw, Ah/Ih)` e centralizar a imagem inteira. Esse cálculo é temporário; não altera `r`, classificação ou progresso. O módulo fornece a geometria da visão completa, enquanto `reader-ui` é dona do snapshot e da alternância de estado.

Todo offset é limitado ao intervalo de zero até seu máximo. Ao mudar tamanho/DPI do viewport, recalcular a escala com a mesma preferência e enquadrar uma âncora da imagem preservada pelo consumidor, limitada à área válida. Não multiplicar duas vezes pelo fator DPI do Windows.

## Contratos fornecidos

```csharp
public interface IPageLayoutCalculator
{
    PageKind Classify(PixelSize image, PageClassificationOverride manualOverride);
    PageLayout CalculateReading(PixelSize image, ViewportSize viewport,
        PageKind kind, double preferredPageWidthRatio);
    PageLayout CalculateOverview(PixelSize image, ViewportSize viewport);
}
```

`PixelSize` vem do contrato de arquivos, `PageClassificationOverride` do contrato de preferências. `PageKind`, `ViewportSize` e `PageLayout` pertencem a este módulo. `PageLayout` contém escala, largura/altura de exibição, limites de offsets e origem de centralização nos eixos sem overflow. Funções são determinísticas e não dependem de IO, timers, controles WPF ou memória de pixels. Entrada inválida por consumidor é ArgumentException; não corrigir NaN ou dimensões zero silenciosamente.

## Estrutura, estilo e comandos

Regras em `src/ComicBookReader.Core/Layout`; testes em `tests/ComicBookReader.Core.Tests/Layout`. Seguir o [estilo comum](SPEC.md#estilo-de-código), explicitar unidades nos nomes e evitar arredondamento antes da apresentação.

Na raiz, após preparar os projetos:

```powershell
rtk proxy "C:\Program Files\dotnet\dotnet.exe" build ComicBookReader.slnx --configuration Release -warnaserror
rtk proxy "C:\Program Files\dotnet\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
```

## Estratégia de testes e critérios de aceitação

| ID | Caso e resultado esperado | Evidência |
| --- | --- | --- |
| PL-01 | Retrato/quadrado classificam Single; paisagem classifica Spread; override prevalece e Auto o remove. | Unidade parametrizada. |
| PL-02 | Todos os exemplos da tabela produzem dimensões esperadas com tolerância numérica de 0,001 DIP. | Unidade. |
| PL-03 | Scans proporcionais com resolução dobrada exibem a mesma dimensão visual; altura não reduz a escala de leitura. | Unidade com exemplos independentes. |
| PL-04 | Imagem sem overflow fica centralizada; em cada eixo com overflow, os offsets nunca saem do intervalo válido. | Unidade. |
| PL-05 | Visão completa contém toda a imagem tanto em viewport alto quanto largo e não altera a preferência recebida. | Unidade. |
| PL-06 | Mudança de viewport/DPI recalcula geometria preservando proporção e enquadramento válido. | Unidade e validação WPF da interface. |
| PL-07 | Zero, valores negativos, NaN e infinito são rejeitados nas entradas geométricas. | Unidade. |

## Limites e perguntas abertas

Sempre preservar proporções e manter a compensação de duas larguras; revisar com o usuário mudanças na heurística ou no significado do tamanho de leitura; nunca recortar a composição original para encaixá-la na tela. A tolerância acima verifica geometria, não percepção de legibilidade. Não há decisão funcional pendente neste módulo.
