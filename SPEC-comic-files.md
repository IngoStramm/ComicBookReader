# Spec: comic-files

Produto: ComicBookReader. Status: proposta para revisão. Módulo do [mapa aprovado](CAPABILITY-MAP.md); requisitos comuns em [SPEC.md](SPEC.md).

## Objetivo

Disponibilizar páginas de CBZ/CBR locais em ordem determinística, com dados de imagem independentes da interface e sem modificar a HQ. Consumidores: `page-layout` usa dimensões; `reader-ui` usa índice e pixels.

## Comportamento

- Abrir um único arquivo local por vez, somente para leitura. Validar o conteúdo como ZIP/RAR; não confiar somente na extensão.
- Incluir imagens JPEG/PNG/WebP de subpastas. Ignorar diretórios, metadados e entradas auxiliares como `__MACOSX`.
- Ordenar o caminho completo por comparação natural, de forma que `1`, `2`, `10` não se tornem `1`, `10`, `2`. Empates usam comparação ordinal e posição original da entrada; números longos não podem estourar um inteiro.
- Índices internos começam em zero; a interface apresenta página 1 até N. Uma imagem dupla ocupa uma única entrada.
- Decodificar e normalizar a orientação EXIF antes de retornar dimensões. Retornar pixels BGRA32 com alpha premultiplicado e stride explícito. Os consumidores não recebem objetos SkiaSharp.
- Carregar a página pedida em trabalho de background; leitura de um arquivo grande não pode bloquear o thread WPF. Propagar cancelamento e não transformar cancelamento em falha de leitura.
- Arquivo sem imagens retorna erro específico. Uma imagem inválida mantém sua posição no índice e retorna erro de página; não renumerar silenciosamente as outras páginas.
- Arquivos com senha ou múltiplos volumes retornam uma mensagem de recurso não suportado nesta versão. ZIP, RAR4/RAR5 e RAR sólido sem senha precisam passar na matriz de testes.

## Contratos fornecidos

```csharp
namespace ComicBookReader.Core.ComicFiles;

public readonly record struct PixelSize(int Width, int Height);
public sealed record PageInfo(int Index, string PageKey, string EntryName);

public interface IComicSession : IAsyncDisposable
{
    BookInfo Book { get; }
    IReadOnlyList<PageInfo> Pages { get; }
    ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken);
}
```

`IComicOpener.OpenAsync(path, cancellationToken)` retorna uma sessão. `BookInfo` contém BookId, caminho canônico, tamanho e timestamp UTC do arquivo; a identidade é SHA-256 desses campos normalizados. Isso não exige ler/hashar todo o arquivo ao abrir. A mudança de caminho ou do arquivo produz outro BookId; migração automática de arquivos renomeados não faz parte desta versão.

`PageKey` identifica caminho da entrada mais seu ordinal de duplicidade no arquivo. `PageImage` é um recurso descartável com PixelSize, stride e memória de pixels válida até Dispose; o consumidor libera a referência quando deixa de usá-la. O adapter pode manter referências próprias para cache sem invalidar as referências entregues. `ReadPageAsync` valida o índice e não usa uma sessão já fechada.

Falhas de entrada usam `ReaderException`/`ReaderErrorCode` do contrato comum: FileUnavailable, InvalidArchive, UnsupportedArchive, NoImages, PageDecodeFailed e ResourceLimit. Não expor exceções de terceiros na interface. Índice inválido por um consumidor é erro de programação, não uma falha do arquivo.

## Adaptação, cache e limites

SharpCompress 0.50.5 é o adapter proposto. Sua [documentação de formatos](https://github.com/adamhathcock/sharpcompress/blob/master/docs/FORMATS.md) distingue acesso aleatório e Reader sequencial e indica suporte a RAR sólido pelo Reader. Para esses arquivos, materializar entradas de imagem em cache privado antes de oferecer acesso arbitrário, mantendo progresso e cancelamento. Não assumir que cada página de RAR sólido pode ser acessada isoladamente pela API de Archive.

O cache materializado usa nomes gerados, e não caminhos de extração fornecidos pelo arquivo; é exclusivo da sessão. Limite inicial proposto: 2 GiB no cache de entradas por sessão, 128 MiB por entrada codificada e 96 milhões de pixels por imagem decodificada. Validar dimensões e limites antes da alocação quando o decoder permitir. Ultrapassar o limite comunica ResourceLimit e permite abrir outro livro, sem ampliar limites silenciosamente.

Pré-carregar no máximo uma página anterior e uma seguinte quando não houver leitura prioritária. Cache de imagens adicionais tem orçamento inicial de 256 MiB, contabilizado por stride × altura, e descarta entradas sem uso. A página atual é contabilizada separadamente; isso não é uma promessa de limite total de memória do processo. Descartar buffers nativos e recursos do arquivo ao fechar a sessão. Cache temporário interrompido deve ser removido na próxima inicialização.

## Estrutura e estilo

Contratos/índice em `src/ComicBookReader.Core/ComicFiles`; adapters em `src/ComicBookReader.Infrastructure/Archives` e `Imaging`. Seguir o [estilo comum](SPEC.md#estilo-de-código); recursos têm dono e ciclo de vida explícitos, operações canceláveis levam sufixo Async. Não acoplar decodificação a BitmapSource ou a controles WPF.

## Comandos

Na raiz, após criar os projetos e disponibilizar o SDK:

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" build ComicBookReader.slnx --configuration Release -warnaserror
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Integration.Tests/ComicBookReader.Integration.Tests.csproj --configuration Release
```

## Estratégia de testes e critérios de aceitação

| ID | Caso e resultado esperado | Evidência |
| --- | --- | --- |
| CF-01 | CBZ com `1.jpg`, `2.jpg`, `10.jpg`, subpastas e XML tem somente imagens e ordem natural determinística. | Unidade e integração. |
| CF-02 | Fixtures CBR RAR4, RAR5 e RAR sólido oferecem as mesmas páginas esperadas e permitem voltar a uma página anterior. | Integração com arquivos de procedência conhecida. |
| CF-03 | JPEG/PNG/WebP e imagem EXIF rotacionada retornam dimensões/pixels corretos. | Integração com pixels de referência. |
| CF-04 | Cancelar a leitura/fechar sessão libera o arquivo e recursos temporários; outro livro pode ser aberto. | Integração. |
| CF-05 | Arquivos sem imagens, danificados, protegidos ou acima dos limites produzem erros específicos. | Integração. |
| CF-06 | Hash da HQ antes/depois da sessão é igual; entradas com caminhos relativos malformados não são gravadas fora do cache. | Integração. |
| CF-07 | Substituir rapidamente uma leitura respeita a prioridade/cancelamento; cache adicional não excede seu orçamento. | Integração instrumentada. |

Não afirmar suporte a um subtipo CBR sem fixture correspondente. Versionar fixtures pequenas redistribuíveis e documentar sua procedência.

## Limites e perguntas abertas

Sempre preservar a entrada e fechar sessões; revisar mudanças de formatos/limites na spec; nunca escrever uma página transformada dentro da HQ. Os limites são defaults técnicos propostos e deverão ser avaliados com scans representativos. Compatibilidade integrada das versões de decoder/archive permanece a validar; não bloqueia a redação deste contrato.
