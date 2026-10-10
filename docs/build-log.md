# Registro de construção

Plano autorizado pelo comando `$build`. Registros abaixo contêm verificações executadas; nenhum teste manual é inferido de build ou cálculo.

## T01

SDK oficial 10.0.401 x64 instalado em pasta privada, SHA512 conferido e --info executado. Caminhos dos comandos atualizados. Pacotes permanecem fixados; integração será comprovada por T03/T07/T09.

## T02

global.json fixa 10.0.401; configurações centrais e solução vazia criadas. --info resolveu o global.json; restore executado com aviso esperado de solução ainda sem projetos.

## T03

Restore dos quatro projetos e build Release com warnings como erros passaram. xUnit 4 requer UseAppHost=true, corrigido conforme erro explícito do pacote. Nenhum runner vazio foi contado como teste.

## T04

Aplicativo net10.0-windows configurado para win-x64. Restore/build Release passaram; aplicação WPF mínima iniciou e encerrou com código zero.

## T05

RED observado por namespace de contratos ausente; GREEN: três testes reais de identidade, validade do buffer e lease independente passaram. Build Release sem avisos. Core sem bibliotecas de terceiros.

## T06

RED por classes ausentes observado; GREEN: ordenação/índice passaram (5 Core antes de Layout e 2 Integration). Build sem avisos após integrar Layout; números enormes, subpastas, duplicidades e ZIP vazio conferidos.

## T10

Execução antecipada independente após T05. RED observado por Core.Layout ausente; GREEN: 46 testes Core incluindo 41 de layout passaram, build sem avisos. Exemplos de escala, classificação, centros, overview, viewport e entradas inválidas verificados.

## T07

RED observado por decoder/opener ausentes; GREEN: 19 testes Integration passaram, inclusive JPEG/PNG/WebP, oito orientações EXIF, alpha premultiplicado, hash, fechamento concorrente e limites. Build Release sem avisos. APIs obsoletas e tokens de testes corrigidos sem supressões.

## T17

Implementação independente antecipada após T05; RED pelos modelos/resolver ausentes. GREEN: 51 casos de preferências dentro de 99 Core passaram; build completo agora validado. Precedência, snapshots separados, bindings e posições relativas conferidos.

## T18

RED observado por Storage/AtomicFileWriter ausentes. GREEN: 41 testes Integration totais (19 novos de JSON, 19 anteriores e 3 RAR) passaram; build sem avisos. Backup, corrupção preservada, futuro read-only, cancelamento e escrita interrompida conferidos.

## T09

Três casos reais RAR passaram dentro de 41 Integration: RAR4/RAR5 não sólidos e RAR5 sólido via Reader sequencial. SHA e cinco pixels JPEG independentes conferidos, arquivo liberado/imutável, JPEG materializado lido duas vezes. Fixtures MIT e limites do caso documentados.
