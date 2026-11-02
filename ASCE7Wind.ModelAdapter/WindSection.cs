using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using BuildingModel.Model;

namespace ASCE7WindLoadCalculator.ModelAdapter
{
    /// <summary>
    /// The wind app's section of the building model file (the building's "Wind" data): what the wind calculation needs
    /// that is about the wind, not the building. The building's size, stories and roof are never here; they are the
    /// model's. The starting values are the engine's (<see cref="WindParameters_Base"/>, <see cref="BuildingData"/>).
    /// Any app that reads this section and runs the engine gets the same wind loads.
    /// </summary>
    [DataContract]
    public sealed class WindSection
    {
        public const string Key = "Wind";

        [DataMember(Order = 1)] public ASCE7_Versions Edition { get; set; }
        [DataMember(Order = 2)] public string RiskCategory { get; set; }
        [DataMember(Order = 3)] public double WindSpeed { get; set; }
        [DataMember(Order = 4)] public WindExposureCategories Exposure { get; set; }
        [DataMember(Order = 5)] public double Kzt { get; set; }
        [DataMember(Order = 6)] public double GustFactor { get; set; }
        [DataMember(Order = 7)] public double RoofComponentArea { get; set; }
        [DataMember(Order = 8)] public double WallComponentArea { get; set; }
        [DataMember(Order = 9)] public LateralProcedure Procedure { get; set; }
        [DataMember(Order = 10)] public bool OnlyCases1And3 { get; set; }
        [DataMember(Order = 11)] public ComponentChoice RoofComponent { get; set; }
        [DataMember(Order = 12)] public ComponentChoice WallComponent { get; set; }
        [DataMember(Order = 13)] public BuildingEnclosures Enclosure { get; set; }
        [DataMember(Order = 14)] public BuildingEnclosures ParapetEnclosure { get; set; }

        /// <summary>The newest edition the engine has: a new project's edition.</summary>
        public const ASCE7_Versions NewestEdition = ASCE7_Versions.ASCE_VER_7_22;

        /// <summary>A new project's wind inputs: the engine's starting values.</summary>
        public static WindSection New()
        {
            var b = new BuildingData();
            return From(NewestEdition, new WindParameters_Base(), b.EnclosureType, b.ParapetEnclosure, false, null, null);
        }

        public static WindSection From(ASCE7_Versions edition, WindParameters_Base p, BuildingEnclosures enclosure, BuildingEnclosures parapetEnclosure,
                                       bool onlyCases1And3, ComponentChoice roof, ComponentChoice wall) => new WindSection
        {
            Edition = edition,
            RiskCategory = p.RiskCategory,
            WindSpeed = p.WindSpeed,
            Exposure = p.ExposureCategory,
            Kzt = p.Kzt,
            GustFactor = p.GustFactor,
            RoofComponentArea = p.RoofComponentArea,
            WallComponentArea = p.WallComponentArea,
            Procedure = p.LateralProcedure,
            OnlyCases1And3 = onlyCases1And3,
            RoofComponent = roof,
            WallComponent = wall,
            Enclosure = enclosure,
            ParapetEnclosure = parapetEnclosure,
        };

        /// <summary>The engine's parameters (the defaults of anything not in the section).</summary>
        public WindParameters_Base ToParameters()
        {
            var p = new WindParameters_Base
            {
                RiskCategory = RiskCategory,
                WindSpeed = WindSpeed,
                ExposureCategory = Exposure,
                Kzt = Kzt,
                GustFactor = GustFactor,
                RoofComponentArea = RoofComponentArea,
                WallComponentArea = WallComponentArea,
                LateralProcedure = Procedure,
            };
            return p;
        }

        public WindOptions Options => new WindOptions { Enclosure = Enclosure, ParapetEnclosure = ParapetEnclosure };

        /// <summary>The engine's building for this model: its size, stories and roof, with this section's enclosure.</summary>
        public WindModelInput BuildingFor(Building b) => WindAdapter.From(b, Options);

        // ------------------------------------------------------------------ in the model file

        /// <summary>The section stored in the building, or null when the building has none (wind not set up yet).</summary>
        public static WindSection Read(Building b)
        {
            string json = b?.GetData(Key);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (WindSection)new DataContractJsonSerializer(typeof(WindSection)).ReadObject(ms);
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        /// <summary>Stores the section in the building (one undo step for the caller).</summary>
        public void Write(Building b) => b.SetData(Key, ToJson());

        public string ToJson()
        {
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(WindSection)).WriteObject(ms, this);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
