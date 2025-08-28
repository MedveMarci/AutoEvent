using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Yaml;
using LiteNetLib.Utils;

namespace AutoEvent.Loader;

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
                File.WriteAllText(ConfigPath, YamlConfigParser.Serializer.Serialize(configs));
            }
            else
            {
                configs =
                    YamlConfigParser.Deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath));
            }

            // Move translations to each mini-games
            foreach (var ev in AutoEvent.EventManager.Events)
            {
                if (configs is null)
                    continue;

                if (!configs.TryGetValue(ev.Name, out var rawDeserializedConfig))
                {
                    LogManager.Warn($"[ConfigManager] {ev.Name} doesn't have configs");
                    continue;
                }

                var translation = (EventConfig)YamlConfigParser.Deserializer.Deserialize(
                    YamlConfigParser.Serializer.Serialize(rawDeserializedConfig), ev.InternalConfig.GetType());
                ev.InternalConfig.CopyProperties(translation);
            }

            LogManager.Info("[ConfigManager] The configs of the mini-games are loaded.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] cannot read from the config.\n{ex}");
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
                catch (WebException)
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
            foreach (var ev in AutoEvent.EventManager.Events)
            {
                if (translations is null)
                    continue;

                if (!translations.TryGetValue(ev.Name, out var rawDeserializedTranslation))
                {
                    LogManager.Warn($"[ConfigManager] {ev.Name} doesn't have translations");
                    continue;
                }

                var obj = YamlConfigParser.Deserializer.Deserialize(
                    YamlConfigParser.Serializer.Serialize(rawDeserializedTranslation),
                    ev.InternalTranslation.GetType());
                if (obj is not EventTranslation translation)
                {
                    LogManager.Warn($"[ConfigManager] {ev.Name} malformed translation.");
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
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    LogManager.Warn($"[ConfigManager] The language '{language}' was not found in the assembly.");
                    translationFile = default;
                    return false;
                }

                using (var reader = new StreamReader(stream))
                {
                    var yaml = reader.ReadToEnd();
                    translationFile = YamlConfigParser.Deserializer.Deserialize<T>(yaml);

                    // Save the translation file
                    File.WriteAllText(path, yaml);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"[ConfigManager] The language '{language}' cannot load from the assembly.\n{ex}");
        }

        translationFile = default;
        return false;
    }

    public static void CopyProperties(this object target, object source)
    {
        var type = target.GetType();
        if (type != source.GetType())
            throw new InvalidTypeException("Target and source type mismatch!");
        foreach (var property in type.GetProperties())
            type.GetProperty(property.Name)?.SetValue(target, property.GetValue(source, null), null);
    }
}