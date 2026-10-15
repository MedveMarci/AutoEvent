using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Loader;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Translations : ICommand, IUsageProvider
{
    public string Command => "Language";

    public string[] Aliases { get; } = [];

    public string Description => "Change plugin's language";

    public string[] Usage { get; } = [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (AutoEvent.Singleton is null)
        {
            response = "AutoEvent is not loaded. Check console for errors.";
            return false;
        }

        if (!sender.HasPermissions("ev.language"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (arguments.Count == 0)
            goto syntax;

        string arg0 = arguments.At(0).ToLower();
        if (arg0 == "list")
        {
            if (arguments.Count != 1)
            {
                response = "Usage: ev language list";
                return false;
            }

            response = "List of translations:\n";
            try
            {
                IOrderedEnumerable<IGrouping<string, KeyValuePair<string, string>>> grouped = ConfigManager.LanguageByCountryCodeDictionary.GroupBy(kv => kv.Value).OrderBy(g => g.Key);

                foreach (IGrouping<string, KeyValuePair<string, string>> group in grouped)
                    response += $"{group.Key} ({string.Join(", ", group.Select(kv => kv.Key))})\n";
            }
            catch (Exception e)
            {
                response = $"Failed to list translations: {e.Message}";
                return false;
            }

            response += "Use ev language load [language or code] to load a translation.";
            return true;
        }

        if (arg0 == "load")
        {
            if (arguments.Count != 2)
            {
                response = "Usage: ev language load [language or code]";
                return false;
            }

            try
            {
                string input = arguments.At(1);
                string language = ConfigManager.ResolveLanguage(input);

                if (language == null)
                {
                    string available = string.Join(", ", ConfigManager.LanguageByCountryCodeDictionary.Values.Distinct().OrderBy(x => x));
                    response = $"Language '{input}' not found!\nAvailable: {available}";
                    return false;
                }

                _ = ConfigManager.LoadTranslationFromAssembly(language);
                ConfigManager.LoadTranslations();
                LanguageManager.SetLanguage(language);
                response = $"Translation '{language}' loaded for AutoEvent and its add-ons!";
                return true;
            }
            catch (Exception e)
            {
                response = $"Failed to load translation: {e.Message}";
                return false;
            }
        }

        syntax:
        response = "Translations management:\n" + "ev language list - list all available plugin localisations\n" + "ev language load [language] - set language\n";
        return true;
    }
}