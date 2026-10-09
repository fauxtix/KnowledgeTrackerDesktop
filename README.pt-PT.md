# KnowledgeTrackerDesktop

O KnowledgeTrackerDesktop é uma aplicação para Windows que permite registar
problemas técnicos e as respetivas soluções, para as encontrar e reutilizar
mais tarde. Guarde notas de resolução, passos ordenados, excertos de código,
etiquetas, projetos e referências a ficheiros numa base de conhecimento SQLite
pessoal.

**[Read in English](README.md)**

## Funcionalidades

- Pesquise entradas e combine a pesquisa com filtros por projeto, etiqueta e
  favoritos.
- Ordene por data de resolução, título ou projeto.
- Crie, edite e elimine entradas com descrições em Markdown, comentários,
  código, tecnologias, etiquetas e ligações opcionais para o YouTube.
- Organize passos de resolução e anexos. Os anexos guardam os caminhos dos
  ficheiros; os ficheiros não são copiados para a base de dados.
- Consulte estatísticas de entradas, favoritos, projetos, etiquetas e
  atividade ao longo do tempo.
- Alterne o idioma da aplicação entre inglês e português (Portugal).

## Requisitos

- Windows.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- Uma chave de licença Syncfusion WinForms configurada na variável de ambiente
  `SYNCFUSION_LICENSE_KEY`. A aplicação verifica a chave ao iniciar.

Obtenha e utilize uma licença de acordo com os termos da Syncfusion. Não
adicione a chave ao repositório.

## Compilar e executar

Clone o repositório e compile a solução:

```powershell
git clone https://github.com/fauxtix/KnowledgeTrackerDesktop.git
cd KnowledgeTrackerDesktop
dotnet restore
dotnet build KnowledgeTrackerDesktop.slnx
```

Configure `SYNCFUSION_LICENSE_KEY` nas variáveis de ambiente do utilizador ou
do sistema e inicie a aplicação:

```powershell
dotnet run --project KnowledgeTrackerDesktop\KnowledgeTrackerDesktop.csproj
```

Para definir a chave temporariamente na sessão atual do PowerShell:

```powershell
$env:SYNCFUSION_LICENSE_KEY = "a-sua-chave-de-licenca"
dotnet run --project KnowledgeTrackerDesktop\KnowledgeTrackerDesktop.csproj
```

## Testes

Execute todos os testes:

```powershell
dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj
```

Execute um teste pelo nome do método ou da classe:

```powershell
dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj --filter "Name~NomeDoMetodoDeTeste"
dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj --filter "FullyQualifiedName~NomeDaClasseDeTeste"
```

Os testes usam xUnit e bases de dados SQLite temporárias para cenários de
persistência e migração.

## Dados e privacidade

A aplicação funciona localmente e não disponibiliza sincronização na nuvem.
Durante o desenvolvimento, a base de dados SQLite é criada como
`knowledge.db3` na raiz da solução. Nas versões publicadas, fica junto da
aplicação. No primeiro arranque, uma base de dados existente em
`%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3` é copiada para a nova
localização através da API de cópia de segurança do SQLite; o ficheiro original
é mantido.

A base de dados local e os ficheiros gerados durante a compilação são excluídos
do Git. Faça cópias de segurança da base de dados separadamente se quiser
preservar a sua biblioteca. Eliminar uma entrada ou uma referência de anexo
não elimina o ficheiro físico indicado pelo caminho do anexo.

## Documentação do projeto

- [Arquitetura](KnowledgeTrackerDesktop/ARCHITECTURE.md)
- [Normas de código](KnowledgeTrackerDesktop/CODING_STANDARDS.md)
- [Requisitos do produto](KnowledgeTrackerDesktop/PRD.md)
- [Diretrizes da interface](KnowledgeTrackerDesktop/UI_GUIDELINES.md)
- [Especificação de design da interface](KnowledgeTrackerDesktop/UI_DESIGN_SPEC.md)
