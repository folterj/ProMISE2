using System.ComponentModel;

namespace ProMISE2
{
	public struct CompPosition
	{
		public bool set;
		public string label;
		public string pos;
		public string con;
	}

	public enum RegMode
	{
		Academic,
		Commercial,
		Activation
	}

	public enum LicenseType
	{
		Academic = 0,
		Commercial = 1
	}

	public enum UpdateSupport
	{
		All = 0,
		Major = 1,
		Minor = 2,
		None = 3
	}

	public enum CodeStatus
	{
		Missing,
		Ok,
		Invalid,
		DataChanged,
		Expired,
		NovelVersion
	}

	public enum ProfileType
	{
		[Description("CCD")]
		CCD,
		[Description("CCC")]
		CCC,
		[Description("Toroidal CCC")]
		ToroidalCCC,
		[Description("Dropplet CCC")]
		DroppletCCC,
		[Description("CPC")]
		CPC,
		[Description("Vortex CCD")]
		VortexCCD,
	}

	public enum ModelType
	{
		CCD,
		Probabilistic,
		Transport
	}

	public enum RunModeType
	{
		[Description("Upper phase")]
		UpperPhase,
		[Description("Lower phase")]
		LowerPhase,
		[Description("Dual mode")]
		DualMode,
		[Description("Intermittent mode")]
		Intermittent,
		[Description("Co-current")]
		CoCurrent
	}

	public enum EEModeType
	{
		None,
		[Description("Elution-Extrusion")]
		EE,
		[Description("Back Elution-Extrusion")]
		BEE
	}

	public enum InjectModeType
	{
		Instant,
		Batch,
		Continuous
	}

	public enum PhaseType
	{
		None,
		Upper,
		Lower,
		Both
	}

	public enum VolUnitsType
	{
		[Description("[ml]")]
		ml,
		[Description("[l]")]
		l
	}

	public enum MassUnitsType
	{
		[Description("[pg]")]
		pg,
		[Description("[ng]")]
		ng,
		[Description("[\x03BCg]")]
		ug,
		[Description("[mg]")]
		mg,
		[Description("[g]")]
		g,
		[Description("[kg]")]
		kg
	}

	public enum TimeUnitsType
	{
		[Description("[s]")]
		s,
		[Description("[min]")]
		min,
		[Description("[hr]")]
		hour
	}

	public enum QuantityType
	{
		Volume,
		Time,
		Steps,
		Mobile,
		Column,
		ReS
	}

	public enum IntModeType
	{
		Volume,
		Time,
		Steps,
		Component
	}

	public enum KdefType
	{
		[Description("Upper/Lower")]
		U_L,
		[Description("Lower/Upper")]
		L_U
	}

	public enum ViewType
	{
		Setup = 0,
		Out = 1,
		Time = 2
	}

	public enum PhaseDisplayType
	{
		UpperLower,
		UpperLowerTime,
		All,
		Upper,
		Lower
	}

	public enum PeaksDisplayType
	{
		Peaks,
		Sum,
		PeaksSum,
		IntTotals
	}

	public enum YScaleType
	{
		Automatic,
		Absolute,
		Normalised,
		Logarithmic
	}

	public enum VisSerieType
	{
		Graph,
		Units
	}

	public enum ExponentType
	{
		Exponents,
		Prefixes
	}

	public enum ParamType
	{
		None,
		Column,
		Flow,
		Inject,
		Advanced,
		Components,
		Stability
	}

	public enum Transparency
	{
		Outline,
		Opaque,
		Partial,
		Transparent
	}

	public enum ChromPageContent
	{
		Chrom,
		Params
	}

}
