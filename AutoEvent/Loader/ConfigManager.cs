using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Yaml;
using LiteNetLib.Utils;

namespace AutoEvent.Loader;

public static class ConfigManager
{
    private static string ConfigPath { get; } = Path.Combine(AutoEvent.BaseConfigPath, "configs.yml");

    private static string TranslationPath { get; } = Path.Combine(AutoEvent.BaseConfigPath, "translation.yml");

    internal static Dictionary<string, string> LanguageByCountryCodeDictionary { get; } = new()
    {
        ["EN"] = "english",
        ["HU"] = "hungarian",
        ["CN"] = "chinese",
        ["FR"] = "french",
        ["DE"] = "german",
        ["NL"] = "german", //sorry :)
        ["IT"] = "italian",
        ["PL"] = "polish",
        ["BR"] = "portuguese",
        ["PT"] = "portuguese",
        ["RU"] = "russian",
        ["KZ"] = "russian",
        ["BY"] = "russian",
        ["UA"] = "russian", //sorry :)
        ["ES"] = "spanish",
        ["TH"] = "thai",
        ["TR"] = "turkish"
    };

    public static void LoadConfigsAndTranslations()
    {
        LoadConfigs();
        LoadTranslations();
    }

    private static void LoadConfigs()
    {
        try
        {
            Dictionary<string, object> configs;

            if (!File.Exists(ConfigPath))
            {
                configs = new Dictionary<string, object>();
                foreach (Event ev in AutoEvent.InternalEventManager.Events.OrderBy(r => r.InternalName))
                    configs[ev.InternalName] = ev.InternalConfig;
                File.WriteAllText(ConfigPath, YamlConfigParser.Serializer.Serialize(configs));
                return;
            }

            configs = YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath));

            foreach (Event ev in AutoEvent.InternalEventManager.Events)
            {
                if (configs is null)
                    continue;

                if (!configs.TryGetValue(ev.InternalName, out object rawDeserializedConfig))
                {
                    LogManager.Warn($"[ConfigManager] {ev.InternalName} doesn't have configs");
                    continue;
                }

                EventConfig loadedConfig = (EventConfig)YamlConfigParser.Deserializer.Deserialize(YamlConfigParser.Serializer.Serialize(rawDeserializedConfig), ev.InternalConfig.GetType());

                List<MapChance> originalMaps = ev.InternalConfig.AvailableMaps?.ToList();
                ev.InternalConfig.CopyProperties(loadedConfig);

                // If the YAML had only empty map_name entries (old/corrupt config),
                // restore the code-defined defaults so the event can actually start.
                if (ev.InternalConfig.AvailableMaps is { Count: > 0 } && ev.InternalConfig.AvailableMaps.All(m => string.IsNullOrEmpty(m.MapName)) && originalMaps is { Count: > 0 } && originalMaps.Any(m => !string.IsNullOrEmpty(m.MapName)))
                {
                    ev.InternalConfig.AvailableMaps = originalMaps;
                    LogManager.Warn($"[ConfigManager] {ev.InternalName}: AvailableMaps had empty names in config, restored defaults.");
                }
            }

            // Keep the sections of events that aren't registered yet (external events register after
            // AutoEvent loads), otherwise their configs would be wiped on every startup.
            Dictionary<string, object> updatedConfigs = configs ?? new Dictionary<string, object>();
            foreach (Event ev in AutoEvent.InternalEventManager.Events)
                updatedConfigs[ev.InternalName] = ev.InternalConfig;

