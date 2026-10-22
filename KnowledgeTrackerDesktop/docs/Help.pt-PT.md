# Ajuda - Knowledge Tracker

## 1. Introdução

O **Knowledge Tracker** é uma aplicação para registar, organizar e consultar a informação do seu conhecimento. Permite guardar registos com descrição, notas, etiquetas, categorias, passos de resolução, código, anexos e ligações para encontrar rapidamente o que precisa.

## 2. Primeiros passos

1. **Abrir a aplicação** - Inicie o Knowledge Tracker. A janela principal apresenta a barra de navegação à esquerda e a área de conteúdo à direita.
2. **Explorar a Biblioteca** - A opção **Biblioteca** mostra os seus registos em formato de cartões. É o ponto de partida para consultar, criar, editar e gerir o seu conhecimento.
3. **Ver Estatísticas** - A opção **Estatísticas** apresenta informação agregada sobre os registos existentes.
4. **Alterar idioma** - Pode mudar entre Português e Inglês através dos botões PT/EN no fundo da barra de navegação.

## 3. Principais funcionalidades

### 3.1 Biblioteca

Na vista **Biblioteca** tem à disposição:

#### Cabeçalho
- **+ Novo registo** - Cria um novo registo no editor.
- **Importar** - Importa uma biblioteca a partir de um ficheiro JSON gerado pelo Knowledge Tracker.
- **Exportar** - Exporta toda a biblioteca atual para um ficheiro JSON.

#### Filtros e pesquisa
- **Pesquisa** - Filtra os registos pelo texto introduzido (a localização da origem também é pesquisada).
- **Favoritos** - Mostra apenas os registos marcados como favoritos.
- **Etiquetas** - Filtra por uma ou mais etiquetas.
- **Projeto** - Filtra os registos por projeto.
- **Origem** - Filtra os registos pela origem do conhecimento: pasta local, repositório Git, página web ou registos sem origem registada.
- **Ordenação** - Permite ordenar por mais recentes/mais antigos, título ou projeto.

#### Lista de cartões
Cada registo aparece sob a forma de cartão. A partir do cartão ou do seu menu de ações é possível, consoante o contexto:

- **Abrir/selecionar** - Clicar no cartão para o visualizar no painel de pré-visualização à direita.
- **Marcar/Desmarcar como favorito** - Adicionar ou remover o registo dos favoritos.
- **Abrir origem** - Quando o registo tem origem registada, o cartão mostra-a como ligação; clicar abre a fonte sem abrir o registo.
- **Duplicar registo** - Cria uma cópia do registo selecionado para facilitar a criação de um novo com base no existente.
- **Eliminar registo** - Remove o registo da biblioteca (esta ação pede confirmação).

### 3.2 Pré-visualização do registo

Ao selecionar um cartão, o painel à direita mostra uma pré-visualização só de leitura do registo com as seguintes ações (quando aplicáveis):

- **Editar** - Abre o registo no editor para alteração.
- **Ver código** - Abre a pré-visualização do excerto de código associado ao registo.
- **Ver anexos** - Abre a galeria de anexos/imagens associados ao registo.
- **Abrir ligação** - Se existir uma ligação do YouTube, permite abri-la no browser.
- **Abrir origem** - Se o registo tiver origem registada, abre a pasta ou a ligação para onde aponta.

### 3.3 Editor de registos (Criar/Editar)

Ao criar ou editar um registo tem acesso a diversos campos para organizar a informação:

- **Título** - Identificação clara do registo.
- **Projeto** - Associar o registo a um projeto específico.
- **Descrição / Notas** - Espaço para descrever o problema, contexto ou observações.
- **Etiquetas (Tags)** - Adicionar uma ou mais etiquetas para facilitar a pesquisa transversal.
- **Favorito** - Marcar como favorito para acesso rápido.
- **Passos de resolução** - Gerir os passos individualmente através do gestor de passos (`Gerir passos`), com descrição e ordenação.
- **Excerto de código** - Adicionar código com formatação para consulta futura.
- **Anexos / Imagens** - Adicionar ficheiros/imagens ao registo. Podem ser visualizados na galeria integrada.
- **Ligação YouTube** - Associar um vídeo de referência (se aplicável).
- **Origem** - Registar de onde veio o conhecimento: uma pasta local, um repositório Git ou uma página web, juntamente com a localização. A localização é um caminho absoluto de pasta para uma pasta local e um URL http(s) para os restantes.
- **Comentários** - Espaço para notas adicionais.

