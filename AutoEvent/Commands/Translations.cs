using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (!sender.HasPermissions("ev.language"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (arguments.Count == 0)
            goto syntax;

        var arg0 = arguments.At(0).ToLower();
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
                var grouped = ConfigManager.LanguageByCountryCodeDictionary
                    .GroupBy(kv => kv.Value)
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
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
                var input = arguments.At(1);
                var language = ConfigManager.ResolveLanguage(input);

                if (language == null)
                {
                    var available = string.Join(", ",
                        ConfigManager.LanguageByCountryCodeDictionary.Values.Distinct().OrderBy(x => x));
                    response = $"Language '{input}' not found!\nAvailable: {available}";
                    return false;
                }

                _ = ConfigManager.LoadTranslationFromAssembly(language);
                ConfigManager.LoadTranslations();
                response = $"Translation '{language}' loaded!";
                return true;
            }
            catch (Exception e)
            {
                response = $"Failed to load translation: {e.Message}";
                return false;
            }
        }

        syntax:
        response = "Translations management:\n" +
                   "ev language list - list all available plugin localisations\n" +
                   "ev language load [language] - set language\n";
        return true;
    }

    public string[] Usage { get; } = [];
}