using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

public sealed class BimxJsonCreator : IDisposable
{
    private static readonly JsonWriterOptions Jwo = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly BoundedChannelOptions Bco = new(1000) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = true };
    private readonly FileInfo _targetFile;
    private readonly ArrayBufferWriter<byte> _bufferMaterial;
    private readonly Utf8JsonWriter _writerMaterial;

    private readonly ArrayBufferWriter<byte> _bufferType;
    private readonly Utf8JsonWriter _writerType;

    private readonly ArrayBufferWriter<byte> _bufferProperty;
    private readonly Utf8JsonWriter _writerProperty;

    private readonly Channel<IPersistEntity> _channel;
    private readonly Task _backgroundTask;

    public BimxJsonCreator(FileInfo jsonTargetFile)
    {
        _targetFile = jsonTargetFile;

        _bufferMaterial = new ArrayBufferWriter<byte>(1000);
        _writerMaterial = new Utf8JsonWriter(_bufferMaterial, Jwo);

        _bufferType = new ArrayBufferWriter<byte>(1000);
        _writerType = new Utf8JsonWriter(_bufferType, Jwo);

        _bufferProperty = new ArrayBufferWriter<byte>(1000);
        _writerProperty = new Utf8JsonWriter(_bufferProperty, Jwo);

        _writerMaterial.WriteStartObject();
        _writerType.WriteStartObject();
        _writerProperty.WriteStartObject();

        // _channel = Channel.CreateBounded<IPersistEntity>(Bco);
        // _backgroundTask = Task.Factory.StartNew(BTask, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent, TaskScheduler.Default).Unwrap();
    }

    public void BTask(IPersistEntity item)
    {
        var id = $"{item.ExpressType.Name}_{item.EntityLabel}";

        if (item is IIfcPipeSegmentType ifcPipeSegmentType)
        {
            _writerMaterial.WriteStartObject(ifcPipeSegmentType.GlobalId.Value.ToString());
            _writerMaterial.WriteStartObject(ifcPipeSegmentType.ExpressType.Name);

            _writerMaterial.WriteStartArray("IfcPropertySet");
            foreach (var item1 in ifcPipeSegmentType.HasPropertySets)
            {
                _writerMaterial.WriteStartObject();
                _writerMaterial.WriteString("xlink:href", item1.GlobalId.Value.ToString());
                _writerMaterial.WriteEndObject();
            }
            _writerMaterial.WriteEndArray();

            _writerMaterial.WriteString("Name", ifcPipeSegmentType.Name);
            _writerMaterial.WriteString("PredefinedType", ifcPipeSegmentType.PredefinedType.ToString());
            _writerMaterial.WriteString("Tag", ifcPipeSegmentType.Tag);
            _writerMaterial.WriteString("id", ifcPipeSegmentType.GlobalId.Value.ToString());

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
                _writerType.WriteStartObject(ifcSpaceType.GlobalId.Value.ToString());
                _writerType.WriteStartObject(ifcSpaceType.ExpressType.Name);

                _writerType.WriteString("Name", ifcSpaceType.Name);
                _writerType.WriteString("PredefinedType", ifcSpaceType.PredefinedType.ToString());
                _writerType.WriteString("Tag", ifcSpaceType.Tag);
                _writerType.WriteString("id", ifcSpaceType.GlobalId.Value.ToString());

                _writerType.WriteEndObject();
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
                _writerType.WriteStartObject(ifcDistributionElementType.GlobalId.Value.ToString());
                _writerType.WriteStartObject(ifcDistributionElementType.ExpressType.Name);

                _writerType.WriteStartArray("IfcPropertySet");
                foreach (var item1 in ifcDistributionElementType.HasPropertySets)
                {
                    _writerType.WriteStartObject();
                    _writerType.WriteString("xlink:href", item1.GlobalId.Value.ToString());
                    _writerType.WriteEndObject();
                }
                _writerType.WriteEndArray();

                _writerType.WriteString("Name", ifcDistributionElementType.Name);
                _writerType.WriteString("Tag", ifcDistributionElementType.Tag);
                _writerType.WriteString("id", ifcDistributionElementType.GlobalId.Value.ToString());

                _writerType.WriteEndObject();
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
                _writerType.WriteStartObject(ifcFurnitureType.GlobalId.Value.ToString());
                _writerType.WriteStartObject(ifcFurnitureType.ExpressType.Name);

                _writerType.WriteStartArray("IfcPropertySet");
                foreach (var item1 in ifcFurnitureType.HasPropertySets)
                {
                    _writerType.WriteStartObject();
                    _writerType.WriteString("xlink:href", item1.GlobalId.Value.ToString());
                    _writerType.WriteEndObject();
                }
                _writerType.WriteEndArray();

                _writerType.WriteString("Name", ifcFurnitureType.Name);
                _writerType.WriteString("PredefinedType", ifcFurnitureType.PredefinedType.ToString());
                _writerType.WriteString("Tag", ifcFurnitureType.Tag);
                _writerType.WriteString("id", ifcFurnitureType.GlobalId.Value.ToString());

                _writerType.WriteEndObject();
                _writerType.WriteEndObject();
                break;
            }
            case IIfcWindowStyle ifcWindowStyle:
            {
                _writerType.WriteStartObject(ifcWindowStyle.GlobalId.Value.ToString());
                _writerType.WriteStartObject(ifcWindowStyle.ExpressType.Name);

                _writerType.WriteString("ConstructionType", ifcWindowStyle.ConstructionType.ToString());

                _writerType.WriteStartArray("IfcPropertySet");
                foreach (var item1 in ifcWindowStyle.HasPropertySets)
                {
                    _writerType.WriteStartObject();
                    _writerType.WriteString("xlink:href", item1.GlobalId.Value.ToString());
                    _writerType.WriteEndObject();
                }
                _writerType.WriteEndArray();

                _writerType.WriteString("Name", ifcWindowStyle.Name);
                _writerType.WriteString("OperationType", ifcWindowStyle.OperationType.ToString());
                _writerType.WriteString("ParameterTakesPrecedence", ifcWindowStyle.ParameterTakesPrecedence.ToString());
                _writerType.WriteString("Sizeable", ifcWindowStyle.Sizeable.ToString());
                _writerType.WriteString("Tag", ifcWindowStyle.Tag);
                _writerType.WriteString("id", ifcWindowStyle.GlobalId.Value.ToString());

                _writerType.WriteEndObject();
                _writerType.WriteEndObject();
                break;
            }
            // Properties
            case IIfcPropertySet ifcPropertySet:
            {
                _writerProperty.WriteStartObject(ifcPropertySet.GlobalId.Value.ToString());

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
                _writerProperty.WriteString("id", ifcPropertySet.GlobalId.Value.ToString());

                _writerProperty.WriteEndObject();

                break;
            }
            case IIfcDoorLiningProperties ifcDoorLiningProperties:
            {
                _writerProperty.WriteStartObject(ifcDoorLiningProperties.GlobalId.Value.ToString());

                _writerProperty.WriteStartArray("IfcPropertySingleValue");
                foreach (var item1 in ifcDoorLiningProperties.PropertySetDefinitions)
                {
                    _writerProperty.WriteStartObject();
                    _writerProperty.WriteString("Name", item1.Name);
                    _writerProperty.WriteEndObject();
                }
                _writerProperty.WriteEndArray();

                _writerProperty.WriteString("Name", ifcDoorLiningProperties.Name);
                _writerProperty.WriteString("id", ifcDoorLiningProperties.GlobalId.Value.ToString());

                _writerProperty.WriteEndObject();

                break;
            }
            case IIfcDoorPanelProperties ifcDoorPanelProperties:
            {
                _writerProperty.WriteStartObject(ifcDoorPanelProperties.GlobalId.Value.ToString());

                _writerProperty.WriteStartArray("IfcPropertySingleValue");
                foreach (var item1 in ifcDoorPanelProperties.PropertySetDefinitions)
                {
                    _writerProperty.WriteStartObject();
                    _writerProperty.WriteString("Name", item1.Name);
                    _writerProperty.WriteEndObject();
                }
                _writerProperty.WriteEndArray();

                _writerProperty.WriteString("Name", ifcDoorPanelProperties.Name);
                _writerProperty.WriteString("id", ifcDoorPanelProperties.GlobalId.Value.ToString());

                _writerProperty.WriteEndObject();

                break;
            }
            case IIfcWindowLiningProperties ifcWindowLiningProperties:
            {
                _writerProperty.WriteStartObject(ifcWindowLiningProperties.GlobalId.Value.ToString());

                _writerProperty.WriteStartArray("IfcPropertySingleValue");
                foreach (var item1 in ifcWindowLiningProperties.PropertySetDefinitions)
                {
                    _writerProperty.WriteStartObject();
                    _writerProperty.WriteString("Name", item1.Name);
                    _writerProperty.WriteEndObject();
                }
                _writerProperty.WriteEndArray();

                _writerProperty.WriteString("Name", ifcWindowLiningProperties.Name);
                _writerProperty.WriteString("id", ifcWindowLiningProperties.GlobalId.Value.ToString());

                _writerProperty.WriteEndObject();

                break;
            }
        }

        static void ConvertTypeToJson(Utf8JsonWriter writer, IIfcBuildingElementType ifcMemberType, string predefinedType)
        {
            writer.WriteStartObject(ifcMemberType.GlobalId.Value.ToString());
            writer.WriteStartObject(ifcMemberType.ExpressType.Name);

            writer.WriteStartArray("IfcPropertySet");
            foreach (var item1 in ifcMemberType.HasPropertySets)
            {
                writer.WriteStartObject();
                writer.WriteString("xlink:href", item1.GlobalId.Value.ToString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteString("Name", ifcMemberType.Name);
            writer.WriteString("PredefinedType", predefinedType);
            writer.WriteString("Tag", ifcMemberType.Tag);
            writer.WriteString("id", ifcMemberType.GlobalId.Value.ToString());

            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }

    public void CreateJson()
    {
        _writerMaterial.WriteEndObject();
        _writerType.WriteEndObject();
        _writerProperty.WriteEndObject();

        _writerMaterial.Flush();
        var jsonMaterial = Encoding.UTF8.GetString(_bufferMaterial.WrittenSpan);

        _writerType.Flush();
        var jsonType = Encoding.UTF8.GetString(_bufferType.WrittenSpan);

        _writerProperty.Flush();
        var jsonProperties = Encoding.UTF8.GetString(_bufferProperty.WrittenSpan);

        using (var stream = File.OpenWrite(_targetFile.FullName))
        using (var writer = new Utf8JsonWriter(stream, Jwo))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("materials");
            writer.WriteRawValue(jsonMaterial);

            writer.WritePropertyName("types");
            writer.WriteRawValue(jsonType);

            writer.WritePropertyName("properties");
            writer.WriteRawValue(jsonProperties);

            writer.WriteEndObject();
        }
    }


    /*
    public async Task WriteItem(IPersistEntity entity)
    {
        while (!await _channel.Writer.WaitToWriteAsync())
        { }

        await _channel.Writer.WriteAsync(entity);
    }

    public async Task Complite()
    {
        _channel.Writer.Complete();
        await _backgroundTask;
    }
    */

    public void Dispose()
    {
        _writerMaterial?.Dispose();
        _writerType?.Dispose();
        _writerProperty?.Dispose();
    }
}

