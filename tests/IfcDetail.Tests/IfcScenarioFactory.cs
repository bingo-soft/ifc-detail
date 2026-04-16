using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.IO;

namespace IfcDetail.Tests;

internal static class IfcScenarioFactory
{
    public static IReadOnlyList<(string Name, FileInfo IfcFile)> CreateScenarios(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);

        return
        [
            ("small", BuildSmall(rootDirectory)),
            ("medium", BuildMedium(rootDirectory)),
            ("large", BuildLarge(rootDirectory)),
            ("edge-cases", BuildEdgeCases(rootDirectory))
        ];
    }

    private static FileInfo BuildSmall(string rootDirectory)
    {
        var ifcPath = new FileInfo(Path.Combine(rootDirectory, "small.ifc"));
        using var model = CreateModel();

        using (var tx = model.BeginTransaction("small"))
        {
            var material = model.Instances.New<IfcMaterial>();
            material.Name = "Steel";

            var propertySet = model.Instances.New<IfcPropertySet>();
            propertySet.GlobalId = NewGlobalId();
            propertySet.Name = "Pset_Small";

            var psv = model.Instances.New<IfcPropertySingleValue>();
            psv.Name = "Code";
            psv.NominalValue = new IfcLabel("S-001");
            propertySet.HasProperties.Add(psv);

            var wallType = model.Instances.New<IfcWallType>();
            wallType.GlobalId = NewGlobalId();
            wallType.Name = "Wall-Small";
            wallType.Tag = "W-S";
            wallType.PredefinedType = IfcWallTypeEnum.STANDARD;
            wallType.HasPropertySets.Add(propertySet);

            tx.Commit();
        }

        model.SaveAs(ifcPath.FullName);
        return ifcPath;
    }

    private static FileInfo BuildMedium(string rootDirectory)
    {
        var ifcPath = new FileInfo(Path.Combine(rootDirectory, "medium.ifc"));
        using var model = CreateModel();

        using (var tx = model.BeginTransaction("medium"))
        {
            var concrete = model.Instances.New<IfcMaterial>();
            concrete.Name = "Concrete";

            var insulation = model.Instances.New<IfcMaterial>();
            insulation.Name = "Insulation";

            var layerSet = model.Instances.New<IfcMaterialLayerSet>();
            layerSet.LayerSetName = "ExternalWall";

            var layer1 = model.Instances.New<IfcMaterialLayer>();
            layer1.Material = concrete;
            layer1.LayerThickness = 200;

            var layer2 = model.Instances.New<IfcMaterialLayer>();
            layer2.Material = insulation;
            layer2.LayerThickness = 20;

            layerSet.MaterialLayers.Add(layer1);
            layerSet.MaterialLayers.Add(layer2);

            var layerUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
            layerUsage.ForLayerSet = layerSet;
            layerUsage.DirectionSense = IfcDirectionSenseEnum.POSITIVE;
            layerUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
            layerUsage.OffsetFromReferenceLine = 0;

            var psetA = CreatePropertySet(model, "Pset_WallCommon", "IsExternal", "TRUE", "FireRating", null);
            var psetB = CreatePropertySet(model, "Pset_Identity", null, null, null, null);

            var wallType = model.Instances.New<IfcWallType>();
            wallType.GlobalId = NewGlobalId();
            wallType.Name = "WallTypeA";
            wallType.Tag = "W1";
            wallType.PredefinedType = IfcWallTypeEnum.STANDARD;
            wallType.HasPropertySets.Add(psetA);
            wallType.HasPropertySets.Add(psetB);

            var wallType2 = model.Instances.New<IfcWallType>();
            wallType2.GlobalId = NewGlobalId();
            wallType2.Name = "WallTypeB";
            wallType2.Tag = "W2";
            wallType2.PredefinedType = IfcWallTypeEnum.USERDEFINED;

            tx.Commit();
        }

        model.SaveAs(ifcPath.FullName);
        return ifcPath;
    }

    private static FileInfo BuildLarge(string rootDirectory)
    {
        var ifcPath = new FileInfo(Path.Combine(rootDirectory, "large.ifc"));
        using var model = CreateModel();

        using (var tx = model.BeginTransaction("large"))
        {
            for (var i = 1; i <= 25; i++)
            {
                var material = model.Instances.New<IfcMaterial>();
                material.Name = $"M{i:D2}";

                var pset = CreatePropertySet(model, $"Pset_{i:D2}", "N", i.ToString(), null, null);

                var wallType = model.Instances.New<IfcWallType>();
                wallType.GlobalId = NewGlobalId();
                wallType.Name = $"Type{i:D2}";
                wallType.Tag = $"T{i:D2}";
                wallType.PredefinedType = IfcWallTypeEnum.USERDEFINED;
                wallType.HasPropertySets.Add(pset);
            }

            tx.Commit();
        }

        model.SaveAs(ifcPath.FullName);
        return ifcPath;
    }

    private static FileInfo BuildEdgeCases(string rootDirectory)
    {
        var ifcPath = new FileInfo(Path.Combine(rootDirectory, "edge-cases.ifc"));
        using var model = CreateModel();

        using (var tx = model.BeginTransaction("edge-cases"))
        {
            var duplicateGlobalId = NewGlobalId();

            var oldType = model.Instances.New<IfcWallType>();
            oldType.GlobalId = duplicateGlobalId;
            oldType.Name = "TypeX";
            oldType.Tag = "OldTag";
            oldType.PredefinedType = IfcWallTypeEnum.USERDEFINED;

            var latestType = model.Instances.New<IfcWallType>();
            latestType.GlobalId = duplicateGlobalId;
            latestType.Name = "TypeX";
            latestType.Tag = "LatestTag";
            latestType.PredefinedType = IfcWallTypeEnum.USERDEFINED;

            var duplicatePropertyGlobalId = NewGlobalId();

            var psetOld = model.Instances.New<IfcPropertySet>();
            psetOld.GlobalId = duplicatePropertyGlobalId;
            psetOld.Name = "EdgePset";
            var psvOld = model.Instances.New<IfcPropertySingleValue>();
            psvOld.Name = "Nullable";
            psvOld.NominalValue = new IfcLabel("old");
            psetOld.HasProperties.Add(psvOld);

            var psetNew = model.Instances.New<IfcPropertySet>();
            psetNew.GlobalId = duplicatePropertyGlobalId;
            psetNew.Name = "EdgePset";
            var psvNew = model.Instances.New<IfcPropertySingleValue>();
            psvNew.Name = "Nullable";
            psvNew.NominalValue = null;
            psetNew.HasProperties.Add(psvNew);

            tx.Commit();
        }

        model.SaveAs(ifcPath.FullName);
        return ifcPath;
    }

    private static IfcPropertySet CreatePropertySet(
        IfcStore model,
        string setName,
        string? name1,
        string? value1,
        string? name2,
        string? value2)
    {
        var pset = model.Instances.New<IfcPropertySet>();
        pset.GlobalId = NewGlobalId();
        pset.Name = setName;

        if (!string.IsNullOrWhiteSpace(name1))
        {
            var item = model.Instances.New<IfcPropertySingleValue>();
            item.Name = name1;
            item.NominalValue = value1 is null ? null : new IfcLabel(value1);
            pset.HasProperties.Add(item);
        }

        if (!string.IsNullOrWhiteSpace(name2))
        {
            var item = model.Instances.New<IfcPropertySingleValue>();
            item.Name = name2;
            item.NominalValue = value2 is null ? null : new IfcLabel(value2);
            pset.HasProperties.Add(item);
        }

        return pset;
    }

    private static IfcStore CreateModel()
    {
        var credentials = new XbimEditorCredentials
        {
            ApplicationDevelopersName = "Tests",
            ApplicationFullName = "IfcDetail.Tests",
            ApplicationIdentifier = "IfcDetail.Tests",
            ApplicationVersion = "1.0",
            EditorsFamilyName = "Tests",
            EditorsGivenName = "Tests",
            EditorsOrganisationName = "Tests"
        };

        return IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
    }

    private static string NewGlobalId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 22);
    }
}
