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
            LogManager.Error($"[ConfigManager] cannot read from the config.\n{ex}");
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
            foreach (var ev in AutoEvent.InternalEventManager.Events.Where(_ => translations is not null))
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
            LogManager.Error($"[ConfigManager] Cannot read from the translation.\n{ex}");
        }
    }

    internal static Dictionary<string, object> LoadTranslationFromAssembly(string countryCode)
    {
        // Try to get a translation from an assembly
        if (!TryGetTranslationFromAssembly(countryCode, TranslationPath, out Dictionary<string, object> translations))
            translations = GenerateDefaultTranslations();

        return translations;
    }

    private static Dictionary<string, object> GenerateDefaultTranslations()
    {
        // Otherwise, create default translations from all mini-games.
        var translations = new Dictionary<string, object>();

        foreach (var ev in AutoEvent.InternalEventManager.Events.OrderBy(r => r.Name))
        {
            ev.InternalTranslation.Name = ev.Name;
            ev.InternalTranslation.Description = ev.Description;
            ev.InternalTranslation.CommandName = ev.CommandName;

            translations.Add(ev.Name, ev.InternalTranslation);
        }

        // Save the translation file
        File.WriteAllText(TranslationPath, YamlConfigParser.Serializer.Serialize(translations));
        return translations;
    }

    private static bool TryGetTranslationFromAssembly<T>(string countryCode, string path, out T translationFile)
    {
        if (!LanguageByCountryCodeDictionary.TryGetValue(countryCode, out var language))
        {
            translationFile = default;
            return false;
        }

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