public static class BimXExtensions
{
    public static string AsString(this IfcSpaceTypeEnum enum1)
    {
        return enum1 switch
        {
            IfcSpaceTypeEnum.SPACE => "SPACE",
            IfcSpaceTypeEnum.PARKING => "PARKING",
            IfcSpaceTypeEnum.GFA => "GFA",
            IfcSpaceTypeEnum.INTERNAL => "INTERNAL",
            IfcSpaceTypeEnum.EXTERNAL => "EXTERNAL",
            IfcSpaceTypeEnum.USERDEFINED => "USERDEFINED",
            IfcSpaceTypeEnum.NOTDEFINED => "NOTDEFINED",
            _ => "NOTDEFINED"
        };
    }

    public static string AsString(this IfcColumnTypeEnum enum1)
    {
        return enum1 switch
        {
            IfcColumnTypeEnum.COLUMN => "COLUMN",
            IfcColumnTypeEnum.PILASTER => "PILASTER",
            IfcColumnTypeEnum.USERDEFINED => "USERDEFINED",
            IfcColumnTypeEnum.NOTDEFINED => "NOTDEFINED",
            _ => "NOTDEFINED"
        };
    }

    public static string AsString(this IfcWallTypeEnum enum1)
    {
        return enum1 switch
        {
            IfcWallTypeEnum.MOVABLE => "MOVABLE",
            IfcWallTypeEnum.PARAPET => "PARAPET",
            IfcWallTypeEnum.PARTITIONING => "PARTITIONING",
            IfcWallTypeEnum.PLUMBINGWALL => "PLUMBINGWALL",
            IfcWallTypeEnum.SHEAR => "SHEAR",
            IfcWallTypeEnum.SOLIDWALL => "SOLIDWALL",
            IfcWallTypeEnum.STANDARD => "STANDARD",
            IfcWallTypeEnum.POLYGONAL => "POLYGONAL",
            IfcWallTypeEnum.ELEMENTEDWALL => "ELEMENTEDWALL",
            IfcWallTypeEnum.USERDEFINED => "USERDEFINED",
            IfcWallTypeEnum.NOTDEFINED => "NOTDEFINED",
            _ => "NOTDEFINED"
        };
    }

    public static string AsString(this IfcSlabTypeEnum enum1)
    {
        return enum1 switch
        {
            IfcSlabTypeEnum.FLOOR => "FLOOR",
            IfcSlabTypeEnum.ROOF => "RoROOFof",
            IfcSlabTypeEnum.LANDING => "LANDING",
            IfcSlabTypeEnum.BASESLAB => "BASESLAB",
            IfcSlabTypeEnum.USERDEFINED => "USERDEFINED",
            IfcSlabTypeEnum.NOTDEFINED => "NOTDEFINED",
            _ => "UNKNOWN"
        };
    }
}
