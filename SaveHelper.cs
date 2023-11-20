using UnityEngine;

namespace CommonModNS
{
    public enum SaveSettingsMode { Tournament, Casual, Disabled, Tampered }

    internal struct SecretData
    {
        private readonly int savedCards;
        private readonly int month;
        private readonly float monthTimer;
        public SecretData(SaveRound saveRound)
        {
            savedCards = saveRound.SavedCards.Count;
            month = saveRound.BoardMonths.MainMonth;
            monthTimer = saveRound.MonthTimer;
        }

        public readonly string RawString()
        {
            List<string> strings = new List<string>();
            strings.Add(savedCards.ToString());
            strings.Add(month.ToString());
            strings.Add(monthTimer.ToString());
            return String.Join(" ", strings);
        }
    }

    public class SaveHelper
    {
        private readonly string saveRoundKey;
        private readonly string oldSaveRoundKey = null;
        private readonly string salt;

        private static readonly string SettingsStatus_casual = "CASUAL";
        private static readonly string SettingsStatus_broken = "BROKEN";
        private static readonly string SettingsStatus_disabled = "DISABLED";

        public delegate string OnGetSettings();
        public OnGetSettings onGetSettings;

        public SaveHelper(string modName)
        {
            saveRoundKey = modName + "_save";
            salt = (Environment.MachineName ?? "") + "?" + modName;
        }

        public void Ready(string path)
        {
            string locPath = Path.Combine(path, "savehelper.tsv");
            if (File.Exists(locPath))
            {
                SokLoc.instance.LoadTermsFromFile(locPath);
            }
        }
        private string Construct(SecretData secrets, string payload)
        {
            List<string> strings = new List<string>();
            strings.Add(secrets.RawString());
            if (payload != null) strings.Add(payload);
            string hashSource = String.Join(" ", strings);
            string result = (salt + ":" + hashSource).GetHashCode().ToString();
            if (payload != null) result += ":" + payload;
            return result;
        }

        private (SaveSettingsMode mode, string) Interpret(string hash, SecretData secrets)
        {
            SaveSettingsMode SettingsMode = SaveSettingsMode.Casual;

            int pos = hash.IndexOf(":");
            string payload = hash.Substring(pos + 1);
            if (payload == SettingsStatus_disabled)
            {
                SettingsMode = SaveSettingsMode.Disabled;
                I.Log("LoadData - succeeded - mod is disabled.");
            }
            else if (payload == SettingsStatus_casual)
            {
                SettingsMode = SaveSettingsMode.Casual;
                I.Log("LoadData - succeeded - No value stored in save data, using mod options value.");
            }
            else if (payload == SettingsStatus_broken || pos < 0)
            {
                SettingsMode = SaveSettingsMode.Tampered;
                I.Log("LoadData - succeeded - save files has already been reported broken.");
            }
            else if (hash != Construct(secrets, onGetSettings?.Invoke()))
            {
                SettingsMode = SaveSettingsMode.Tampered;
                I.Log("LoadData - failed - hashes do not match.");
            }
            else
            {
                SettingsMode = SaveSettingsMode.Tournament;
                I.Log($"LoadData - succeeded - {payload}");
            }
            return (SettingsMode, payload);
        }

        public void SaveData(SaveRound saveRound, SaveSettingsMode SettingsMode)
        {
            SecretData secret = new SecretData(saveRound);
            string payload = SettingsMode switch
            {
                SaveSettingsMode.Casual => SettingsStatus_casual,
                SaveSettingsMode.Disabled => Construct(secret, SettingsStatus_disabled),
                SaveSettingsMode.Tournament => Construct(secret, onGetSettings?.Invoke()),
                _ => SettingsStatus_broken,
            };

            I.Log($"SaveData - {payload}");
            saveRound.ExtraKeyValues.SetOrAdd(saveRoundKey, payload);
        }

        public (SaveSettingsMode, string) LoadData(SaveRound saveRound)
        {
            string hash = saveRound.ExtraKeyValues.Find(x => x.Key == saveRoundKey)?.Value;
            if (hash == null)
            {
                if (oldSaveRoundKey != null)
                {
                    hash = saveRound.ExtraKeyValues.Find(x => x.Key == oldSaveRoundKey)?.Value;
                }
                if (hash == null)
                {
                    I.Log("LoadData - value was null");
                    return (SaveSettingsMode.Casual, null);
                }
            }
            return Interpret(hash, new SecretData(saveRound));
        }

        public void ClearCurrentSave()
        {
            SaveRound round = I.WM.CurrentSave.LastPlayedRound;
            int at = round.ExtraKeyValues.FindIndex(x => x.Key == saveRoundKey);
            if (at >= 0) round.ExtraKeyValues.RemoveAt(at);
        }

        public string DescribeCurrentSave()
        {
            SaveRound round = I.WM.CurrentSave.LastPlayedRound;
            string hash = round?.ExtraKeyValues.Find(x => x.Key == saveRoundKey)?.Value;
            if (round != null) foreach (var x in round.ExtraKeyValues)
            {
                I.Log($"key {x.Key} value {x.Value}");
            }
            I.Log($"game {round} saveRoundKey {saveRoundKey} hash {hash}");
            if (hash == null || round == null)
            {
                return I.Xlat("savehelper_descript_nosave");
            }
            (SaveSettingsMode mode, string payload) = Interpret(hash, new SecretData(round));
            return mode switch
            {
                SaveSettingsMode.Casual => I.Xlat("savehelper_descript_casual"),
                SaveSettingsMode.Disabled => I.Xlat("savehelper_descript_disabled"),
                SaveSettingsMode.Tampered => I.Xlat("savehelper_descript_broken"),
                _ => I.Xlat("savehelper_descript_tournament", LocParam.Create("percentage", payload))
            };
        }
    }
}
