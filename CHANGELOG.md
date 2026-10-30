# Histórico de versões

## 0.2.1 — 2026-10-30

### Corrigido

- Sentido de leitura agora está disponível diretamente na barra, ao lado do tipo de página. O seletor permanece sincronizado com as Preferências, aplica a mudança imediatamente e salva a escolha.
- Fechar o seletor devolve o foco à leitura. As opções são traduzidas para português e inglês e cabem na largura mínima da janela.

## 0.2.0 — 2026-10-30

### Alterado

- Espaço percorre as faixas da imagem na horizontal antes de descer e retornar ao início da faixa seguinte. Só avança após o último trecho da última faixa.
- A distância configurável do Espaço usa a largura visível no eixo horizontal e a altura visível no vertical, mantendo a sobreposição padrão de 10%.

### Adicionado

- Sentido de leitura em Preferências: esquerda para direita (padrão) ou direita para esquerda (mangá), persistido entre sessões.
- Páginas novas começam no topo e na borda correspondente ao sentido escolhido. Mudar o sentido mantém a altura e retorna ao início da faixa atual; reabrir mantém o ponto salvo.

### Validação

246 testes passaram (171 Core e 75 Integration), incluindo percurso completo nos dois sentidos, bordas, retomada e compatibilidade de perfis antigos. Entrada roteada na janela percorreu todas as faixas antes da troca de página; preferência e leitura recuperadas no segundo processo. Verificação humana de suavidade e outros monitores/DPI continua no roteiro existente.

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
