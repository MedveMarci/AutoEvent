using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using AutoEvent.Interfaces;
#if EXILED
using Exiled.API.Features;
using Exiled.API.Extensions;
using Exiled.Loader;
#else
using LiteNetLib.Utils;
using LabApi.Loader.Features.Yaml;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent;

public static class ConfigManager
{
    internal static string ConfigPath { get; } = Path.Combine(AutoEvent.BaseConfigPath, "configs.yml");

    internal static string TranslationPath { get; } = Path.Combine(AutoEvent.BaseConfigPath, "translation.yml");

    internal static Dictionary<string, string> LanguageByCountryCodeDictionary { get; } = new()
    {
        ["EN"] = "english",
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

    internal static void LoadConfigs()
    {
        try
        {
            var configs = new Dictionary<string, object>();

            if (!File.Exists(ConfigPath))
            {
                foreach (var ev in AutoEvent.EventManager.Events.OrderBy(r => r.Name))
                    configs.Add(ev.Name, ev.InternalConfig);
                // Save the config file
#if EXILED
                File.WriteAllText(ConfigPath, Loader.Serializer.Serialize(configs));
#else
                File.WriteAllText(ConfigPath, YamlConfigParser.Serializer.Serialize(configs));
#endif
            }
            else
            {
#if EXILED
                configs = Loader.Deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath));
#else
                configs =
                    YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath));
#endif
            }

            // Move translations to each mini-games
            foreach (var ev in AutoEvent.EventManager.Events)
            {
                if (configs is null)
                    continue;

                if (!configs.TryGetValue(ev.Name, out var rawDeserializedConfig))
                {
                    DebugLogger.LogDebug($"[ConfigManager] {ev.Name} doesn't have configs");
                    continue;
                }
#if EXILED
                var translation =
                    (EventConfig)Loader.Deserializer.Deserialize(Loader.Serializer.Serialize(rawDeserializedConfig),
                        ev.InternalConfig.GetType());
#else
                var translation = (EventConfig)YamlConfigParser.Deserializer.Deserialize(
                    YamlConfigParser.Serializer.Serialize(rawDeserializedConfig), ev.InternalConfig.GetType());
#endif
                ev.InternalConfig.CopyProperties(translation);
            }

            DebugLogger.LogDebug("[ConfigManager] The configs of the mini-games are loaded.");
        }
        catch (Exception ex)
        {
            DebugLogger.LogDebug("[ConfigManager] cannot read from the config.", LogLevel.Error, true);
            DebugLogger.LogDebug($"{ex}");
        }
    }

    internal static void LoadTranslations()
    {
        try
        {
            var translations = new Dictionary<string, object>();

            // If the translation file is not found, then create a new one.
            if (!File.Exists(TranslationPath))
            {
                var countryCode = "EN";
                try
                {
                    using (var client = new WebClient())
                    {
                        var url = $"http://ipinfo.io/{Server.IpAddress}/country";
                        countryCode = client.DownloadString(url).Trim();
                    }
                }
                catch (WebException ex)
                {
                    DebugLogger.LogDebug("Couldn't verify the server country. Providing default translation.");
                }

                DebugLogger.LogDebug(
                    $"[ConfigManager] The translation.yml file was not found. Creating a new translation for {countryCode} language...");
                translations = LoadTranslationFromAssembly(countryCode);
            }
            // Otherwise, check language of the translation with the language of the config.
            else
            {
#if EXILED
                translations =
                    Loader.Deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(TranslationPath));
#else
                translations =
                    YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(
                        File.ReadAllText(TranslationPath));
#endif
            }

            // Move translations to each mini-games
            foreach (var ev in AutoEvent.EventManager.Events)
            {
                if (translations is null)
                    continue;

                if (!translations.TryGetValue(ev.Name, out var rawDeserializedTranslation))
                {
                    DebugLogger.LogDebug($"[ConfigManager] {ev.Name} doesn't have translations");
                    continue;
                }
#if EXILED
                var obj = Loader.Deserializer.Deserialize(Loader.Serializer.Serialize(rawDeserializedTranslation),
#else
                var obj = YamlConfigParser.Deserializer.Deserialize(
                    YamlConfigParser.Serializer.Serialize(rawDeserializedTranslation),
#endif
                    ev.InternalTranslation.GetType());
                if (obj is not EventTranslation translation)
                {
                    DebugLogger.LogDebug($"[ConfigManager] {ev.Name} malformed translation.");
                    continue;
                }

                ev.InternalTranslation.CopyProperties(translation);

                ev.Name = translation.Name;
                ev.Description = translation.Description;
                ev.CommandName = translation.CommandName;
            }

            DebugLogger.LogDebug("[ConfigManager] The translations of the mini-games are loaded.");
        }
        catch (Exception ex)
        {
            DebugLogger.LogDebug("[ConfigManager] Cannot read from the translation.", LogLevel.Error, true);
            DebugLogger.LogDebug($"{ex}");
        }
    }

    internal static Dictionary<string, object> LoadTranslationFromAssembly(string countryCode)
    {
        Dictionary<string, object> translations;

        // Try to get a translation from an assembly
        if (!TryGetTranslationFromAssembly(countryCode, TranslationPath, out translations))
            translations = GenerateDefaultTranslations();

        return translations;
    }

    internal static Dictionary<string, object> GenerateDefaultTranslations()
    {
        // Otherwise, create default translations from all mini-games.
        var translations = new Dictionary<string, object>();

        foreach (var ev in AutoEvent.EventManager.Events.OrderBy(r => r.Name))
        {
            ev.InternalTranslation.Name = ev.Name;
            ev.InternalTranslation.Description = ev.Description;
            ev.InternalTranslation.CommandName = ev.CommandName;

            translations.Add(ev.Name, ev.InternalTranslation);
        }

        // Save the translation file
#if EXILED
        File.WriteAllText(TranslationPath, Loader.Serializer.Serialize(translations));
#else
        File.WriteAllText(TranslationPath, YamlConfigParser.Serializer.Serialize(translations));
#endif
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
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    DebugLogger.LogDebug($"[ConfigManager] The language '{language}' was not found in the assembly.",
                        LogLevel.Error);
                    translationFile = default;
                    return false;
                }

                using (var reader = new StreamReader(stream))
                {
                    var yaml = reader.ReadToEnd();
#if EXILED
                    translationFile = Loader.Deserializer.Deserialize<T>(yaml);
#else
                    translationFile = YamlConfigParser.Deserializer.Deserialize<T>(yaml);
#endif

                    // Save the translation file
                    File.WriteAllText(path, yaml);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogDebug($"[ConfigManager] The language '{language}' cannot load from the assembly.",
                LogLevel.Error, true);
            DebugLogger.LogDebug($"{ex}");
        }

        translationFile = default;
        return false;
    }

#if LABAPI
    public static void CopyProperties(this object target, object source)
    {
        var type = target.GetType();
        if (type != source.GetType())
            throw new InvalidTypeException("Target and source type mismatch!");
        foreach (var property in type.GetProperties())
            type.GetProperty(property.Name)?.SetValue(target, property.GetValue(source, null), null);
    }
#endif
}