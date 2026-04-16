using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Bingosoft.Net.IfcDetail;

internal sealed class FastIfcJsonComposer
{
    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly string[] MaterialOrder =
    [
        "IFCMATERIAL",
        "IFCMATERIALLIST",
        "IFCMATERIALLAYERSET",
        "IFCMATERIALLAYER",
        "IFCMATERIALCONSTITUENT",
        "IFCMATERIALCONSTITUENTSET",
        "IFCMATERIALLAYERSETUSAGE",
        "IFCPIPESEGMENTTYPE"
    ];

    private static readonly string[] TypeOrder =
    [
        "IFCSPACETYPE",
        "IFCCOLUMNTYPE",
        "IFCWALLTYPE",
        "IFCSLABTYPE",
        "IFCCOVERINGTYPE",
        "IFCSTAIRFLIGHTTYPE",
        "IFCPLATETYPE",
        "IFCMEMBERTYPE",
        "IFCCURTAINWALLTYPE",
        "IFCDISTRIBUTIONELEMENTTYPE",
        "IFCBUILDINGELEMENTPROXYTYPE",
        "IFCFURNITURETYPE"
    ];

    private static readonly string[] PropertyOrder =
    [
        "IFCPROPERTYSET",
        "IFCDOORLININGPROPERTIES",
        "IFCDOORPANELPROPERTIES",
        "IFCWINDOWLININGPROPERTIES"
    ];

    private static readonly Dictionary<string, string> ExpressTypeNames = new(StringComparer.Ordinal)
    {
        ["IFCMATERIAL"] = "IfcMaterial",
        ["IFCMATERIALLIST"] = "IfcMaterialList",
        ["IFCMATERIALLAYERSET"] = "IfcMaterialLayerSet",
        ["IFCMATERIALLAYER"] = "IfcMaterialLayer",
        ["IFCMATERIALCONSTITUENT"] = "IfcMaterialConstituent",
        ["IFCMATERIALCONSTITUENTSET"] = "IfcMaterialConstituentSet",
        ["IFCMATERIALLAYERSETUSAGE"] = "IfcMaterialLayerSetUsage",
        ["IFCSPACETYPE"] = "IfcSpaceType",
        ["IFCCOLUMNTYPE"] = "IfcColumnType",
        ["IFCWALLTYPE"] = "IfcWallType",
        ["IFCSLABTYPE"] = "IfcSlabType",
        ["IFCCOVERINGTYPE"] = "IfcCoveringType",
        ["IFCSTAIRFLIGHTTYPE"] = "IfcStairFlightType",
        ["IFCPLATETYPE"] = "IfcPlateType",
        ["IFCMEMBERTYPE"] = "IfcMemberType",
        ["IFCCURTAINWALLTYPE"] = "IfcCurtainWallType",
        ["IFCDISTRIBUTIONELEMENTTYPE"] = "IfcDistributionElementType",
        ["IFCBUILDINGELEMENTPROXYTYPE"] = "IfcBuildingElementProxyType",
        ["IFCPIPESEGMENTTYPE"] = "IfcPipeSegmentType",
        ["IFCFURNITURETYPE"] = "IfcFurnitureType",
        ["IFCPROPERTYSET"] = "IfcPropertySet",
        ["IFCDOORLININGPROPERTIES"] = "IfcDoorLiningProperties",
        ["IFCDOORPANELPROPERTIES"] = "IfcDoorPanelProperties",
        ["IFCWINDOWLININGPROPERTIES"] = "IfcWindowLiningProperties"
    };

