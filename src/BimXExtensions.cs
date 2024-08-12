using Xbim.Ifc4.Interfaces;

namespace Bingosoft.Net.IfcDetail;

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
