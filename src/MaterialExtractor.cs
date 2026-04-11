using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

internal class MaterialExtractor(FileInfo jsonTargetFile)
{
    public static readonly JsonWriterOptions Jwo = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly HashSet<Type> TargetTypes = new()
    {
        typeof(IIfcMaterial),
        typeof(IIfcMaterialList),
        typeof(IIfcMaterialLayerSet),
        typeof(IIfcMaterialLayer),
        typeof(IIfcMaterialConstituent),
        typeof(IIfcMaterialConstituentSet),
        typeof(IIfcMaterialLayerSetUsage),
        typeof(IIfcSpaceType),
        typeof(IIfcColumnType),
        typeof(IIfcWallType),
        typeof(IIfcSlabType),
        typeof(IIfcCoveringType),
        typeof(IIfcStairFlightType),
        typeof(IIfcPlateType),
        typeof(IIfcMemberType),
        typeof(IIfcCurtainWallType),
        typeof(IIfcDistributionElementType),
        typeof(IIfcBuildingElementProxyType),
        typeof(IIfcPipeSegmentType),
        typeof(IIfcFurnitureType),
        typeof(IIfcRelDefinesByType),
        typeof(IIfcPropertySingleValue),
        typeof(IIfcPropertySet),
        typeof(IIfcRelDefinesByProperties),
        typeof(IIfcDoorLiningProperties),
        typeof(IIfcDoorPanelProperties),
        typeof(IIfcWindowLiningProperties)
    };

    public void Start(FileInfo ifcFileInfo)
    {
        using var model = IfcStore.Open(ifcFileInfo.FullName, accessMode: Xbim.IO.XbimDBAccess.Read);
        using var jsonWriter = new BimxJsonCreator(jsonTargetFile);

        foreach (var item in model.Instances)
        {
            var itemType = item.GetType();
            foreach (var targetType in TargetTypes)
            {
                if (targetType.IsAssignableFrom(itemType))
                {
                    jsonWriter.BTask(item);
                    break;
                }
            }
        }

        jsonWriter.CreateJson();
    }
}
