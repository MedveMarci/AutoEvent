using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoEvent.ApiFeatures;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Yaml;

namespace AutoEvent.API;

public static class LanguageManager
{
    public const string DefaultLanguage = "english";

    public static string CurrentLanguage => string.IsNullOrWhiteSpace(AutoEvent.Singleton?.Config.Language) ? DefaultLanguage : AutoEvent.Singleton.Config.Language;

    public static event Action<string> LanguageChanged;

    internal static void SetLanguage(string language, bool notify = true)
    {
        if (string.IsNullOrWhiteSpace(language))
            return;

        if (AutoEvent.Singleton is { } plugin && !string.Equals(plugin.Config.Language, language, StringComparison.OrdinalIgnoreCase))
        {
            plugin.Config.Language = language;
            plugin.SaveConfig();
        }

        if (!notify || LanguageChanged is null)
            return;

        foreach (Delegate handler in LanguageChanged.GetInvocationList())
            try
            {
                ((Action<string>)handler)(language);
            }
            catch (Exception e)
            {
                LogManager.Error($"[LanguageManager] A LanguageChanged subscriber ({handler.Method.DeclaringType?.FullName}) failed.\n{e}");
            }
    }

    public static T LoadTranslation<T>(Plugin plugin, string fileName = "translation.yml") where T : class, new()
    {
        if (!File.Exists(plugin.GetConfigPath(fileName)))
            return ApplyLanguage<T>(plugin, CurrentLanguage, fileName);

        if (!plugin.TryReadConfig(fileName, out T translation))
        {
            LogManager.Error($"[LanguageManager] {plugin.Name}'s {fileName} could not be read, the default (English) texts are used until it's fixed.");
            return new T();
        }

        plugin.TrySaveConfig(translation, fileName);
        return translation;
    }

    public static T ApplyLanguage<T>(Plugin plugin, string language, string fileName = "translation.yml") where T : class, new()
    {
        T translation = ReadEmbedded<T>(plugin.GetType().Assembly, language);
        if (translation is null && !string.Equals(language, DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            LogManager.Debug($"[LanguageManager] {plugin.Name} has no '{language}' translation, using English.");

        translation ??= new T();
        plugin.TrySaveConfig(translation, fileName);
        return translation;
    }

    private static T ReadEmbedded<T>(Assembly assembly, string language) where T : class
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        string suffix = $".Translations.{language}.yml";
        string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
            return null;

        try
        {
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                return null;

            using StreamReader reader = new(stream);
            return YamlConfigParser.Deserializer.Deserialize<T>(reader.ReadToEnd());
        }
        catch (Exception e)
        {
            LogManager.Error($"[LanguageManager] The embedded translation '{resourceName}' could not be read.\n{e}");
            return null;
        }
    }
}