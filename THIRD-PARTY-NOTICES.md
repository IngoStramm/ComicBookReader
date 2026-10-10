# Avisos de terceiros

O pacote inclui as dependências abaixo. O instalador leva os textos integrais em `licenses/`. Nos dois formatos distribuídos, esses textos também são incorporados ao executável e podem ser consultados/copiados pelo botão Sobre; o portátil pode ser compartilhado sozinho. Builds de desenvolvimento incorporam apenas este resumo; `build-distribution.ps1` reúne as licenças completas dos pacotes resolvidos antes de publicar.

| Componente | Base usada | Licença / texto incluído |
| --- | --- | --- |
| SharpCompress | 0.50.5, commit `e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626` | MIT; `SharpCompress-LICENSE.txt` e `SharpCompress-README.md`, incluindo atribuições internas e Bouncy Castle. |
| SkiaSharp e NativeAssets.Win32 | 4.153.1 | MIT; `SkiaSharp-LICENSE.txt` e `SkiaSharp-THIRD-PARTY-NOTICES.txt`, com atribuições do Skia e componentes nativos. |
| .NET e Windows Desktop Runtime | Versão registrada no runtimeconfig, incorporado no portátil | Licenças dos dois pacotes runtime win-x64 e avisos adicionais do .NET copiados sem alterações para `licenses/` e incorporados ao executável; o pacote Windows Desktop não fornece arquivo separado de avisos. |

A licença SharpCompress foi conferida no [commit do pacote](https://raw.githubusercontent.com/adamhathcock/sharpcompress/e4193af83b6ec8bdd5fb4f6ebffc2315dd3fc626/LICENSE.txt); o texto coincide com o arquivo que já acompanha as fixtures. As licenças SkiaSharp e runtime são obtidas dos respectivos pacotes restaurados, sem reconstruir a lista de avisos nativos.

xUnit v3 4.0.2 e suas dependências pertencem apenas aos runners de teste e não são incluídos no executável distribuído. Microsoft.NET.ILLink.Tasks 10.0.12 é usado somente pelo SDK para analisar a publicação em arquivo único, sem trimming, e fica fora do runtime distribuído. As fixtures RAR de teste têm procedência/licença documentadas em [tests/fixtures/README.md](tests/fixtures/README.md) e não entram no pacote de leitura.

O código nativo e os recursos do CDisplayEx não são usados na distribuição.
