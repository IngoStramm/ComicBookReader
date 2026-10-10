# Histórico de versões

## [0.1.0](https://github.com/IngoStramm/ComicBookReader/releases/tag/v0.1.0) — 2026-10-10

Primeira versão pública para Windows x64.

### Adicionado

- Leitura local de CBZ e CBR, incluindo RAR4, RAR5 e RAR sólido, com imagens JPEG, PNG e WebP.
- Rolagem por teclado, roda e arraste da página; Espaço desce a distância configurada e avança ao chegar ao fim.
- Atalhos de navegação, largura, visão completa, tela cheia, barra de ferramentas e preferências.
- Classificação global automática, simples ou dupla; largura consistente nas páginas duplas.
- Preferências e progresso salvos por livro, com retomada da página e posição.
- Interface em português do Brasil e inglês, campos numéricos e ícone próprio de HQ.
- Executável portátil único e instalador por usuário, ambos com runtime incluído e licenças completas em Sobre.

### Validação

233 testes passaram. Os dois formatos executaram CBZ/RAR4/RAR5/sólido e retomada de leitura. Instalação, reinstalação e desinstalação foram verificadas, preservando preferências e conteúdo do usuário. O roteiro de interação física, suavidade e outros monitores/DPI está em [docs/manual-validation.md](docs/manual-validation.md).
