using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

internal class FastMaterialExtractor(FileInfo jsonTargetFile)
{
    public void Start(FileInfo ifcFileInfo)
    {
        using var model = IfcStore.Open(ifcFileInfo.FullName, accessMode: Xbim.IO.XbimDBAccess.Read);
        using var jsonWriter = new BimxJsonCreator(jsonTargetFile);

        Process(model, jsonWriter);
        jsonWriter.CreateJson();
    }

    private static void Process(IfcStore model, BimxJsonCreator jsonWriter)
    {
        WriteSet(model.Instances.OfType<IIfcMaterial>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMaterialList>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMaterialLayerSet>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMaterialLayer>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMaterialConstituent>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMaterialConstituentSet>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMaterialLayerSetUsage>(), jsonWriter);

        WriteSet(model.Instances.OfType<IIfcSpaceType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcColumnType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcWallType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcSlabType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcCoveringType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcStairFlightType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcPlateType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcMemberType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcCurtainWallType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcDistributionElementType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcBuildingElementProxyType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcPipeSegmentType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcFurnitureType>(), jsonWriter);

        WriteSet(model.Instances.OfType<IIfcRelDefinesByType>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcPropertySingleValue>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcPropertySet>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcRelDefinesByProperties>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcDoorLiningProperties>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcDoorPanelProperties>(), jsonWriter);
        WriteSet(model.Instances.OfType<IIfcWindowLiningProperties>(), jsonWriter);
    }

    private static void WriteSet<T>(IEnumerable<T> entities, BimxJsonCreator jsonWriter)
        where T : IPersistEntity
    {
        foreach (var entity in entities)
        {
            jsonWriter.BTask(entity);
        }
    }
}
