using System.Diagnostics;
using System.Globalization;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Input;
using ComboBoxAdv = Syncfusion.Windows.Forms.Tools.ComboBoxAdv;

namespace KnowledgeTrackerDesktop.UI
{
    internal enum AppLanguage
    {
        English,
        Portuguese,
    }

    internal static class AppLocalization
    {
        private const string EnglishCulture = "en-US";
        private const string PortugueseCulture = "pt-PT";
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KnowledgeTrackerApp",
            "language.txt");

        private static readonly Dictionary<string, string> PortugueseText = new(StringComparer.Ordinal)
        {
            ["Your knowledge library will appear here."] = "A sua biblioteca de conhecimento aparecerÃ¡ aqui.",
            ["Library"] = "Biblioteca",
            ["Statistics"] = "Estatísticas",
            ["The language preference could not be saved."] = "Não foi possível guardar a preferência de idioma.",
            ["The language preference could not be saved."] = "Não foi possível guardar a preferência de idioma.",
            ["The Syncfusion license key was not found. Set SYNCFUSION_LICENSE_KEY in your User or Machine environment variables, then restart the application."] = "A chave de licença Syncfusion não foi encontrada. Defina SYNCFUSION_LICENSE_KEY nas variáveis de ambiente do utilizador ou do sistema e reinicie a aplicação.",
            ["The knowledge base could not be opened. Please try again."] = "Não foi possível abrir a base de conhecimento. Tente novamente.",
            ["Não foi possível abrir a base de conhecimento. Tente novamente."] = "Não foi possível abrir a base de conhecimento. Tente novamente.",
            ["The knowledge base was created by a newer version of KnowledgeTracker. Update the application and try again."] =
                "A base de conhecimento foi criada por uma versão mais recente do KnowledgeTracker. Atualize a aplicação e tente novamente.",
            ["A safety copy of the knowledge base could not be created, so no changes were made. Check that there is enough free disk space and try again."] =
                "Não foi possível criar uma cópia de segurança da base de conhecimento, pelo que não foram feitas alterações. Verifique se há espaço livre suficiente em disco e tente novamente.",
            ["The knowledge base could not be upgraded. Please try again."] =
                "Não foi possível atualizar a base de conhecimento. Tente novamente.",
            ["The knowledge base could not be upgraded. A safety copy was saved here:\r\n\r\n{0}"] =
                "Não foi possível atualizar a base de conhecimento. Foi guardada uma cópia de segurança em:\r\n\r\n{0}",
            ["Entries"] = "Entradas",
            ["New Entry"] = "Nova entrada",
            ["Edit Entry"] = "Editar entrada",
            ["Select an entry to preview it."] = "Selecione uma entrada para a pré-visualizar.",
            ["Description"] = "Descrição",
            ["Description"] = "Descrição",
            ["Step {0}"] = "Passo {0}",
            ["Open attachments ({0})"] = "Abrir anexos ({0})",
            ["Save"] = "Guardar",
            ["Cancel"] = "Cancelar",
            ["Back"] = "Voltar",
            ["Geral"] = "Geral",
            ["Resoluções"] = "Resoluções",
            ["YouTube"] = "YouTube",
            ["General"] = "Geral",
            ["Resolutions"] = "Resoluções",
            ["Title"] = "Título",
            ["Favorite"] = "Favorito",
            ["Project"] = "Projeto",
            ["Technologies"] = "Tecnologias",
            ["Date resolved"] = "Data de resolução",
            ["Not set"] = "Não definida",
            ["Main information"] = "Informação principal",
            ["Classification"] = "Classificação",
            ["Description (Markdown)"] = "Descrição (Markdown)",
            ["Markdown is supported here."] = "Aqui é suportado Markdown.",
            ["Attachments and references"] = "Anexos e referências",
            ["Code & Attachments"] = "Código e anexos",
            ["No resolution steps yet. Add steps to document how the issue was resolved."] = "Ainda não há passos de resolução. Adicione passos para documentar como o problema foi resolvido.",
            ["Ainda não há passos de resolução. Adicione passos para documentar como o problema foi resolvido."] = "Ainda não há passos de resolução. Adicione passos para documentar como o problema foi resolvido.",
            ["Manage steps"] = "Gerir passos",
            ["1 step"] = "1 passo",
            ["{0} steps"] = "{0} passos",
            ["+ Add step"] = "+ Adicionar",
            ["Code"] = "Código",
            ["Copy"] = "Copiar",
            ["Preview"] = "Pré-visualizar",
            ["Code preview"] = "Pré-visualização do código",
            ["Close"] = "Fechar",
            ["Open video"] = "Abrir vídeo",
            ["Open code"] = "Abrir código",
            ["Attachments"] = "Anexos",
            ["+ Add file"] = "+ Adicionar ficheiro",
            ["Open gallery"] = "Abrir galeria",
            ["Image gallery"] = "Galeria de imagens",
            ["No images to display."] = "Sem imagens para mostrar.",
            ["No attachments."] = "Sem anexos.",
            ["added {0}"] = "adicionado em {0}",
            ["Open"] = "Abrir",
            ["Remove"] = "Remover",
            ["Add attachment"] = "Adicionar anexo",
            ["All files (*.*)|*.*"] = "Todos os ficheiros (*.*)|*.*",
            ["Comments"] = "Comentários",
            ["The file could not be found at the recorded location:\r\n\r\n{0}"] =
                "Não foi possível encontrar o ficheiro na localização registada:\r\n\r\n{0}",
            ["Attachment"] = "Anexo",
            ["The file could not be opened."] = "Não foi possível abrir o ficheiro.",
            ["The link could not be opened."] = "Não foi possível abrir a ligação.",
            ["A title is required."] = "É necessário indicar um título.",
            ["The knowledge entry could not be saved. Please try again."] =
                "Não foi possível guardar a entrada. Tente novamente.",
            ["Discard unsaved changes?"] = "Descartar as alterações não guardadas?",
            ["Discard changes"] = "Descartar alterações",
            ["+ New Entry"] = "+ Nova entrada",
            ["Favorites"] = "Favoritos",
            ["Tags"] = "Etiquetas",
            ["No tags yet."] = "Ainda não há etiquetas.",
            ["Retry"] = "Tentar novamente",
            ["Loading your knowledge library..."] = "A carregar a biblioteca de conhecimento...",
            ["Unable to load the knowledge library."] = "Não foi possível carregar a biblioteca de conhecimento.",
            ["All projects"] = "Todos os projetos",
            ["Newest first"] = "Mais recentes primeiro",
            ["Oldest first"] = "Mais antigos primeiro",
            ["Title Aâ€“Z"] = "Tí­tulo Aâ€“Z",
            ["Title Zâ€“A"] = "TÃ­tulo Zâ€“A",
            ["Project Aâ€“Z"] = "Projeto Aâ€“Z",
            ["No knowledge entries yet.\r\n\r\nStart documenting the technical problems you solve."] =
                "Ainda nã£o há entradas de conhecimento.\r\n\r\nComece por documentar os problemas técnicos que resolve.",
            ["No entries match your search.\r\n\r\nTry a different search term or clear the filters."] =
                "Nenhuma entrada corresponde à pesquisa.\r\n\r\nTente outro termo ou limpe os filtros.",
            ["The favorite could not be updated. Please try again."] =
                "Não foi possível atualizar os favoritos. Tente novamente.",
            ["Delete \"{0}\"?\r\n\r\nThis also removes its steps and attachments."] =
                "Eliminar \"{0}\"?\r\n\r\nEsta ação também remove os passos e anexos.",
            ["Delete entry"] = "Eliminar entrada",
            ["The entry could not be deleted. Please try again."] =
                "Não foi possível eliminar a entrada. Tente novamente.",
            ["Manage Steps"] = "Gerir passos",
            ["Apply steps"] = "Aplicar passos",
            ["Resolution steps"] = "Passos de resolução",
            ["No resolution steps yet.\r\n\r\nBreak the solution into clear, reproducible steps."] =
                "Ainda não há passos de resolução.\r\n\r\nDivida a solução em passos claros e reproduzíveis.",
            ["Entries over time"] = "Entradas ao longo do tempo",
            ["Total entries"] = "Total de entradas",
            ["Projects"] = "Projetos",
            ["Top projects"] = "Projetos principais",
            ["Top tags"] = "Etiquetas principais",
            ["Loading your statistics..."] = "A carregar as estatísticas...",
            ["Unable to load statistics."] = "Não foi possível carregar as estatísticas.",
            ["+ Create entry"] = "+ Criar entrada",
            ["Open Library"] = "Abrir biblioteca",
            ["Your knowledge statistics will appear here once you create some entries."] =
                "As estatísticas de conhecimento aparecerão aqui depois de criar algumas entradas.",
            ["No projects yet."] = "Ainda não há projetos.",
            ["No resolved entries in the last 12 months.\r\nSet a \"Date resolved\" value on an entry to see activity here."] =
                "Não há entradas resolvidas nos últimos 12 meses.\r\nDefina uma data de resolução numa entrada para ver atividade aqui.",
            ["No description."] = "Sem descrição.",
            ["Remove favorite"] = "Remover dos favoritos",
            ["Add favorite"] = "Adicionar aos favoritos",
            ["Open/Edit"] = "Abrir/Editar",
            ["Toggle favorite"] = "Alternar favorito",
            ["Delete"] = "Eliminar",
            ["1 entry"] = "1 entrada",
            ["{0} entries"] = "{0} entradas",
            ["{0} of {1} entries"] = "{0} de {1} entradas",
            ["Clear filters"] = "Limpar filtros",
            ["{0} step"] = "{0} passo",
            ["{0} file"] = "{0} ficheiro",
            ["{0} files"] = "{0} ficheiros",
        };

