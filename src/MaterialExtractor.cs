using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

internal class MaterialExtractor
{
    public static readonly JsonWriterOptions Jwo = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly FileInfo _jsonTargetFile;

    public MaterialExtractor(FileInfo jsonTargetFile)
    {
        _jsonTargetFile = jsonTargetFile;
    }

    public void Start(FileInfo ifcFileInfo)
    {
        using (var model = IfcStore.Open(ifcFileInfo.FullName, accessMode: Xbim.IO.XbimDBAccess.Read))
        using (var jsonWriter = new BimxJsonCreator(_jsonTargetFile))
        {
            foreach (var item in Array.Empty<IPersistEntity>()
                                    .Concat(model.Instances.OfType<IIfcMaterial>())
                                    .Concat(model.Instances.OfType<IIfcMaterialList>())
                                    .Concat(model.Instances.OfType<IIfcMaterialLayerSet>())
                                    .Concat(model.Instances.OfType<IIfcMaterialLayer>())
                                    .Concat(model.Instances.OfType<IIfcMaterialConstituent>())
                                    .Concat(model.Instances.OfType<IIfcMaterialConstituentSet>())
                                    .Concat(model.Instances.OfType<IIfcMaterialLayerSetUsage>())
                                    .Concat(model.Instances.OfType<IIfcSpaceType>())
                                    .Concat(model.Instances.OfType<IIfcColumnType>())
                                    .Concat(model.Instances.OfType<IIfcWallType>())
                                    .Concat(model.Instances.OfType<IIfcSlabType>())
                                    .Concat(model.Instances.OfType<IIfcCoveringType>())
                                    .Concat(model.Instances.OfType<IIfcStairFlightType>())
                                    .Concat(model.Instances.OfType<IIfcPlateType>())
                                    .Concat(model.Instances.OfType<IIfcMemberType>())
                                    .Concat(model.Instances.OfType<IIfcCurtainWallType>())
                                    .Concat(model.Instances.OfType<IIfcDistributionElementType>())
                                    .Concat(model.Instances.OfType<IIfcBuildingElementProxyType>())
                                    .Concat(model.Instances.OfType<IIfcPipeSegmentType>())
                                    .Concat(model.Instances.OfType<IIfcFurnitureType>())
                                    .Concat(model.Instances.OfType<IIfcRelDefinesByType>())
                                    .Concat(model.Instances.OfType<IIfcPropertySingleValue>())
                                    .Concat(model.Instances.OfType<IIfcPropertySet>())
                                    .Concat(model.Instances.OfType<IIfcRelDefinesByProperties>())
                                    .Concat(model.Instances.OfType<IIfcDoorLiningProperties>())
                                    .Concat(model.Instances.OfType<IIfcDoorPanelProperties>())
                                    .Concat(model.Instances.OfType<IIfcWindowLiningProperties>()))
            {
                jsonWriter.BTask(item);
            }

            jsonWriter.CreateJson();
        }
    }
}