    public void Write(FileInfo jsonTargetFile, FastIfcDataModel model)
    {
        using var stream = File.Create(jsonTargetFile.FullName);
        using var writer = new Utf8JsonWriter(stream, WriterOptions);

        writer.WriteStartObject();

        writer.WritePropertyName("materials");
        writer.WriteStartObject();
        WriteMaterials(writer, model);
        writer.WriteEndObject();

        writer.WritePropertyName("types");
        writer.WriteStartObject();
        WriteTypes(writer, model);
        writer.WriteEndObject();

        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        WriteProperties(writer, model);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    private static void WriteMaterials(Utf8JsonWriter writer, FastIfcDataModel model)
    {
        foreach (var typeName in MaterialOrder)
        {
            var entities = model.GetEntitiesByType(typeName);
            foreach (var entityIndex in entities)
            {
                switch (typeName)
                {
                    case "IFCMATERIAL":
                        WriteMaterial(writer, model, entityIndex);
                        break;
                    case "IFCMATERIALLIST":
                        WriteMaterialList(writer, model, entityIndex);
                        break;
                    case "IFCMATERIALLAYERSET":
                        WriteMaterialLayerSet(writer, model, entityIndex);
                        break;
                    case "IFCMATERIALLAYER":
                        WriteMaterialLayer(writer, model, entityIndex);
                        break;
                    case "IFCMATERIALCONSTITUENT":
                        WriteMaterialConstituent(writer, model, entityIndex);
                        break;
                    case "IFCMATERIALCONSTITUENTSET":
                        WriteMaterialConstituentSet(writer, model, entityIndex);
                        break;
                    case "IFCMATERIALLAYERSETUSAGE":
                        WriteMaterialLayerSetUsage(writer, model, entityIndex);
                        break;
                    case "IFCPIPESEGMENTTYPE":
                        WritePipeSegmentType(writer, model, entityIndex);
                        break;
                }
            }
        }

        var duplicatePipeSegments = model.GetEntitiesByType("IFCPIPESEGMENTTYPE");
        foreach (var entityIndex in duplicatePipeSegments)
        {
            WritePipeSegmentType(writer, model, entityIndex);
        }
    }

    private static void WriteTypes(Utf8JsonWriter writer, FastIfcDataModel model)
    {
        foreach (var typeName in TypeOrder)
        {
            var entities = model.GetEntitiesByType(typeName);
            foreach (var entityIndex in entities)
            {
                switch (typeName)
                {
                    case "IFCSPACETYPE":
                        WriteSpaceType(writer, model, entityIndex);
                        break;
                    case "IFCDISTRIBUTIONELEMENTTYPE":
                        WriteDistributionElementType(writer, model, entityIndex);
                        break;
                    case "IFCFURNITURETYPE":
                        WriteFurnitureType(writer, model, entityIndex);
                        break;
                    default:
                        WriteBuildingElementType(writer, model, entityIndex);
                        break;
                }
            }
        }
    }

    private static void WriteProperties(Utf8JsonWriter writer, FastIfcDataModel model)
    {
        foreach (var typeName in PropertyOrder)
        {
            var entities = model.GetEntitiesByType(typeName);
            foreach (var entityIndex in entities)
            {
                switch (typeName)
                {
                    case "IFCPROPERTYSET":
                        WritePropertySet(writer, model, entityIndex);
                        break;
                    case "IFCDOORLININGPROPERTIES":
                    case "IFCDOORPANELPROPERTIES":
                    case "IFCWINDOWLININGPROPERTIES":
                        WriteSimplePropertyDefinition(writer, model, entityIndex);
                        break;
                }
            }
        }
    }

    private static string EntityObjectId(FastIfcDataModel model, int entityIndex)
    {
        var typeName = model.Table.GetEntityType(entityIndex);
        var expressType = ToExpressTypeName(typeName);
        return $"{expressType}_{model.Table.GetEntityId(entityIndex)}";
    }

    private static void WriteMaterial(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);
        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 0));
        writer.WriteString("Id", objectId);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteMaterialList(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);
        writer.WriteStartArray("IfcMaterial");

        model.ForEachReferenceInArgument(entityIndex, 0, targetIndex =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", model.ReadStringOrDefault(targetIndex, 0));
            writer.WriteEndObject();
        });

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteMaterialLayerSet(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);

        writer.WriteStartArray("IfcMaterialLayer");
        model.ForEachReferenceInArgument(entityIndex, 0, layerIndex =>
        {
            WriteMaterialLayerDescriptor(writer, model, layerIndex);
        });
        writer.WriteEndArray();

        writer.WriteString("LayerSetName", model.ReadStringOrDefault(entityIndex, 1));
        writer.WriteString("id", objectId);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteMaterialLayer(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);
        writer.WriteStartArray("IfcMaterialLayer");

        var layerSetRefs = FindLayerSetsContainingLayer(model, entityIndex);
        foreach (var layerSetIndex in layerSetRefs)
        {
            model.ForEachReferenceInArgument(layerSetIndex, 0, layerIndex =>
            {
                WriteMaterialLayerDescriptor(writer, model, layerIndex);
            });
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteMaterialConstituent(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);

        var materialName = string.Empty;
        model.ForEachReferenceInArgument(entityIndex, 2, materialIndex =>
        {
            materialName = model.ReadStringOrDefault(materialIndex, 0);
        });

        writer.WriteString("Name", materialName);
        writer.WriteString("Id", objectId);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteMaterialConstituentSet(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);
        writer.WriteStartArray("IfcMaterialConstituent");

        model.ForEachReferenceInArgument(entityIndex, 0, constituentIndex =>
        {
            var materialName = string.Empty;
            model.ForEachReferenceInArgument(constituentIndex, 2, materialIndex =>
            {
                materialName = model.ReadStringOrDefault(materialIndex, 0);
            });

            writer.WriteStartObject();
            writer.WriteString("Name", materialName);
            writer.WriteString("Id", objectId);
            writer.WriteEndObject();
        });

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteMaterialLayerSetUsage(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var objectId = EntityObjectId(model, entityIndex);
        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));

        writer.WriteStartObject(objectId);
        writer.WriteStartObject(expressType);

        var directionSense = model.ReadStringOrDefault(entityIndex, 2, "POSITIVE");
        writer.WriteString("DirectionSense", directionSense == "POSITIVE" ? "POSITIVE" : "NEGATIVE");

        writer.WriteStartArray("IfcMaterialLayer");
        model.ForEachReferenceInArgument(entityIndex, 0, layerSetIndex =>
        {
            model.ForEachReferenceInArgument(layerSetIndex, 0, layerIndex =>
            {
                WriteMaterialLayerDescriptor(writer, model, layerIndex);
            });
        });
        writer.WriteEndArray();

        writer.WriteString("LayerSetDirection", model.ReadStringOrDefault(entityIndex, 1, "AXIS1"));

        var layerSetName = string.Empty;
        model.ForEachReferenceInArgument(entityIndex, 0, layerSetIndex =>
        {
            layerSetName = model.ReadStringOrDefault(layerSetIndex, 1);
        });

        writer.WriteString("LayerSetName", layerSetName);
        writer.WriteNumber("OffsetFromReferenceLine", ReadDouble(model.GetArgument(entityIndex, 3)));
        writer.WriteString("id", objectId);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WritePipeSegmentType(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);

        var expressType = ToExpressTypeName(model.Table.GetEntityType(entityIndex));
        writer.WriteStartObject(expressType);

        writer.WriteStartArray("IfcPropertySet");
        ForEachPropertySet(writer, model, entityIndex);
        writer.WriteEndArray();

        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("PredefinedType", ReadTailEnum(model, entityIndex));
        writer.WriteString("Tag", ReadTag(model, entityIndex));
        writer.WriteString("id", globalId);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteSpaceType(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);
        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("PredefinedType", ReadTailEnum(model, entityIndex));
        writer.WriteString("Tag", ReadTag(model, entityIndex));
        writer.WriteString("id", globalId);
        writer.WriteEndObject();
    }

    private static void WriteBuildingElementType(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);

        writer.WriteStartArray("properties");
        ForEachPropertySetId(writer, model, entityIndex);
        writer.WriteEndArray();

        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("PredefinedType", ReadTailEnum(model, entityIndex));
        writer.WriteString("Tag", ReadTag(model, entityIndex));
        writer.WriteString("id", globalId);

        writer.WriteEndObject();
    }

    private static void WriteDistributionElementType(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);

        writer.WriteStartArray("properties");
        ForEachPropertySetId(writer, model, entityIndex);
        writer.WriteEndArray();

        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("Tag", ReadTag(model, entityIndex));
        writer.WriteString("id", globalId);

        writer.WriteEndObject();
    }

    private static void WriteFurnitureType(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);

        writer.WriteStartArray("properties");
        ForEachPropertySetId(writer, model, entityIndex);
        writer.WriteEndArray();

        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("PredefinedType", ReadTailEnum(model, entityIndex));
        writer.WriteString("Tag", ReadTag(model, entityIndex));
        writer.WriteString("id", globalId);

        writer.WriteEndObject();
    }

    private static void WritePropertySet(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);

        writer.WriteStartArray("IfcPropertySingleValue");
        model.ForEachReferenceInArgument(entityIndex, 4, propertyIndex =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", model.ReadStringOrDefault(propertyIndex, 0));
            writer.WriteString("NominalValue", ReadNominalValue(model, propertyIndex));
            writer.WriteEndObject();
        });
        writer.WriteEndArray();

        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("id", globalId);
        writer.WriteEndObject();
    }

    private static void WriteSimplePropertyDefinition(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        var globalId = model.ReadGlobalId(entityIndex);
        writer.WriteStartObject(globalId);

        writer.WriteStartArray("IfcPropertySingleValue");
        writer.WriteEndArray();

        writer.WriteString("Name", model.ReadStringOrDefault(entityIndex, 2));
        writer.WriteString("id", globalId);

        writer.WriteEndObject();
    }

    private static void WriteMaterialLayerDescriptor(Utf8JsonWriter writer, FastIfcDataModel model, int layerIndex)
    {
        var thickness = ReadDouble(model.GetArgument(layerIndex, 1)).ToString(CultureInfo.InvariantCulture);
        var materialName = string.Empty;
        model.ForEachReferenceInArgument(layerIndex, 0, materialIndex =>
        {
            materialName = model.ReadStringOrDefault(materialIndex, 0);
        });

        writer.WriteStartObject();
        writer.WriteString("LayerThickness", thickness);
        writer.WriteString("Name", materialName);
        writer.WriteEndObject();
    }

    private static IReadOnlyList<int> FindLayerSetsContainingLayer(FastIfcDataModel model, int layerIndex)
    {
        return model.GetMaterialLayerSetsContainingLayer(layerIndex);
    }

    private static void ForEachPropertySet(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        model.ForEachReferenceInArgument(entityIndex, 5, propertySetIndex =>
        {
            writer.WriteStartObject();
            writer.WriteString("xlink:href", model.ReadGlobalId(propertySetIndex));
            writer.WriteEndObject();
        });
    }

    private static void ForEachPropertySetId(Utf8JsonWriter writer, FastIfcDataModel model, int entityIndex)
    {
        model.ForEachReferenceInArgument(entityIndex, 5, propertySetIndex =>
        {
            writer.WriteStringValue(model.ReadGlobalId(propertySetIndex));
        });
    }

    private static string ReadTailEnum(FastIfcDataModel model, int entityIndex)
    {
        var count = model.Table.GetArgumentCount(entityIndex);
        for (var i = count - 1; i >= 0; i--)
        {
            var value = model.GetArgument(entityIndex, i);
            if (value.Kind == StepValueKind.Enum)
            {
                return value.Text;
            }
        }

        return string.Empty;
    }

    private static string ReadTag(FastIfcDataModel model, int entityIndex)
    {
        var count = model.Table.GetArgumentCount(entityIndex);
        for (var i = count - 1; i >= 0; i--)
        {
            var value = model.GetArgument(entityIndex, i);
            if (value.Kind == StepValueKind.String)
            {
                return value.Text;
            }
        }

        return string.Empty;
    }

    private static string ReadNominalValue(FastIfcDataModel model, int propertySingleValueEntityIndex)
    {
        var value = model.GetArgument(propertySingleValueEntityIndex, 2);
        return value.Kind switch
        {
            StepValueKind.String => value.Text,
            StepValueKind.Enum => value.Text,
            StepValueKind.Number => value.Number.ToString(CultureInfo.InvariantCulture),
            StepValueKind.Raw => value.Text,
            _ => string.Empty
        };
    }

    private static double ReadDouble(StepValue value)
    {
        return value.Kind == StepValueKind.Number ? value.Number : 0;
    }

    private static string ToExpressTypeName(string upperTypeName)
    {
        if (ExpressTypeNames.TryGetValue(upperTypeName, out var expressTypeName))
        {
            return expressTypeName;
        }

        return upperTypeName;
    }
}