### 3.4 Importar e Exportar

- **Exportar** - Gera um ficheiro `.json` com toda a biblioteca. Útil para backup ou para migrar entre máquinas.
- **Importar** - Carrega um ficheiro de exportação `.json`. Caso existam registos com o mesmo conteúdo, a aplicação pergunta se pretende manter os existentes ou importar como cópias.

### 3.5 Estatísticas

A vista **Estatísticas** apresenta uma visão geral da sua biblioteca, com totais, distribuição por projeto/etiquetas, distribuição por origem e outras informações agregadas para perceber melhor a sua coleção de conhecimento. Clicar num projeto, numa etiqueta ou num tipo de origem leva-o à Biblioteca com o filtro correspondente já aplicado.

## 4. Dicas e truques

- **Use favoritos** - Marque os registos mais consultados como favoritos para filtrá-los rapidamente.
- **Seja consistente com etiquetas e projetos** - Facilita imenso a filtragem à medida que a biblioteca cresce.
- **Aproveite o preview** - Em vez de abrir sempre o editor, utilize o painel de pré-visualização à direita para consulta rápida.
- **Faça backups regulares** - Utilize a opção **Exportar** para guardar uma cópia da sua biblioteca.
- **Atalho para ajuda** - Prima **F1** em qualquer altura para abrir esta janela de ajuda (ou clique em **Ajuda (F1)** no menu lateral).

## 5. Perguntas frequentes (FAQ)

**P: Como altero o idioma da aplicação?**  
R: Utilize os botões PT/EN no fundo da barra de navegação lateral. A ajuda atualiza automaticamente para o idioma selecionado.

**P: Como adiciono um novo registo?**  
R: Na vista Biblioteca, clique em **+ Novo registo** no cabeçalho, ou utilize as ações disponíveis na interface.

**P: Consigo duplicar um registo existente?**  
R: Sim. Através das ações do cartão (ou consoante o contexto), existe a opção **Duplicar registo** para criar uma cópia editável.

**P: Onde ficam os meus dados?**  
R: A aplicação utiliza uma base de dados local (`knowledge.db3`). Em modo Debug fica na raiz da solução. Em builds não-Debug fica em `%LOCALAPPDATA%\KnowledgeTrackerApp\`.

**P: Posso abrir anexos/imagens e código a partir da pré-visualização?**  
R: Sim. Se o registo tiver código ou anexos, o painel de pré-visualização apresenta botões para os visualizar (pré-visualização de código ou galeria de imagens/anexos).

**P: Qual a diferença entre "Projeto" e "Origem"?**  
R: O projeto é o nome da coisa que o registo documenta. A origem é de onde veio o conhecimento em si: uma pasta local, um repositório Git ou uma página web. Ambos são opcionais e a origem é registada por entrada, não por projeto.

**P: O que acontece se a origem registada já não existir?**  
R: A origem é um registo, não uma garantia. A pasta pode ter sido movida ou a página removida, por isso ao abrir é indicado o alvo em falta em vez de falhar em silêncio. Pode então corrigir a localização no editor.

**P: Posso abrir esta ajuda sem usar o menu?**  
R: Sim, basta premir **F1**.

## 6. Créditos e Versão

- **Aplicação:** Knowledge Tracker
- **Versão:** 1.0.0
- **Autores:** fauxtix
- **Copyright:** © 2026 fauxtix

---

*Esta ajuda foi atualizada com base nas funcionalidades atuais da aplicação. Caso sejam adicionadas novas funcionalidades, este documento deverá ser atualizado.*
