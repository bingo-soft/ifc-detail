using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text.Encodings.Web;
using System.Text.Json;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

internal class MaterialExtractor(FileInfo jsonTargetFile, OutputWriteOptions? outputWriteOptions = null)
{
    public static readonly JsonWriterOptions Jwo = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public void Start(FileInfo ifcFileInfo)
    {
        using var model = IfcStore.Open(ifcFileInfo.FullName, accessMode: Xbim.IO.XbimDBAccess.Read);
        using var jsonWriter = new BimxJsonCreator(jsonTargetFile, outputWriteOptions);

        EmitEntities(model.Instances.OfType<IIfcMaterial>(), jsonWriter);

        EmitEntities(model.Instances.OfType<IIfcMaterialList>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcMaterialLayerSet>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcMaterialLayer>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcMaterialConstituent>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcMaterialConstituentSet>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcMaterialLayerSetUsage>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcSpaceType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcColumnType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcWallType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcSlabType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcCoveringType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcStairFlightType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcPlateType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcMemberType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcCurtainWallType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcDistributionElementType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcBuildingElementProxyType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcPipeSegmentType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcFurnitureType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcRelDefinesByType>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcPropertySingleValue>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcPropertySet>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcRelDefinesByProperties>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcDoorLiningProperties>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcDoorPanelProperties>(), jsonWriter);
        EmitEntities(model.Instances.OfType<IIfcWindowLiningProperties>(), jsonWriter);

        jsonWriter.CreateJson();

    }

    private static void EmitEntities<T>(IEnumerable<T> entities, BimxJsonCreator jsonWriter)
        where T : class, IPersistEntity
    {
        foreach (var entity in entities)
        {
            jsonWriter.BTask(entity);
        }
    }
}

