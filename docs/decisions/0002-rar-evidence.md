# 0002 — Evidência de containers RAR

Data: 2026-10-09. Escopo: T09, preparação das fixtures e pesquisa de APIs.

Manter SharpCompress **0.50.5**, conforme SPEC-comic-files. As fixtures reais de
RAR4, RAR5 e RAR5 sólido, com procedência MIT, hashes, assinaturas, flags e JPEG
de referência, estão em [tests/fixtures/README.md](../../tests/fixtures/README.md).
As cópias não foram alteradas e nenhuma HQ pessoal ou arquivo do CDisplayEx foi
usado. Os incrementos T09-A e T09-B têm cinco arquivos cada, registrados no README.

O sólido escolhido exige processamento das duas entradas anteriores ao JPEG:
arquivo de texto, EXE e então `jpg/test.jpg`, com dependência do dicionário
preservada. Materializar apenas as imagens é suficiente para o cache, mas o
Reader deve processar ou consumir corretamente todas as entradas anteriores.
Os testes não precisam gravar nem executar o EXE do arquivo de teste.

APIs confirmadas no código oficial do commit
`e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626`:

- [`RarArchive.OpenArchive(Stream, ReaderOptions)`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/src/SharpCompress/Archives/Rar/RarArchive.Factory.cs)
  abre streams seekable e retorna `IRarArchive`; `Entries` e
  `entry.OpenEntryStream()` permitem ler a página nos arquivos não sólidos.
  Não usar exemplos antigos com `RarArchive.Open`.
- [`RarReader.OpenReader(Stream, ReaderOptions)`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/src/SharpCompress/Readers/Rar/RarReader.cs)
  retorna `IReader`; usar `MoveToNextEntry()` e `OpenEntryStream()` para percorrer
  o sólido. Não usar exemplos antigos com `RarReader.Open`.
- [`ReaderOptions.ForExternalStream`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/src/SharpCompress/Readers/ReaderOptions.cs)
  deixa o stream aberto para o dono externo. O dono deve dispor explicitamente
  arquivo, archive/reader e entry streams. Não desativar a checagem de entrada
  incompleta para conseguir ler o primeiro volume isolado.
- [`AbstractReader`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/src/SharpCompress/Readers/AbstractReader.cs)
  permite abrir cada entry stream somente uma vez. Entradas sólidas puladas são
  descomprimidas pelo Reader para preservar continuidade. `Cancel()` encerra a
  operação e impede continuar para a próxima entrada; não substitui o contrato
  público de `CancellationToken` nem seu `OperationCanceledException`.
- [`RarArchive.IsEncrypted`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/src/SharpCompress/Archives/Rar/RarArchive.cs)
  consulta somente a primeira entrada não diretório. Para a política do produto,
  verificar encriptação em todas as entradas, inclusive auxiliares, e tratar
  headers encriptados durante a abertura. `RarVolume.IsMultiVolume` expõe a flag
  de volume e evita depender apenas da presença física de outra parte.

A [tabela oficial de formatos no mesmo commit](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/docs/FORMATS.md)
determina o Reader para RAR sólido. O cache privado deverá usar nomes gerados,
limites de sessão e cancelamento conforme a spec; as fixtures não autorizam
extração para caminhos definidos pelas entradas.

Verificação já realizada: SHA-256 e tamanho de todos os binários; assinaturas
RAR; CRC32 de todos os headers dos três arquivos de leitura; flags de sólido,
encriptação e volume nas fixtures de rejeição; JPEG upstream com SHA-256 e
dimensões 384 × 512 extraídas do marcador SOF. Nenhuma dessas inspeções é uma
alegação de que os testes C# de integração já passaram.

Validação de execução ainda necessária no runner de integração: ler os bytes do
JPEG via Archive em RAR4/RAR5; percorrer o sólido via Reader e conferir o mesmo
hash; reler os dados materializados; comprovar preservação dos hashes dos CBR e
liberação dos streams; retornar `UnsupportedArchive` nas três fixtures de
rejeição. Acrescentar a evidência efetiva após executar esses casos.

Limitações explícitas: uma única imagem por fixture; reler o JPEG materializado
cobre reutilização, mas não navegação entre duas imagens diferentes. JPEG é o
único codec contido nesses RARs; PNG/WebP/EXIF são exercitados pelo decoder
compartilhado em CBZ. Não foi testado sólido RAR4, sólido RAR7, nem extração
multivolume. Nenhum requisito CBR foi removido ou reduzido por essas limitações.
