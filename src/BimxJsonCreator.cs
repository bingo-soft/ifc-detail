using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

public sealed class BimxJsonCreator : IDisposable
{
    private static readonly JsonWriterOptions Jwo = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false
    };

    private static readonly Dictionary<string, JsonEncodedText> PropertyNameCache = new()
    {
        ["Name"] = JsonEncodedText.Encode("Name"),
        ["Tag"] = JsonEncodedText.Encode("Tag"),
        ["PredefinedType"] = JsonEncodedText.Encode("PredefinedType"),
        ["id"] = JsonEncodedText.Encode("id"),
        ["Id"] = JsonEncodedText.Encode("Id"),
        ["IfcPropertySet"] = JsonEncodedText.Encode("IfcPropertySet"),
        ["xlink:href"] = JsonEncodedText.Encode("xlink:href"),
        ["IfcMaterial"] = JsonEncodedText.Encode("IfcMaterial"),
        ["DirectionSense"] = JsonEncodedText.Encode("DirectionSense"),
        ["IfcMaterialLayer"] = JsonEncodedText.Encode("IfcMaterialLayer"),
        ["LayerThickness"] = JsonEncodedText.Encode("LayerThickness"),
        ["LayerSetDirection"] = JsonEncodedText.Encode("LayerSetDirection"),
        ["LayerSetName"] = JsonEncodedText.Encode("LayerSetName"),
        ["OffsetFromReferenceLine"] = JsonEncodedText.Encode("OffsetFromReferenceLine"),
        ["IfcMaterialConstituent"] = JsonEncodedText.Encode("IfcMaterialConstituent"),
        ["properties"] = JsonEncodedText.Encode("properties"),
        ["ConstructionType"] = JsonEncodedText.Encode("ConstructionType"),
        ["OperationType"] = JsonEncodedText.Encode("OperationType"),
        ["ParameterTakesPrecedence"] = JsonEncodedText.Encode("ParameterTakesPrecedence"),
        ["Sizeable"] = JsonEncodedText.Encode("Sizeable"),
        ["IfcPropertySingleValue"] = JsonEncodedText.Encode("IfcPropertySingleValue"),
        ["NominalValue"] = JsonEncodedText.Encode("NominalValue")
    };

    private readonly FileInfo _targetFile;
    private readonly ArrayBufferWriter<byte> _bufferMaterial;
    private readonly Utf8JsonWriter _writerMaterial;

    private readonly ArrayBufferWriter<byte> _bufferType;
    private readonly Utf8JsonWriter _writerType;

    private readonly ArrayBufferWriter<byte> _bufferProperty;
    private readonly Utf8JsonWriter _writerProperty;

    private readonly StringBuilder _idBuilder = new(64);

    public BimxJsonCreator(FileInfo jsonTargetFile)
    {
        _targetFile = jsonTargetFile;

        _bufferMaterial = new ArrayBufferWriter<byte>(1024 * 1024);
        _writerMaterial = new Utf8JsonWriter(_bufferMaterial, Jwo);

        _bufferType = new ArrayBufferWriter<byte>(512 * 1024);
        _writerType = new Utf8JsonWriter(_bufferType, Jwo);

        _bufferProperty = new ArrayBufferWriter<byte>(512 * 1024);
        _writerProperty = new Utf8JsonWriter(_bufferProperty, Jwo);

        _writerMaterial.WriteStartObject();
        _writerType.WriteStartObject();
        _writerProperty.WriteStartObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteStringCached(Utf8JsonWriter writer, string propertyName, string value)
    {
        if (PropertyNameCache.TryGetValue(propertyName, out var cached))
        {
            writer.WriteString(cached, value);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteStartArrayCached(Utf8JsonWriter writer, string propertyName)
    {
        if (PropertyNameCache.TryGetValue(propertyName, out var cached))
        {
            writer.WriteStartArray(cached);
        }
        else
        {
            writer.WriteStartArray(propertyName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void BTask(IPersistEntity item)
    {
        _idBuilder.Clear();
        _idBuilder.Append(item.ExpressType.Name);
        _idBuilder.Append('_');
        _idBuilder.Append(item.EntityLabel);
        var id = _idBuilder.ToString();

        if (item is IIfcPipeSegmentType ifcPipeSegmentType)
        {
            var globalIdStr = ifcPipeSegmentType.GlobalId.Value.ToString();
            _writerMaterial.WriteStartObject(globalIdStr);
            _writerMaterial.WriteStartObject(ifcPipeSegmentType.ExpressType.Name);

            WriteStartArrayCached(_writerMaterial, "IfcPropertySet");
            foreach (var item1 in ifcPipeSegmentType.HasPropertySets)
            {
                _writerMaterial.WriteStartObject();
                WriteStringCached(_writerMaterial, "xlink:href", item1.GlobalId.Value.ToString());
                _writerMaterial.WriteEndObject();
            }
            _writerMaterial.WriteEndArray();

            WriteStringCached(_writerMaterial, "Name", ifcPipeSegmentType.Name);
            WriteStringCached(_writerMaterial, "PredefinedType", ifcPipeSegmentType.PredefinedType.ToString());
            WriteStringCached(_writerMaterial, "Tag", ifcPipeSegmentType.Tag);
            WriteStringCached(_writerMaterial, "id", globalIdStr);

            _writerMaterial.WriteEndObject();
            _writerMaterial.WriteEndObject();
            return;
        }

        switch (item)
        {
            case IIfcMaterial material:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(material.ExpressType.Name);

                    _writerMaterial.WriteString("Name", material.Name);
                    _writerMaterial.WriteString("Id", id);

                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            case IIfcMaterialList materialList:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(materialList.ExpressType.Name);
                    _writerMaterial.WriteStartArray("IfcMaterial");
                    foreach (var material in materialList.Materials)
                    {
                        _writerMaterial.WriteStartObject();
                        _writerMaterial.WriteString("Name", material.Name);
                        _writerMaterial.WriteEndObject();
                    }

                    _writerMaterial.WriteEndArray();
                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            case IIfcMaterialLayerSetUsage materialLayerSetUsage:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(materialLayerSetUsage.ExpressType.Name);

                    _writerMaterial.WriteString("DirectionSense", materialLayerSetUsage.DirectionSense == IfcDirectionSenseEnum.POSITIVE ? "POSITIVE" : "NEGATIVE");

                    _writerMaterial.WriteStartArray("IfcMaterialLayer");
                    foreach (var materialLayer in materialLayerSetUsage.ForLayerSet.MaterialLayers)
                    {
                        _writerMaterial.WriteStartObject();
                        _writerMaterial.WriteString("LayerThickness", materialLayer.LayerThickness.Value.ToString());
                        _writerMaterial.WriteString("Name", materialLayer.Material.Name);
                        _writerMaterial.WriteEndObject();
                    }
                    _writerMaterial.WriteEndArray();

                    _writerMaterial.WriteString("LayerSetDirection", materialLayerSetUsage.LayerSetDirection switch
                    {
                        IfcLayerSetDirectionEnum.AXIS1 => "AXIS1",
                        IfcLayerSetDirectionEnum.AXIS2 => "AXIS2",
                        IfcLayerSetDirectionEnum.AXIS3 => "AXIS3",
                        _ => "AXIS1"
                    });
                    _writerMaterial.WriteString("LayerSetName", materialLayerSetUsage.ForLayerSet.LayerSetName);
                    _writerMaterial.WriteNumber("OffsetFromReferenceLine", materialLayerSetUsage.OffsetFromReferenceLine);
                    _writerMaterial.WriteString("id", id);

                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            case IIfcMaterialLayerSet ifcMaterialLayerSet:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(ifcMaterialLayerSet.ExpressType.Name);

                    _writerMaterial.WriteStartArray("IfcMaterialLayer");
                    foreach (var materialLayer in ifcMaterialLayerSet.MaterialLayers)
                    {
                        _writerMaterial.WriteStartObject();
                        _writerMaterial.WriteString("LayerThickness", materialLayer.LayerThickness.Value.ToString());
                        _writerMaterial.WriteString("Name", materialLayer.Material.Name);
                        _writerMaterial.WriteEndObject();
                    }
                    _writerMaterial.WriteEndArray();

                    _writerMaterial.WriteString("LayerSetName", ifcMaterialLayerSet.LayerSetName);
                    _writerMaterial.WriteString("id", id);

                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            case IIfcMaterialConstituent MaterialConstituent:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(MaterialConstituent.ExpressType.Name);

                    _writerMaterial.WriteString("Name", MaterialConstituent.Material.Name);
                    _writerMaterial.WriteString("Id", id);

                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            case IIfcMaterialLayer ifcMaterialLayer:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(ifcMaterialLayer.ExpressType.Name);

                    _writerMaterial.WriteStartArray("IfcMaterialLayer");
                    foreach (var materialLayer in ifcMaterialLayer.ToMaterialLayerSet.MaterialLayers)
                    {
                        _writerMaterial.WriteStartObject();
                        _writerMaterial.WriteString("LayerThickness", materialLayer.LayerThickness.Value.ToString());
                        _writerMaterial.WriteString("Name", materialLayer.Material.Name);
                        _writerMaterial.WriteEndObject();
                    }
                    _writerMaterial.WriteEndArray();

                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            case IIfcMaterialConstituentSet ifcMaterialConstituentSet:
                {
                    _writerMaterial.WriteStartObject(id);
                    _writerMaterial.WriteStartObject(ifcMaterialConstituentSet.ExpressType.Name);

                    _writerMaterial.WriteStartArray("IfcMaterialConstituent");
                    foreach (var materialConstituent in ifcMaterialConstituentSet.MaterialConstituents)
                    {
                        _writerMaterial.WriteStartObject();

                        _writerMaterial.WriteString("Name", materialConstituent.Material.Name);
                        _writerMaterial.WriteString("Id", id);

                        _writerMaterial.WriteEndObject();
                    }
                    _writerMaterial.WriteEndArray();

                    _writerMaterial.WriteEndObject();
                    _writerMaterial.WriteEndObject();

                    break;
                }
            // TYPES
            case IIfcSpaceType ifcSpaceType:
                {
                    var globalIdStr = ifcSpaceType.GlobalId.Value.ToString();
                    _writerType.WriteStartObject(globalIdStr);

                    _writerType.WriteString("Name", ifcSpaceType.Name);
                    _writerType.WriteString("PredefinedType", ifcSpaceType.PredefinedType.ToString());
                    _writerType.WriteString("Tag", ifcSpaceType.Tag);
                    _writerType.WriteString("id", globalIdStr);

                    _writerType.WriteEndObject();

                    break;
                }
            case IIfcColumnType ifcColumnType:
                {
                    ConvertTypeToJson(_writerType, ifcColumnType, ifcColumnType.PredefinedType.ToString());
                    break;
                }
            case IIfcWallType ifcWallType:
                {
                    ConvertTypeToJson(_writerType, ifcWallType, ifcWallType.PredefinedType.ToString());
                    break;
                }
            case IIfcSlabType ifcSlabType:
                {
                    ConvertTypeToJson(_writerType, ifcSlabType, ifcSlabType.PredefinedType.ToString());
                    break;
                }
            case IIfcCoveringType ifcCoveringType:
                {
                    ConvertTypeToJson(_writerType, ifcCoveringType, ifcCoveringType.PredefinedType.ToString());
                    break;
                }
            case IIfcStairFlightType ifcStairFlightType:
                {
                    ConvertTypeToJson(_writerType, ifcStairFlightType, ifcStairFlightType.PredefinedType.ToString());
                    break;
                }
            case IIfcPlateType ifcPlateType:
                {
                    ConvertTypeToJson(_writerType, ifcPlateType, ifcPlateType.PredefinedType.ToString());
                    break;
                }
            case IIfcMemberType ifcMemberType:
                {
                    ConvertTypeToJson(_writerType, ifcMemberType, ifcMemberType.PredefinedType.ToString());
                    break;
                }
            case IIfcCurtainWallType ifcCurtainWallType:
                {
                    ConvertTypeToJson(_writerType, ifcCurtainWallType, ifcCurtainWallType.PredefinedType.ToString());
                    break;
                }
            case IIfcDistributionElementType ifcDistributionElementType:
                {
                    var globalIdStr = ifcDistributionElementType.GlobalId.Value.ToString();
                    _writerType.WriteStartObject(globalIdStr);

                    _writerType.WritePropertiesArray(ifcDistributionElementType.HasPropertySets);

                    _writerType.WriteString("Name", ifcDistributionElementType.Name);
                    _writerType.WriteString("Tag", ifcDistributionElementType.Tag);
                    _writerType.WriteString("id", globalIdStr);

                    _writerType.WriteEndObject();
                    break;
                }
            case IIfcBuildingElementProxyType ifcBuildingElementProxyType:
                {
                    ConvertTypeToJson(_writerType, ifcBuildingElementProxyType, ifcBuildingElementProxyType.PredefinedType.ToString());
                    break;
                }
            case IIfcFurnitureType ifcFurnitureType:
                {
                    var globalIdStr = ifcFurnitureType.GlobalId.Value.ToString();
                    _writerType.WriteStartObject(globalIdStr);

                    _writerType.WritePropertiesArray(ifcFurnitureType.HasPropertySets);

                    _writerType.WriteString("Name", ifcFurnitureType.Name);
                    _writerType.WriteString("PredefinedType", ifcFurnitureType.PredefinedType.ToString());
                    _writerType.WriteString("Tag", ifcFurnitureType.Tag);
                    _writerType.WriteString("id", globalIdStr);

                    _writerType.WriteEndObject();
                    break;
                }
            case IIfcWindowStyle ifcWindowStyle:
                {
                    var globalIdStr = ifcWindowStyle.GlobalId.Value.ToString();
                    _writerType.WriteStartObject(globalIdStr);

                    _writerType.WriteString("ConstructionType", ifcWindowStyle.ConstructionType.ToString());

                    _writerType.WritePropertiesArray(ifcWindowStyle.HasPropertySets);

                    _writerType.WriteString("Name", ifcWindowStyle.Name);
                    _writerType.WriteString("OperationType", ifcWindowStyle.OperationType.ToString());
                    _writerType.WriteString("ParameterTakesPrecedence", ifcWindowStyle.ParameterTakesPrecedence.ToString());
                    _writerType.WriteString("Sizeable", ifcWindowStyle.Sizeable.ToString());
                    _writerType.WriteString("Tag", ifcWindowStyle.Tag);
                    _writerType.WriteString("id", globalIdStr);

                    _writerType.WriteEndObject();
                    break;
                }
            // Properties
            case IIfcPropertySet ifcPropertySet:
                {
                    var globalIdStr = ifcPropertySet.GlobalId.Value.ToString();
                    _writerProperty.WriteStartObject(globalIdStr);

                    _writerProperty.WriteStartArray("IfcPropertySingleValue");
                    foreach (var item1 in ifcPropertySet.HasProperties)
                    {
                        _writerProperty.WriteStartObject();
                        _writerProperty.WriteString("Name", item1.Name);
                        _writerProperty.WriteString("NominalValue", ((IIfcPropertySingleValue)item1).NominalValue?.ToString());
                        _writerProperty.WriteEndObject();
                    }
                    _writerProperty.WriteEndArray();

                    _writerProperty.WriteString("Name", ifcPropertySet.Name);
                    _writerProperty.WriteString("id", globalIdStr);

                    _writerProperty.WriteEndObject();

                    break;
                }
            case IIfcDoorLiningProperties ifcDoorLiningProperties:
                {
                    var globalIdStr = ifcDoorLiningProperties.GlobalId.Value.ToString();
                    _writerProperty.WriteStartObject(globalIdStr);

                    _writerProperty.WriteStartArray("IfcPropertySingleValue");
                    foreach (var item1 in ifcDoorLiningProperties.PropertySetDefinitions)
                    {
                        _writerProperty.WriteStartObject();
                        _writerProperty.WriteString("Name", item1.Name);
                        _writerProperty.WriteEndObject();
                    }
                    _writerProperty.WriteEndArray();

                    _writerProperty.WriteString("Name", ifcDoorLiningProperties.Name);
                    _writerProperty.WriteString("id", globalIdStr);

                    _writerProperty.WriteEndObject();

                    break;
                }
            case IIfcDoorPanelProperties ifcDoorPanelProperties:
                {
                    var globalIdStr = ifcDoorPanelProperties.GlobalId.Value.ToString();
                    _writerProperty.WriteStartObject(globalIdStr);

                    _writerProperty.WriteStartArray("IfcPropertySingleValue");
                    foreach (var item1 in ifcDoorPanelProperties.PropertySetDefinitions)
                    {
                        _writerProperty.WriteStartObject();
                        _writerProperty.WriteString("Name", item1.Name);
                        _writerProperty.WriteEndObject();
                    }
                    _writerProperty.WriteEndArray();

                    _writerProperty.WriteString("Name", ifcDoorPanelProperties.Name);
                    _writerProperty.WriteString("id", globalIdStr);

                    _writerProperty.WriteEndObject();

                    break;
                }
            case IIfcWindowLiningProperties ifcWindowLiningProperties:
                {
                    var globalIdStr = ifcWindowLiningProperties.GlobalId.Value.ToString();
                    _writerProperty.WriteStartObject(globalIdStr);

                    _writerProperty.WriteStartArray("IfcPropertySingleValue");
                    foreach (var item1 in ifcWindowLiningProperties.PropertySetDefinitions)
                    {
                        _writerProperty.WriteStartObject();
                        _writerProperty.WriteString("Name", item1.Name);
                        _writerProperty.WriteEndObject();
                    }
                    _writerProperty.WriteEndArray();

                    _writerProperty.WriteString("Name", ifcWindowLiningProperties.Name);
                    _writerProperty.WriteString("id", globalIdStr);

                    _writerProperty.WriteEndObject();

                    break;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ConvertTypeToJson(Utf8JsonWriter writer, IIfcBuildingElementType ifcMemberType, string predefinedType)
        {
            var globalIdStr = ifcMemberType.GlobalId.Value.ToString();
            writer.WriteStartObject(globalIdStr);

            writer.WritePropertiesArray(ifcMemberType.HasPropertySets);

            writer.WriteString("Name", ifcMemberType.Name);
            writer.WriteString("PredefinedType", predefinedType);
            writer.WriteString("Tag", ifcMemberType.Tag);
            writer.WriteString("id", globalIdStr);

            writer.WriteEndObject();
        }
    }

    public void CreateJson()
    {
        _writerMaterial.WriteEndObject();
        _writerType.WriteEndObject();
        _writerProperty.WriteEndObject();

        _writerMaterial.Flush();
        _writerType.Flush();
        _writerProperty.Flush();

        using var stream = new FileStream(
            _targetFile.FullName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920);

        using var writer = new Utf8JsonWriter(stream, Jwo);
        writer.WriteStartObject();

        writer.WritePropertyName("materials");
        writer.WriteRawValue(_bufferMaterial.WrittenSpan, skipInputValidation: true);

        writer.WritePropertyName("types");
        writer.WriteRawValue(_bufferType.WrittenSpan, skipInputValidation: true);

        writer.WritePropertyName("properties");
        writer.WriteRawValue(_bufferProperty.WrittenSpan, skipInputValidation: true);

        writer.WriteEndObject();
    }

    public void Dispose()
    {
        _writerMaterial?.Dispose();
        _writerType?.Dispose();
        _writerProperty?.Dispose();
    }
}

public static class IfcExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePropertiesArray(this Utf8JsonWriter writer, IItemSet<IIfcPropertySetDefinition> props)
    {
        writer.WriteStartArray("properties");
        foreach (var item in props)
        {
            writer.WriteStringValue(item.GlobalId.ToString());
        }
        writer.WriteEndArray();
    }
}