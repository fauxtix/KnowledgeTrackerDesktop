# RUNTIME_ERROR_HANDOFF

Working/handoff note for the current change. MUST NOT be committed. Delete after the change is committed and pushed.

## Contexto

Repositório `C:\NewProjects\KnowledgeTrackerDesktop`.

**Fase 1 (concluída, ainda NÃO commitada)**: pesquisa/tema da biblioteca (debounce, campos de pesquisa
alargados, contador, estado sem resultados + limpar filtros, libertação de recursos dos cartões).
Verificado: `dotnet build KnowledgeTrackerDesktop.slnx` OK (0 warnings, 0 errors) e
`dotnet test` → 173 testes aprovados.

Ficheiros da Fase 1 pendentes de commit: `UI/Views/ctlMain.cs`, `UI/Views/EntryListFilter.cs`,
`UI/Views/EntryCard.cs`, `UI/AppLocalization.cs`, `KnowledgeTrackerDesktop.Tests/UI/EntryListFilterTests.cs`,
`KnowledgeTrackerDesktop.Tests/UI/LibrarySearchTests.cs` (novo).
Também untracked e preexistente (NÃO meu): `UI/Views/ctlStatistics.resx`.

## Tarefa atual — Fase 5: Separadores do editor + hierarquia do preview

Dois problemas de UX a corrigir sem perder as melhorias das Fases 1–4:

1. O editor de página única (Fase 4) exige demasiado scroll para alternar entre grupos.
   → Reintroduzir `TabControlAdv` com 5 separadores, reutilizando os cartões `CreateSection`
   da Fase 4 dentro de cada separador. Scroll por separador (não global). Restaurar
   `ApplyEditorTabTheme`/`DrawEditorTab` e o hook em `CarbonTheme.Apply`.
   Separações: General (Título|Projeto + Tecnologias|Data|Etiquetas|Favorito),
   Description (Markdown + Comentários), Resolutions (passos), Code (copiar/preview),
   Attachments and references (anexos + galeria + YouTube).
   Guardar/Cancelar/Voltar continuam no cabeçalho. `_errorLabel` passa para zona própria
   acima dos separadores (visível de qualquer separador); sem título → ativar separador General.
2. Botões do preview da Biblioteca sem hierarquia → `Edit Entry` passa a
   `ButtonRole.Primary`; `Open code`/`Open attachments (N)`/`Open video` ficam `Secondary`.
   Larguras fixas passam a AutoSize + padding.

Âmbito: `UI/Views/ctlAddEdit.cs`, `UI/Views/ctlEntryPreview.cs`, `UI/Theme/CarbonTheme.cs`,
`KnowledgeTrackerDesktop.Tests/UI/EditorStepsVisibilityTests.cs`,
`KnowledgeTrackerDesktop.Tests/UI/EditorScrollAndSizingTests.cs` (reescrito),
`KnowledgeTrackerDesktop.Tests/UI/EntryPreviewTests.cs` (novo teste hierarquia).

Não alterar: esquema SQLite, modelos, serviços, regras de negócio, Biblioteca (layout),
outros vistes. Preservar alterações das Fases 1–4 e ficheiros locais preexistentes
(`ctlStatistics.resx`, este ficheiro). Sem commits/pushes.

Estado: inspeção concluída (versão comitada pré-Fase 4 usava 4 separadores — confirmado via
`git show HEAD`). A apresentar o plano e executar.

### Verificação (Fase 5, concluída)

