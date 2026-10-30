# 0003 — Associações de arquivos do ComicBookReader

Data: 2026-10-30. Escopo: instalador por usuário 0.3.0.

O instalador registra .cbz e .cbr em HKCU\Software\Classes com ProgIDs
próprios do ComicBookReader, comandos open que preservam o caminho completo
do arquivo, ícones, OpenWithProgids e capacidades sob
HKCU\Software\RegisteredApplications. As entradas são removidas na
desinstalação sem apagar outros aplicativos registrados para essas extensões.

O usuário pode escolher, durante a instalação, abrir Configurações do Windows
para tornar o ComicBookReader o leitor padrão. O instalador não grava o valor
UserChoice nem troca diretamente o padrão atual: desde o Windows 8, essa
decisão pertence ao usuário e deve ser confirmada pelo próprio fluxo do
Windows. A associação fica disponível no menu Abrir com e na página de
Aplicativos padrão mesmo quando a pessoa desmarca a tarefa.

Referências oficiais:

- [Microsoft — Default Programs](https://learn.microsoft.com/en-us/windows/win32/shell/default-programs): cadastro de capacidades, ProgIDs e associações disponíveis.
- [Microsoft — Managing Default Applications](https://learn.microsoft.com/en-us/windows/win32/shell/vista-managing-defaults): instalações não devem alterar padrões por usuário; a escolha é do usuário.
- [Microsoft — Launch the Default Apps settings page](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-default-apps-settings): URI ms-settings:defaultapps?registeredAppUser=...
- [Inno Setup — ChangesAssociations](https://jrsoftware.org/ishelp/topic_setup_changesassociations.htm): notifica o Explorer sobre a atualização das associações durante instalação e desinstalação.

A escolha final via Configurações do Windows ainda requer verificação manual em
um perfil isolado de instalação antes de uma próxima publicação.
