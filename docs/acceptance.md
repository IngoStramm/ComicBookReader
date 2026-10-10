# Matriz de aceitação

Data: 10/10/2026. 233 testes passaram (158 Core, 75 Integration), build/format/restore bloqueado passaram. Portátil e instalador 0.1.0 estão em `artifacts/distribution`; o pacote anterior `artifacts/win-x64-drag` foi preservado. O checklist não declara concluída a aceitação visual. **Automático** indica evidência de regra/integração ou WPF com eventos roteados; **Parcial** mantém os casos humanos pendentes.

Nomes de testes abaixo identificam classes reais nos projetos `tests/ComicBookReader.Core.Tests` e `tests/ComicBookReader.Integration.Tests`. Execução da janela, ambiente e limites da evidência: [manual-validation.md](manual-validation.md). Achados: [review.md](review.md).

| ID | Estado | Evidência / limite |
| --- | --- | --- |
| CF-01 | Automático | `ZipComicSessionTests`, `RarComicSessionTests`; WPF dos quatro formatos. |
| CF-02 | Automático | `ArchiveCompatibilityTests`, `SolidRarSessionTests`; sólido real e RAR5 stored sintético. |
| CF-03 | Automático | `ZipComicSessionTests`: JPEG/PNG/WebP e oito orientações EXIF, pixels independentes. |
| CF-04 | Automático | Sessões/controller: background, descarte e cancelamento; leitura repetida/anterior. |
| CF-05 | Automático | `ZipPageIndexTests`, `NaturalEntryComparerTests`, `UnsupportedArchiveTests`: ordem, sem imagens, índices e formatos rejeitados. |
| CF-06 | Automático | `ZipComicSessionTests`, `SolidRarSessionTests`: limites, traversal, cancelamento e hashes. |
| CF-07 | Automático | `PageImageCacheTests`, `ReadPriorityTests`, `OpenerCacheTests`: orçamento, prioridade, leases e fonte liberada. |
| RP-01 | Automático | `PreferencesResolverTests`, `ReadingPreferencesTests`: defaults e precedência. |
| RP-02 | Parcial | Store e reinício real comprovam dados de leitura; interação de preferências/atalhos ainda humana. |
| RP-03 | Automático | `PreferencesStoreTests`, `PreferencesRecoveryTests`: interrupção, corrupção, backup. |
| RP-04 | Automático | `OverviewTests`, `ReadingPreferencesTests`; dois processos fechando em overview. |
| RP-05 | Automático | Resolver/store/save coordinator: conflitos, versão futura, snapshots fora de ordem. |
| RP-06 | Automático | `ReadingPositionTests`, `OverviewTests`: posições relativas e resize. |
| PL-01 | Automático | `PageLayoutCalculatorTests`: paisagem/retrato/quadrado e override. |
| PL-02 | Automático | `PageLayoutCalculatorTests`: cinco exemplos numéricos independentes da spec. |
| PL-03 | Automático | Layout/controller: imagem inteira, overflow e origem; smoke das duas resoluções duplas. |
| PL-04 | Automático | Layout/controller: offsets limitados e origem centralizada quando cabe. |
| PL-05 | Automático | Layout: visão completa dentro do viewport, proporção preservada e preferência intacta. |
| PL-06 | Parcial | Resize/âncora testados; outro monitor/DPI ainda não exercitado. |
| PL-07 | Automático | Layout: entradas não finitas/inválidas rejeitadas. |
| UI-01 | Automático | `ContinuousScrollTests`: 30/60/120 Hz, 600 DIPs e limite temporal. |
| UI-02 | Parcial | Estado e eventos WPF de release/foco testados; auto-repeat físico e diálogo durante tecla pressionada pendentes. |
| UI-03 | Parcial | Clamp e nova pressão na borda não mudam página em Core/WPF; teclado físico pendente. |
| UI-04 | Parcial | Índices, limites e código IsRepeat verificados; pressão física Page Up/Down pendente. |
| UI-05 | Automático | Overview e retomada real entre processos guardam escala/ponto original. |
| UI-06 | Automático | `ReaderLoadingTests`, `ReaderNavigationTests`, `ReadPriorityTests`; navegação concorrente WPF. |
| UI-07 | Parcial | Resize/WPF sem reframe do overlay passaram; outro DPI/monitor pendente. |
| UI-08 | Automático | Classificação e tamanho recuperados em testes e segundo processo. |
| UI-09 | Pendente humano | Dez segundos/cadência foram medidos fora da tela; percepção visual ainda não foi avaliada. |
| G-01 | Automático | CBZ/RAR4/RAR5/sólido no executável, além da ordem natural nos testes. |
| G-02 | Parcial | Cálculo e integração de eventos funcionam; movimento perceptual/teclado físico pendentes. |
| G-03 | Parcial | Navegação separada/bordas validadas; uma troca por pressão física ainda humana. |
| G-04 | Automático | Oráculos PL-02 e smoke com duas resoluções duplas. |
| G-05 | Automático | V/V na janela restaura layout/offsets exatos. |
| G-06 | Parcial | Reinício real recupera leitura; editar bindings e reiniciar ainda humano. |
| G-07 | Parcial | Resize e DIPs a 96 DPI comprovados; mudança real de DPI/monitor pendente. |
| G-08 | Automático | Geração tardia, cancelamento ignorado pelo decoder e navegação concorrente. |
| G-09 | Automático | Hashes preservados, acesso somente leitura; sem escrita no CDisplayEx/Registro. |
| Distribuição portátil | Automático | EXE sozinho/renomeado em caminho com espaços, CBZ/RAR4/RAR5/sólido e retomada, runtime incluído. |
| Instalador por usuário | Automático | Instalação em pt-BR, reinstalação em inglês, HKCU/atalhos conferidos e execução real da aplicação. |
| Desinstalação | Automático | Remove itens instalados e preserva preferências e conteúdo adicional do usuário. |
| Licenças incorporadas | Automático | Sobre pt-BR/en renderiza; SHA256 dos textos completos coincide nos dois formatos. |

A distribuição é uma versão local para avaliação. T30 permanece aberto até a aceitação humana de suavidade, teclado e monitor/DPI; esse estado não é substituído por aprovação do build.
