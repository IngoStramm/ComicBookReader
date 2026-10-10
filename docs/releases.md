# Publicar versões

Repositório: [IngoStramm/ComicBookReader](https://github.com/IngoStramm/ComicBookReader). Downloads: [Releases](https://github.com/IngoStramm/ComicBookReader/releases).

Cada versão usa uma tag anotada `vMAJOR.MINOR.PATCH` e uma release com seus próprios executáveis. A primeira é `v0.1.0`. O número da tag, do projeto, do changelog e dos arquivos publicados deve coincidir. Preserve tags e downloads publicados; correções recebem uma nova versão.

## Preparar

1. Atualizar Version, FileVersion e AssemblyVersion em `src/ComicBookReader.App/ComicBookReader.App.csproj` e a entrada do CHANGELOG. Conferir as notas para o usuário e os links do README.
2. Executar restore bloqueado, build Release com warnings como erros, testes Core/Integration e format verify conforme o README.
3. Gerar os dois formatos com `scripts/build-distribution.ps1`. Os nomes são derivados da versão do executável.
4. Executar `scripts/verify-distribution.ps1 -Version MAJOR.MINOR.PATCH`. Conferir `result.json` da execução e os SHA256 de `artifacts/distribution/SHA256SUMS-MAJOR.MINOR.PATCH.txt`.
5. Revisar e commitar as mudanças. Publicar apenas código, documentação e fixtures redistribuíveis no Git; os executáveis ficam nos assets da release.

## Tag e release

Exemplo da primeira versão, com GitHub CLI autenticado e `origin` apontando para este repositório:

```powershell
rtk git tag -a v0.1.0 -m "ComicBookReader 0.1.0"
rtk git push --atomic origin main v0.1.0
rtk proxy gh release create v0.1.0 --repo IngoStramm/ComicBookReader --verify-tag --draft --title "ComicBookReader 0.1.0" --notes-file artifacts/release-notes-v0.1.0.md
rtk proxy gh release upload v0.1.0 artifacts/distribution/ComicBookReader-0.1.0-Portable.exe artifacts/distribution/ComicBookReader-0.1.0-Setup.exe artifacts/distribution/SHA256SUMS-0.1.0.txt artifacts/distribution/README-0.1.0.txt --repo IngoStramm/ComicBookReader
rtk proxy gh release edit v0.1.0 --repo IngoStramm/ComicBookReader --draft=false --latest
```

Preparar `release-notes-v0.1.0.md` antes de executar o exemplo. Publicar o rascunho somente após conferir tag/commit e nomes, tamanhos e hashes dos assets. Depois, baixar os arquivos publicados para uma pasta nova e comparar seu SHA256 com os arquivos validados localmente. `--verify-tag` impede criar uma tag remota implicitamente no commit errado.

## Corrigir uma publicação

Se um executável tiver defeito, manter o histórico e publicar a correção sob uma nova versão/tag. Indicar nas notas da versão afetada qual versão a substitui; retirar o destaque de mais recente se necessário. Reinstalação/desinstalação preservam os dados em `%LOCALAPPDATA%\ComicBookReader`.