- `dotnet build KnowledgeTrackerDesktop.slnx` → Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj` →
  Passed! Failed: 0, Passed: 233, Total: 233, Duration: 12 s.
  (Fase 4 = 224; +6 testes novos da Fase 5, +3 reescritos.)
- `CarbonTheme.cs` voltou a ficar **idêntico ao commit** (a Fase 4 removeu o hook de
  separadores, a Fase 5 restaurou-o — anulam-se).
- Sem sessão Windows Forms neste ambiente → validação manual continua pendente (checklist
  no relatório). Não foi confirmada nenhuma inspeção visual real.

### Estado git (distinção)

Preexistentes (NÃO commitar, não são meus): `UI/Views/ctlStatistics.resx`, `RUNTIME_ERROR_HANDOFF.md`.
Fases 1–3.1 (preexistentes nesta sessão): `SchemaMigrator.cs`, `Program.cs`, `EntryCard.cs`,
`EntryListFilter.cs`, `ctlMain.cs`, respetivos testes, + ficheiros untracked
(`EntryOpenActions.cs`, `ctlEntryPreview.cs`, 3 exceções em `Data/`, 4 ficheiros de teste untracked).
Fase 4 (único ficheiro alterado nesta sessão que ainda não estava): nenhum — foi absorvida.
Fase 5 (alterados agora): `ctlAddEdit.cs`, `ctlEntryPreview.cs`,
`EditorStepsVisibilityTests.cs`, `EditorScrollAndSizingTests.cs` (novo/reescrito).

### Não feito (validação manual pendente)

Navegar entre os 5 separadores; guardar/cancelar a partir de qualquer separador;
hierarquia dos botões do preview (preenchido vs contorno) no tema Carbon;
alternar PT/EN com os separadores visíveis; redimensionar ao mínimo 1024x700.

---

## Fase 4 (concluída, ainda NÃO commitada): Redesign do editor de entradas (`ctlAddEdit`)

Âmbito: `UI/Views/ctlAddEdit.cs` (página única com secções em vez de separadores),
`UI/Theme/CarbonTheme.cs` (remover hook de separadores), `UI/AppLocalization.cs`
(novos rótulos de secção) e `KnowledgeTrackerDesktop.Tests/UI/EditorStepsVisibilityTests.cs`
(ajustar os testes que assumiam separadores).

Decisões do utilizador: página única com secções (sem separadores); descrição Markdown
apenas com indicação visual (sem botão de pré-visualização novo).

Estado: baseline confirmado antes das alterações — build 0/0, 223 testes aprovados.

### Progresso

- [x] Inspeção: `ctlAddEdit.cs`, testes de editor, `AppLocalization`, `CarbonTheme`,
      `ModernLayoutHelper`, `UI_DESIGN_SPEC` §5, integração Biblioteca/pré-visualização.
- [x] Reescrever `ctlAddEdit.cs` para página única com secções.
- [x] Adicionar chaves PT em `AppLocalization.cs`.
- [x] Ajustar `EditorStepsVisibilityTests.cs`.
- [x] Build + testes: build 0/0, 224 testes aprovados.

### Alterações

- `UI/Views/ctlAddEdit.cs`: substituído o `TabControlAdv` (4 separadores) por uma
  página única com scroll vertical e cartões de secção: Main information,
  Classification, Description (Markdown), Resolution steps, Code,
  Attachments and references, Comments. Removidos `_editorTabs`,
  `ApplyEditorTabTheme` e `DrawEditorTab`. Ações Guardar/Cancelar/Voltar
  continuam no cabeçalho (`_shell.SetHeaderActions`). Aplicada
  `Font = AppTheme.Fonts.Caption` às linhas de metadados para preservar a
  altura dos inputs (que antes herdavam o tipo de letra dos separadores).
- `UI/Theme/CarbonTheme.cs`: removida a chamada a `editor.ApplyEditorTabTheme()`.
- `UI/AppLocalization.cs`: novas chaves EN/PT — "Main information",
  "Classification", "Attachments and references", "Markdown is supported here.".
- `Tests/UI/EditorStepsVisibilityTests.cs`: os testes que assumiam separadores
  passaram a procurar os controlos nos descendentes do editor; novo teste
  `SectionsAppearInATopDownOrder`.

### Não feito (validação manual pendente)

Abrir o editor (nova/existente), editar/guardar por grupo, cancelar, passos,
anexos, redimensionar ao tamanho mínimo e alternar PT/EN.

---

## Fase 2 (concluída, não commitada): Segurança das migrações SQLite

Âmbito: `Data/SchemaMigrator.cs`, `Program.cs`, `UI/AppLocalization.cs`, testes de migração.
Sem alterar esquema, `CurrentVersion` (=1), formato/localização dos backups, nem a reparação de bases antigas.

### Alterações planeadas

1. `SchemaVersionTooNewException` (novo, `Data/`): lançada quando `user_version > CurrentVersion`,
   imediatamente após a leitura da versão — antes de backup, reparações, limpeza de órfãos ou escrita.
   Preserva o ficheiro incompatível.
2. `DatabaseBackupException` (novo, `Data/`): falha ao criar o backup obrigatório aborta a migração.
3. `SchemaMigrationException` (novo, `Data/`): falha de upgrade/limpeza *após* backup; transporta
   `BackupPath` para a mensagem de erro. Sem restauro automático.
4. `CreateBackup`: passa de `File.Copy` para `SQLiteConnection.Backup` (API já usada em
   `DatabasePath.CopyLegacyDatabaseIfNeeded`); recusa sobrepor um backup existente; devolve o caminho.
5. `Program.cs`: distinguir os três casos com mensagens localizadas (mantém `MessageBox` + `return`).
6. Testes novos em `KnowledgeTrackerDesktop.Tests/Data/SchemaMigratorTests.cs`.

### Progresso

- [x] Inspeção: `SchemaMigrator.cs`, `Program.cs`, `DatabasePath.cs`, `AppComposition.cs`,
      `SchemaMigratorTests.cs`, `TempDatabase.cs`, `AppLocalization.cs`, docs (README/AGENTS/PRD).
- [x] Confirmados os problemas da auditoria (downgrade silencioso; `File.Copy` sem tratamento).
- [x] Confirmado: `SQLiteConnection.Backup` existe em sqlite-net 1.9.172; `InternalsVisibleTo` existe;
      `System.IO.FileSystem.AccessControl` disponível sem novo pacote (ACL para falhar o backup em teste).
- [x] Criar as 3 exceções (`SchemaVersionTooNewException`, `DatabaseBackupException`, `SchemaMigrationException`).
- [x] Alterar `SchemaMigrator.cs` (guarda de versão, backup SQLite, encapsulamento de falhas).
- [x] Alterar `Program.cs` (3 casos + `ShowStartupError`).
- [x] Adicionar strings PT em `AppLocalization.cs`.
- [x] Adicionar 6 testes em `SchemaMigratorTests.cs`.
- [x] Build + testes.

### Verificação

- `dotnet build KnowledgeTrackerDesktop.slnx` → Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj` →
  Passed! Failed: 0, Passed: 179, Total: 179 (baseline da Fase 1 = 173; +6 novos).

### Como retomar

- Estilo: uma exceção por ficheiro em `Data/`.
- O teste de falha de backup usa uma ACL de negação de criação de ficheiros (NTFS/Windows);
  funciona no ambiente de CI Windows. Não requer pacote novo.
- Nota residual (fora de âmbito): uma tabela `Step` sem `KnowledgeEntryId` faz `HasOrphanedChildrenAsync`
  lançar `SQLiteException` (apanhado pelo caso genérico em `Program.cs`).
- Não commitar este ficheiro; apagá-lo após commit+push.
