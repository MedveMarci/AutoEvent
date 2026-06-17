using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                foreach (var ev in AutoEvent.InternalEventManager.Events.OrderBy(r => r.InternalName))
                    configs[ev.InternalName] = ev.InternalConfig;
                File.WriteAllText(ConfigPath, YamlConfigParser.Serializer.Serialize(configs));
                return;
            }

            configs =
                YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(
                    File.ReadAllText(ConfigPath));

            foreach (var ev in AutoEvent.InternalEventManager.Events)
            {
                if (configs is null)
                    continue;

                if (!configs.TryGetValue(ev.InternalName, out var rawDeserializedConfig))
                {
                    LogManager.Warn($"[ConfigManager] {ev.InternalName} doesn't have configs");
                    continue;
                }

                var loadedConfig = (EventConfig)YamlConfigParser.Deserializer.Deserialize(
                    YamlConfigParser.Serializer.Serialize(rawDeserializedConfig),
                    ev.InternalConfig.GetType());

                var originalMaps = ev.InternalConfig.AvailableMaps?.ToList();
                ev.InternalConfig.CopyProperties(loadedConfig);

                // If the YAML had only empty map_name entries (old/corrupt config),
                // restore the code-defined defaults so the event can actually start.
                if (ev.InternalConfig.AvailableMaps is { Count: > 0 } &&
                    ev.InternalConfig.AvailableMaps.All(m => string.IsNullOrEmpty(m.MapName)) &&
                    originalMaps is { Count: > 0 } &&
                    originalMaps.Any(m => !string.IsNullOrEmpty(m.MapName)))
                {
                    ev.InternalConfig.AvailableMaps = originalMaps;
                    LogManager.Warn(
                        $"[ConfigManager] {ev.InternalName}: AvailableMaps had empty names in config, restored defaults.");
                }
            }

            var updatedConfigs = new Dictionary<string, object>();
            foreach (var ev in AutoEvent.InternalEventManager.Events.OrderBy(r => r.InternalName))
                updatedConfigs[ev.InternalName] = ev.InternalConfig;

            File.WriteAllText(ConfigPath, YamlConfigParser.Serializer.Serialize(updatedConfigs));

            LogManager.Info("[ConfigManager] The configs of the mini-games are loaded and updated.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] Cannot read from the config. You need to fix this issue in your config file if it's a YamlError!\n{ex}", false);
        }
    }


    public static void LoadTranslations()
    {
        try
        {
            Dictionary<string, object> translations;

            if (!File.Exists(TranslationPath))
            {
                var countryCode = "EN";
                try
                {
                    var url = $"http://ipinfo.io/{Server.IpAddress}/country";
                    countryCode = HttpQuery.Get(url).Trim();
                }
                catch (Exception)
                {
                    LogManager.Warn("Couldn't verify the server country. Providing default translation.");
                }

                LogManager.Warn(
                    $"[ConfigManager] The translation.yml file was not found. Creating a new translation for {countryCode} language...");
                translations = LoadTranslationFromAssembly(countryCode);
            }
            // Otherwise, check language of the translation with the language of the config.
            else
            {
                translations =
                    YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(
                        File.ReadAllText(TranslationPath));
            }

            // Move translations to each mini-games
            var events = AutoEvent.InternalEventManager?.Events;

            if (events == null)
                return;

            foreach (var ev in events.Where(_ => translations is not null))
            {
                if (!translations.TryGetValue(ev.InternalName, out var rawDeserializedTranslation))
                {
                    LogManager.Warn($"[ConfigManager] {ev.InternalName} doesn't have translations");
                    continue;
                }

                var obj = YamlConfigParser.Deserializer.Deserialize(
                    YamlConfigParser.Serializer.Serialize(rawDeserializedTranslation),
                    ev.InternalTranslation.GetType());
                if (obj is not EventTranslation translation)
                {
                    LogManager.Warn($"[ConfigManager] {ev.InternalName} malformed translation.");
                    continue;
                }

                ev.InternalTranslation.CopyProperties(translation);

                ev.Name = translation.Name;
                ev.Description = translation.Description;
                ev.CommandName = translation.CommandName;
            }

            LogManager.Info("[ConfigManager] The translations of the mini-games are loaded.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] Cannot read from the translation. You need to fix this issue in your config file if it's a YamlError!\n{ex}", false);
        }
    }

    public static string ResolveLanguage(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (LanguageByCountryCodeDictionary.TryGetValue(input.ToUpperInvariant(), out var byCode))
            return byCode;

        var languages = LanguageByCountryCodeDictionary.Values.Distinct().ToList();

        var exact = languages.FirstOrDefault(l => string.Equals(l, input, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        var matches = languages.Where(l => l.StartsWith(input, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    internal static Dictionary<string, object> LoadTranslationFromAssembly(string input)
    {
        var language = ResolveLanguage(input);

        if (language == null ||
            !TryGetTranslationFromAssembly(language, TranslationPath, out Dictionary<string, object> translations))
            translations = GenerateDefaultTranslations();

        return translations;
    }

    private static Dictionary<string, object> GenerateDefaultTranslations()
    {
        // Otherwise, create default translations from all mini-games.
        var translations = new Dictionary<string, object>();

        var events = AutoEvent.InternalEventManager?.Events;
        if (events == null) return translations;

        // Key by InternalName — that is what LoadTranslations looks up, and unlike
        // Name it never changes when a translation is applied.
        foreach (var ev in events.OrderBy(r => r.InternalName))
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
        var resourceName = $"AutoEvent.Translations.{language}.yml";

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LogManager.Warn($"[ConfigManager] The language '{language}' was not found in the assembly.");
                translationFile = default;
                return false;
            }

            using var reader = new StreamReader(stream);
            var yaml = reader.ReadToEnd();
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
        var type = target.GetType();
        if (type != source.GetType())
            throw new InvalidTypeException("Target and source type mismatch!");
        foreach (var property in type.GetProperties())
        {
            if (!property.CanRead || !property.CanWrite) continue;
            type.GetProperty(property.Name)?.SetValue(target, property.GetValue(source, null), null);
        }
    }
}