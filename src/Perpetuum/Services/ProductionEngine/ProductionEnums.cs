using System;

namespace Perpetuum.Services.ProductionEngine
{
    /// <summary>
    /// Type of a running production
    /// </summary>
    [Serializable]
    public enum ProductionInProgressType
    {
        //obsolete
        manufacture,
        licenseCreate, //1
        patentNofRunsDevelop, //2
        patentMaterialEfficiencyDevelop, //3
        patentTimeEfficiencyDevelop, //4

        //current ones
        research, //5
        refine, //6
        reprocess, //7
        massProduction, //8
        prototype, //9
        insurance, //10
        calibrationProgramForge, //11
        inserCT, //12
        removeCT, //13
    }


    /// <summary>
    /// Type of a production facility
    /// </summary>
    public enum ProductionFacilityType
    {
        NotDefined,
        Repair,
        Refine,
        Reprocess,
        Research,
        Prototype,
        MassProduce,
        ResearchKitForge,
        CalibrationProgramForge
    }

    /// <summary>
    /// Event type for pbs production facilities
    /// </summary>
    public enum ProductionEvent
    {
        GotPaused,
        GotResumed,
    }
}
