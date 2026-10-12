# KnowledgeTrackerDesktop

![Plataforma: Windows](https://img.shields.io/badge/plataforma-Windows-0078D4?style=flat-square&logo=windows)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
![Interface: Windows Forms](https://img.shields.io/badge/interface-Windows%20Forms-512BD4?style=flat-square)
![Armazenamento: SQLite](https://img.shields.io/badge/armazenamento-SQLite-003B57?style=flat-square&logo=sqlite)
[![Licença: MIT](https://img.shields.io/badge/Licen%C3%A7a-MIT-yellow.svg?style=flat-square)](LICENSE)

**[Read in English](README.md)**

O KnowledgeTrackerDesktop é uma aplicação para Windows que permite registar
problemas técnicos e as respetivas soluções, para as encontrar e reutilizar
mais tarde. Guarde notas de resolução, passos ordenados, excertos de código,
etiquetas, projetos e referências a ficheiros numa base de conhecimento SQLite
pessoal.



Este projeto está licenciado ao abrigo da [Licença MIT](LICENSE).

## Funcionalidades

- Pesquise entradas e combine a pesquisa com filtros por projeto, etiqueta e
  favoritos.
- Ordene por data de resolução, título ou projeto.
- Crie, edite e elimine entradas com descrições em Markdown, comentários,
  código, tecnologias e etiquetas. Associe à entrada um tutorial ou uma
  demonstração do YouTube para voltar a encontrar facilmente o vídeo que
  complementa a solução, junto das respetivas notas.
- Organize passos de resolução e associe ficheiros relacionados, como registos
  ou documentos de referência, para manter o material de apoio acessível junto
  da solução. Os anexos guardam os caminhos dos ficheiros; os ficheiros não
  são copiados para a base de dados.
- Abra uma galeria de imagens a partir da secção de anexos para consultar
  capturas de ecrã e imagens de apoio num carrossel e abrir diretamente o
  ficheiro selecionado.
- Pré-visualize um excerto de código numa janela dedicada só de leitura antes
  de o copiar, a par da ação Copiar no editor.
- Consulte estatísticas de entradas, favoritos, projetos, etiquetas e
  atividade ao longo do tempo.
- Alterne o idioma da aplicação entre inglês e português (Portugal).

## Separadores do editor de entradas

O editor divide uma entrada em quatro separadores para organizar a informação
por assunto e facilitar a sua consulta:

- **Geral** — título e metadados da entrada (projeto, data, tecnologias,
  etiquetas e estado de favorito), além da descrição em Markdown, código e
  comentários.
- **Anexos** — os ficheiros e imagens associados à entrada, com ações para os
  abrir ou remover e para consultar imagens numa galeria.
- **YouTube** — ligação para um vídeo que apoia ou demonstra a solução, guardada
  com a entrada para consulta posterior.
- **Resoluções** — passos de resolução ordenados que descrevem como o problema
  foi resolvido.

Os separadores são partes da mesma entrada, não registos separados. Use
**Guardar** para persistir a entrada e os respetivos passos de resolução em
conjunto.

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
Nas compilações Debug, a base de dados SQLite é criada como `knowledge.db3` na
raiz da solução. Nas compilações Release e versões publicadas, fica junto da
aplicação. No primeiro arranque, uma base de dados existente em
`%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3` é copiada para a nova
localização através da API de cópia de segurança do SQLite; o ficheiro original
é mantido. `KnowledgeTrackerApp` é o nome antigo da pasta LocalAppData,
mantido para preservar a compatibilidade com instalações existentes; não é o
nome atual do produto. A aplicação também continua a usar essa pasta para
guardar a preferência de idioma.

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
