# Ambiente de construção

SDK: .NET 10.0.401 x64; host/runtime 10.0.12; Windows 11 10.0.26200.
Executável: `C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe`.

Instalação privada de desenvolvimento, por ZIP oficial; não altera o PATH nem substitui hosts existentes. O SDK inclui Microsoft.WindowsDesktop.App.

Origem: [download oficial .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) e [metadados oficiais](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json).
Asset: `dotnet-sdk-10.0.401-win-x64.zip`.
SHA-512 verificado: `24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430`.

Verificação executada: `rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" --info`.
Pacotes centralizados conforme spec; compatibilidade efetiva depende do restore e dos testes de codecs/arquivos das próximas fatias.

Decisão: usar caminho explícito desta instalação em lugar de Program Files, evitando privilégios administrativos e o host x86 no PATH.
