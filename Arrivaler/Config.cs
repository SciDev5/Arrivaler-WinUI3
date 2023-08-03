using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Arrivaler
{
    internal static class Config
    {
        private static ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        private const string ARRIVAL_THRESHOLD = "arrivalThreshold";
        public static double ArrivalThreshold {
            get {
                var k = settings.Values[ARRIVAL_THRESHOLD];
                return k is double v ? v : 2.0;
            }
            set
            {
                settings.Values[ARRIVAL_THRESHOLD] = value;
            }
        }
        private const string DESTINATION_INFOS_LEN = "destinationInfosLen";
        private const string DESTINATION_INFO_BASE = "destinationInfos_";

        public static DestinationInfo[] DestinationInfos
        {
            get
            {
                if (settings.Values[DESTINATION_INFOS_LEN] is int len)
                {
                    var dests = new List<DestinationInfo>();
                    for (int i = 0; i < len; i++)
                    {
                        var entryRaw = settings.Values[DESTINATION_INFO_BASE + i];
                        if (
                            entryRaw is ApplicationDataCompositeValue entry
                            && entry["name"] is string name
                            && entry["th"] is double th
                            && entry["ph"] is double ph)
                        {
                            dests.Add(new DestinationInfo()
                            {
                                Name = name,
                                Pos = new PolarPos(th, ph)
                            });
                        }
                    }
                    return dests.ToArray();
                } else
                {
                    return Array.Empty<DestinationInfo>();
                }
            }
            set
            {
                if (settings.Values[DESTINATION_INFOS_LEN] is int len)
                {
                    // remove extras if there are less destinations now.
                    for (int i = value.Length; i < len; ++i)
                    {
                        settings.Values.Remove(DESTINATION_INFO_BASE + i);
                    }
                }
                settings.Values[DESTINATION_INFOS_LEN] = value.Length;
                for (int i = 0; i < value.Length; ++i)
                {
                    settings.Values[DESTINATION_INFO_BASE + i] = new ApplicationDataCompositeValue
                    {
                        ["name"] = value[i].Name,
                        ["th"] = value[i].Pos.th,
                        ["ph"] = value[i].Pos.ph
                    };
                }
            }
        }
    }
}