        private static readonly Dictionary<string, string> EnglishText = PortugueseText
            .Where(pair => !string.Equals(pair.Key, pair.Value, StringComparison.Ordinal))
            .GroupBy(pair => pair.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);

        public static AppLanguage Language { get; private set; } = AppLanguage.English;

        public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo(EnglishCulture);

        public static event EventHandler? LanguageChanged;

        public static CultureInfo GetCulture(AppLanguage language) =>
            language switch
            {
                AppLanguage.English => CultureInfo.GetCultureInfo(EnglishCulture),
                AppLanguage.Portuguese => CultureInfo.GetCultureInfo(PortugueseCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(language)),
            };

        private static void ApplyComboItems(ComboBoxAdv comboBox, IReadOnlyList<(int Index, string EnglishText)> items)
        {
            var selectedIndex = comboBox.SelectedIndex;
            foreach (var item in items)
            {
                if (item.Index >= 0 && item.Index < comboBox.Items.Count)
                {
                    comboBox.Items[item.Index] = Translate(item.EnglishText);
                }
            }

            if (selectedIndex >= 0 && selectedIndex < comboBox.Items.Count)
            {
                comboBox.SelectedIndex = selectedIndex;
            }
        }

        public static void Initialize()
        {
            var selectedCulture = EnglishCulture;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var persistedCulture = File.ReadAllText(SettingsPath).Trim();
                    if (persistedCulture is EnglishCulture or PortugueseCulture)
                    {
                        selectedCulture = persistedCulture;
                    }
                    else
                    {
                        Debug.WriteLine($"Ignoring unsupported saved language '{persistedCulture}'.");
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Unable to read the saved language preference: {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Unable to read the saved language preference: {ex}");
            }

            ApplyCulture(selectedCulture);
        }

        public static void SetLanguage(AppLanguage language)
        {
            var cultureName = GetCulture(language).Name;

            if (Language == language)
            {
                return;
            }

            var directory = Path.GetDirectoryName(SettingsPath)
                ?? throw new InvalidOperationException("The language settings path has no parent directory.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(SettingsPath, cultureName);
            ApplyCulture(cultureName);
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string Translate(string text)
            => Translate(text, Language);

        internal static string Translate(string text, AppLanguage language)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (language == AppLanguage.Portuguese)
            {
                return PortugueseText.TryGetValue(text, out var translated) ? translated : text;
            }

            if (language != AppLanguage.English)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            return EnglishText.TryGetValue(text, out var translatedEnglish) ? translatedEnglish : text;
        }

        public static void Bind(Control control, string englishText)
        {
            ArgumentNullException.ThrowIfNull(control);
            control.Tag = englishText;
            control.Text = Translate(englishText);
        }

        public static void Bind(Control control, Func<string> textFactory)
        {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(textFactory);
            control.Tag = textFactory;
            control.Text = textFactory();
        }

        public static void Bind(ToolStripItem item, string englishText)
        {
            ArgumentNullException.ThrowIfNull(item);
            item.Tag = englishText;
            item.Text = Translate(englishText);
        }

        public static void BindComboItems(ComboBoxAdv comboBox, params (int Index, string EnglishText)[] items)
        {
            ArgumentNullException.ThrowIfNull(comboBox);
            comboBox.Tag = items;
            ApplyComboItems(comboBox, items);
        }

        public static void Apply(Control root)
        {
            if (root.Tag is string key && PortugueseText.ContainsKey(key))
            {
                root.Text = Translate(key);
            }
            else if (root.Tag is Func<string> textFactory)
            {
                root.Text = textFactory();
            }

            if (root.ContextMenuStrip is not null)
            {
                Apply(root.ContextMenuStrip.Items);
            }

            if (root is SfDateTimeEdit dateTimeEdit)
            {
                dateTimeEdit.Watermark = Translate("Not set");
            }

            if (root is ComboBoxAdv comboBoxWithItems && root.Tag is ValueTuple<int, string>[] items)
            {
                ApplyComboItems(comboBoxWithItems, items);
            }

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }

            if (root is ComboBoxFrame comboBoxFrame)
            {
                comboBoxFrame.ApplyLocalization();
            }
        }

        private static void Apply(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.Tag is string key && PortugueseText.ContainsKey(key))
                {
                    item.Text = Translate(key);
                }

                if (item is ToolStripDropDownItem dropDownItem)
                {
                    Apply(dropDownItem.DropDownItems);
                }
            }
        }

        private static void ApplyCulture(string cultureName)
        {
            Culture = CultureInfo.GetCultureInfo(cultureName);
            Language = cultureName == PortugueseCulture ? AppLanguage.Portuguese : AppLanguage.English;
            CultureInfo.DefaultThreadCurrentCulture = Culture;
            CultureInfo.DefaultThreadCurrentUICulture = Culture;
            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = Culture;
        }

    }
}