            File.WriteAllText(ConfigPath, YamlConfigParser.Serializer.Serialize(updatedConfigs.OrderBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value)));

            LogManager.Info("[ConfigManager] The configs of the mini-games are loaded and updated.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] Cannot read from the config. You need to fix this issue in your config file if it's a YamlError!\n{ex}");
        }
    }


    public static void LoadTranslations()
    {
        try
        {
            Dictionary<string, object> translations;

            if (!File.Exists(TranslationPath))
            {
                string countryCode = "EN";
                try
                {
                    string url = $"http://ipinfo.io/{Server.IpAddress}/country";
                    countryCode = HttpQuery.Get(url).Trim();
                }
                catch (Exception)
                {
                    LogManager.Warn("Couldn't verify the server country. Providing default translation.");
                }

                LogManager.Warn($"[ConfigManager] The translation.yml file was not found. Creating a new translation for {countryCode} language...");
                translations = LoadTranslationFromAssembly(countryCode);

                LanguageManager.SetLanguage(ResolveLanguage(countryCode) ?? LanguageManager.DefaultLanguage, false);
            }
            else
            {
                translations = YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(TranslationPath));
            }

            // Move translations to each mini-games
            List<Event> events = AutoEvent.InternalEventManager?.Events;

            if (events == null)
                return;

            translations ??= new Dictionary<string, object>();
            bool addedMissing = false;

            foreach (Event ev in events)
            {
                if (!translations.TryGetValue(ev.InternalName, out object rawDeserializedTranslation))
                {
                    LogManager.Warn($"[ConfigManager] {ev.InternalName} doesn't have translations, adding the default one.");
                    ev.InternalTranslation.Name = ev.Name;
                    ev.InternalTranslation.Description = ev.Description;
                    ev.InternalTranslation.CommandName = ev.CommandName;
                    translations[ev.InternalName] = ev.InternalTranslation;
                    addedMissing = true;
                    continue;
                }

                object obj = YamlConfigParser.Deserializer.Deserialize(YamlConfigParser.Serializer.Serialize(rawDeserializedTranslation), ev.InternalTranslation.GetType());
                if (obj is not EventTranslation translation)
                {
                    LogManager.Warn($"[ConfigManager] {ev.InternalName} malformed translation.");
                    continue;
                }

                ev.InternalTranslation.CopyProperties(translation);

                if (!string.IsNullOrWhiteSpace(translation.Name))
                    ev.Name = translation.Name;
                if (translation.Description != null)
                    ev.Description = translation.Description;
                if (!string.IsNullOrWhiteSpace(translation.CommandName))
                    ev.CommandName = translation.CommandName;
            }

            if (addedMissing)
                File.WriteAllText(TranslationPath, YamlConfigParser.Serializer.Serialize(translations.OrderBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value)));

            LogManager.Info("[ConfigManager] The translations of the mini-games are loaded.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] Cannot read from the translation. You need to fix this issue in your config file if it's a YamlError!\n{ex}");
        }
    }

    public static string ResolveLanguage(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (LanguageByCountryCodeDictionary.TryGetValue(input.ToUpperInvariant(), out string byCode))
            return byCode;

        List<string> languages = [.. LanguageByCountryCodeDictionary.Values.Distinct()];

        string exact = languages.FirstOrDefault(l => string.Equals(l, input, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        List<string> matches = [.. languages.Where(l => l.StartsWith(input, StringComparison.OrdinalIgnoreCase))];
        return matches.Count == 1 ? matches[0] : null;
    }

    internal static Dictionary<string, object> LoadTranslationFromAssembly(string input)
    {
        string language = ResolveLanguage(input);

        if (language == null || !TryGetTranslationFromAssembly(language, TranslationPath, out Dictionary<string, object> translations))
            translations = GenerateDefaultTranslations();

        return translations;
    }

    private static Dictionary<string, object> GenerateDefaultTranslations()
    {
        // Otherwise, create default translations from all mini-games.
        Dictionary<string, object> translations = new();

        List<Event> events = AutoEvent.InternalEventManager?.Events;
        if (events == null) return translations;

        // Key by InternalName — that is what LoadTranslations looks up, and unlike
        // Name it never changes when a translation is applied.
        foreach (Event ev in events.OrderBy(r => r.InternalName))
        {
            ev.InternalTranslation.Name = ev.Name;
            ev.InternalTranslation.Description = ev.Description;
            ev.InternalTranslation.CommandName = ev.CommandName;

            translations[ev.InternalName] = ev.InternalTranslation;
        }

        // Save the translation file
        File.WriteAllText(TranslationPath, YamlConfigParser.Serializer.Serialize(translations));
        return translations;
    }

    private static bool TryGetTranslationFromAssembly<T>(string language, string path, out T translationFile)
    {
        string resourceName = $"AutoEvent.Translations.{language}.yml";

        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LogManager.Warn($"[ConfigManager] The language '{language}' was not found in the assembly.");
                translationFile = default;
                return false;
            }

            using StreamReader reader = new(stream);
            string yaml = reader.ReadToEnd();
            translationFile = YamlConfigParser.Deserializer.Deserialize<T>(yaml);

            // Save the translation file
            File.WriteAllText(path, yaml);
            return true;
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] The language '{language}' cannot load from the assembly.\n{ex}");
        }

        translationFile = default;
        return false;
    }

    private static void CopyProperties(this object target, object source)
    {
        Type type = target.GetType();
        if (type != source.GetType())
            throw new InvalidTypeException("Target and source type mismatch!");
        foreach (PropertyInfo property in type.GetProperties())
        {
            if (!property.CanRead || !property.CanWrite) continue;
            type.GetProperty(property.Name)?.SetValue(target, property.GetValue(source, null), null);
        }
    }
}