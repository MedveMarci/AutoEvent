using System.Linq;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;

namespace AutoEvent.Integrations.MapEditor;

internal static class ProjectMerIntegration
{
    public static bool IsExistsMap(string schematicName)
    {
        return MapUtils.TryGetSchematicDataByName(schematicName, out _);
    }

    public static MapObject LoadMap(string schematicName, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        MeroIntegration.TrySetIsDynamiclyDisabled(true);

        if (!ObjectSpawner.TrySpawnSchematic(schematicName, pos, rot, scale, out var schematicObject))
        {
            AutoEvent.InternalEventManager.CurrentEvent?.StopEvent();
            foreach (var pl in Player.ReadyList)
                pl.SetRole(RoleTypeId.Spectator);
            LogManager.Error(
                $"The map {schematicName} could not be loaded because it was not found. " +
                $"Delete and re-download the schematics.");
            return null;
        }

        foreach (var toyBase in schematicObject.AdminToyBases)
            toyBase.syncInterval = 0;

        MeroIntegration.TrySetIsDynamiclyDisabled(false);
        return new MapObject
        {
            AttachedBlocks = schematicObject.AttachedBlocks.ToList(),
            AdminToyBases = schematicObject.AdminToyBases.ToList(),
            GameObject = schematicObject.gameObject
        };
    }

    public static GameObject CreatePlatformByParent(GameObject parent, Vector3 position)
    {
        var prim = parent.GetComponent<AdminToys.PrimitiveObjectToy>();
        var obj = ObjectSpawner.SpawnPrimitive(new SerializablePrimitive
        {
            PrimitiveType = prim.PrimitiveType,
            Position = position,
            Scale = parent.transform.localScale,
            Color = prim.MaterialColor.ToHex()
        });

        NetworkServer.Spawn(obj.gameObject);
        return obj.gameObject;
    }

    public static SchematicObject LoadSchematic(SerializableSchematic serializableSchematic)
    {
        MeroIntegration.TrySetIsDynamiclyDisabled(true);

        if (!ObjectSpawner.TrySpawnSchematic(serializableSchematic, out var schematicObject))
        {
            AutoEvent.InternalEventManager.CurrentEvent?.StopEvent();
            foreach (var pl in Player.ReadyList)
                pl.SetRole(RoleTypeId.Spectator);
            LogManager.Error(
                $"The schematic {serializableSchematic.SchematicName} could not be loaded. Delete and re-download the schematics.");
            return null;
        }

        foreach (var toyBase in schematicObject.AdminToyBases)
            toyBase.syncInterval = 0;

        MeroIntegration.TrySetIsDynamiclyDisabled(false);
        return schematicObject;
    }
}