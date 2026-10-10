# Fixtures RAR redistribuíveis

Incremento T09-A: `rar4.cbr`, `rar5.cbr`, `solid.cbr`, este documento e
`SHARPCOMPRESS-LICENSE.txt` (cinco arquivos). As três fixtures são cópias binárias
sem alteração; somente o nome/extensão local mudou. Não são HQs pessoais.

Origem: repositório oficial [SharpCompress, tag 0.50.5](https://github.com/adamhathcock/sharpcompress/tree/0.50.5),
commit fixo `e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626`, obtido em 2026-10-09.
O [LICENSE.txt desse commit](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/LICENSE.txt)
é MIT, copyright Adam Hathcock (2014); sua cópia integral acompanha as fixtures.
Não distribuir apenas os binários sem esse aviso. Nenhuma ferramenta proprietária
foi instalada ou usada para produzir arquivos.

| Arquivo local | Caminho upstream em `tests/TestArchives/Archives/` | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| `rar4.cbr` | [`Rar4.rar`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Archives/Rar4.rar) | 59998 | `9d9c261e50d3a84ab11a3701c7a736057fc970e59ca365885a41f60270c1875e` |
| `rar5.cbr` | [`Rar5.rar`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Archives/Rar5.rar) | 60066 | `3319de3e8a91a58d08d8a83f48ebae7e6b022c542b4a58775bab64f4063988a9` |
| `solid.cbr` | [`Rar5.solid.rar`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Archives/Rar5.solid.rar) | 60046 | `3043d68c5a81f46adcbb5393632d243944ead44eb383a677ac82e68afdde27cf` |

Conteúdo de leitura esperado: **uma página**, `jpg/test.jpg` (upstream RAR4 usa
separador `\`, normalizado pelo adapter). JPEG de 384 × 512 pixels, 40372 bytes
codificados, CRC32 `088814e3`, SHA-256 dos bytes codificados
`b251c7501fb0f55dd4a92feabe0a6f5733bc40a02679498155fae9b30138fc53`.
Esse hash e as dimensões foram obtidos do
[JPEG original do mesmo commit](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Original/jpg/test.jpg).
O original é usado como referência, sem acrescentar uma cópia ao projeto.

Todos também contêm `exe/test.exe` (45056 bytes) e `тест.txt` (15498 bytes), além
de diretórios vazios. RAR5 inclui `Empty` e o serviço `QO`. O leitor deve ignorar
essas entradas no índice de páginas. O executável de teste jamais deve ser
executado; não é necessário extraí-lo para disco.

Evidência binária independente da extensão, com CRCs de todos os headers
conferidos:

- `rar4.cbr`: assinatura `52 61 72 21 1a 07 00`; main-header flags `0x0000`;
  entradas comprimidas com método `0x33`, unpack version 29, sem solid flag.
- `rar5.cbr`: assinatura `52 61 72 21 1a 07 01 00`; archive flags `0x0000`;
  compression-info `0x0180` (método 3, algoritmo 0, sem solid flag).
- `solid.cbr`: assinatura RAR5; archive flags `0x0004`; primeiro texto com
  compression-info `0x0d80`, seguido pelo EXE e JPEG com `0x0dc0`.
  A flag `0x0040` nas últimas duas entradas exige continuidade do dicionário.
  É um sólido comprimido real, não apenas armazenamento com um header rotulado.

As flags RAR5 seguem a [especificação oficial RARLAB](https://www.rarlab.com/technote.htm).
A assinatura antiga identifica o formato RAR 1.5–4.x; o nome upstream `Rar4.rar`
complementa essa evidência, sem alegar distinguir o programa autor pela assinatura.

Essas fixtures comprovam os três containers com JPEG. PNG/WebP e EXIF são
comprovados pelas fixtures CBZ sintéticas do decoder compartilhado. Não há
evidência de PNG/WebP dentro de RAR, nem de navegação entre duas imagens distintas
nesses arquivos. Para sólido, o teste deve percorrer as entradas anteriores,
materializar o JPEG e ler os mesmos bytes materializados novamente; testar apenas
a primeira entrada não cobre a dependência sólida do JPEG.

O registro de APIs e os testes de integração efetivamente executados pertencem a
[0002-rar-evidence.md](../../docs/decisions/0002-rar-evidence.md).

## Fixtures para rejeição de recursos não suportados

Incremento T09-B: as três fixtures abaixo, atualização deste documento e
`docs/decisions/0002-rar-evidence.md` (cinco arquivos). Mesma origem, commit e
licença MIT do incremento T09-A.

| Arquivo local | Caminho upstream em `tests/TestArchives/Archives/` | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| `protected.cbr` | [`Rar5.solid.encrypted_headers.rar`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Archives/Rar5.solid.encrypted_headers.rar) | 542 | `33a826f901e2a42337f39ad11bf6721d70212f26dde2b1a345ca72bfc8135b32` |
| `protected-entries.cbr` | [`Rar5.encrypted_filesOnly.rar`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Archives/Rar5.encrypted_filesOnly.rar) | 60336 | `e5fb06af812633b246430f029a051cc462d00180a0df455a798644deccdc7d07` |
| `multivolume.part01.cbr` | [`Rar5.multi.part01.rar`](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/TestArchives/Archives/Rar5.multi.part01.rar) | 10240 | `ad9b6ac157e77ba2cc6b8be7eeba52a2c047a0ef2d06fa6ffa0c1d1ccce3a651` |

Resultado do aplicativo para cada uma: `UnsupportedArchive`, antes de tentar
decodificar uma página ou procurar outros volumes. Não confundir esses casos com
`InvalidArchive` ou `NoImages`.

- `protected.cbr`: assinatura RAR5; primeiro header de tipo 4 (encriptação dos
  headers), com CRC32 correto. O
  [teste upstream](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/SharpCompress.Test/Rar/RarEncryptedHeaderScanTests.cs)
  usa senha de teste `test`, três textos `f000.txt` a `f002.txt` e verifica sólido.
  Não é uma fixture de páginas; cobre a rejeição de headers protegidos antes do
  índice, inclusive quando o conteúdo não contém imagens.
- `protected-entries.cbr`: assinatura RAR5; main-header archive flags `0x0000`;
  primeira entrada contém extra record tipo 1 (encriptação de arquivo).
  Headers são legíveis, mas os dados são protegidos. O
  [teste upstream do Reader](https://github.com/adamhathcock/sharpcompress/blob/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/tests/SharpCompress.Test/Rar/RarReaderTests.cs)
  usa senha `test`; inclui o JPEG padrão, EXE e texto. O aplicativo rejeita sem
  solicitar senha.
- `multivolume.part01.cbr`: assinatura RAR5; main-header archive flags `0x0001`
  (volume), primeiro file-header flags `0x0013` (dados continuam no próximo
  volume), ambos com CRC32 correto. Apenas a primeira parte do conjunto upstream
  de seis volumes foi copiada: suficiente para provar detecção/rejeição; não é
  um conjunto completo nem evidencia extração multipart.
