using CommonModNS;
using UnityEngine;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod : Mod
    {
        private void WM_OnNewRound(WorldManager wm)
        {
            SettingsMode = TournamentMode;// ? SettingsMode.Tournament : SettingsMode.Casual;
            StrengthModifier = (float)StrengthPercentage / 100f;
        }

        private void WM_OnSave(WorldManager wm, SaveRound saveRound)
        {
            SaveHelper.SaveData(saveRound, (int)(StrengthModifier * 100f), SettingsMode);
        }

        private void WM_OnLoad(WorldManager wm, SaveRound saveRound)
        {
            ApplyConfig();
            (SettingsMode, int percentage) = SaveHelper.LoadData(saveRound);
            if (SettingsMode == SettingsMode.Tournament)
            {
                ApplyStrengthMultiplier(percentage);
            }
        }

        private void WM_OnPlay(WorldManager wm)
        {
            Notification();
        }
    }


    public enum ChallengeStatus { ENABLED, DISABLED, BROKEN }
    public interface IChallengeMod
    {
        string Challenge_ModId { get; }
        string Challenge_SaveData_Immutable { get; }

        void Challenge_OnPlay(ChallengeStatus status);

        void Challenge_OnUI(Transform parent);
        void Challenge_OnUISave();
    }

    public class ChallengeManager
    {
        List<IChallengeMod> mods;


    }

    public static class SaveHelper
    {
        private static readonly string saveRoundKey = "enemydifficultymod_mode";
        private static readonly string oldSaveRoundKey = "enemydifficultymod_lock";

        private static readonly string SettingsStatus_casual = "CASUAL";
        private static readonly string SettingsStatus_broken = "BROKEN";
        private static readonly string SettingsStatus_disabled = "DISABLED";

        private static readonly string salt = Environment.MachineName + "?enemystrength";
//        private static SettingsMode SettingsMode { get => EnemyDifficultyMod.SettingsMode; set => EnemyDifficultyMod.SettingsMode = value; }

        internal struct SecretData
        {
            public int savedCards;
            public int month;
            public float monthTimer;
            public SecretData(SaveRound saveRound)
            {
                savedCards = saveRound.SavedCards.Count;
                month = saveRound.BoardMonths.MainMonth;
                monthTimer = saveRound.MonthTimer;
            }
        }

        private static string Construct(SecretData secrets, int value)
        {
            string percent = value.ToString();
            List<string> strings = new List<string>();
            strings.Add(secrets.savedCards.ToString());
            strings.Add(secrets.month.ToString());
            strings.Add(secrets.monthTimer.ToString());
            strings.Add(percent);
            string x = String.Join(" ", strings) + " ";
            return (salt + ":" + x).GetHashCode().ToString() + ":" + percent;
        }

        private static (SettingsMode mode, int) Interpret(string hash, SecretData secrets)
        {
            SettingsMode SettingsMode = SettingsMode.Casual;
            int percentage = 100;

            int pos = hash.IndexOf(":");
            string payload = hash.Substring(pos + 1);
            if (payload == SettingsStatus_disabled)
            {
                SettingsMode = SettingsMode.Disabled;
                I.Log("LoadData - succeeded - mod is disabled.");
            }
            else if (payload == SettingsStatus_casual)
            {
                SettingsMode = SettingsMode.Casual;
                I.Log("LoadData - succeeded - No value stored in save data, using mod options value.");
            }
            else if (payload == SettingsStatus_broken || pos < 0)
            {
                SettingsMode = SettingsMode.Tampered;
                I.Log("LoadData - succeeded - save files has already been reported broken.");
            }
            else if (!Int32.TryParse(payload, out percentage))
            {
                SettingsMode = SettingsMode.Tampered;
                I.Log("LoadData - failed - payload is not a percent value.");
            }
            else if (hash != Construct(secrets, percentage))
            {
                SettingsMode = SettingsMode.Tampered;
                I.Log("LoadData - failed - hashes do not match.");
            }
            else
            {
                SettingsMode = SettingsMode.Tournament;
                I.Log($"LoadData - succeeded - enemy strength modifier={percentage}");
            }
            return (SettingsMode, percentage);
        }

        public static void SaveData(SaveRound saveRound, int value, SettingsMode SettingsMode)
        {
            string payload = SettingsMode switch
            {
                SettingsMode.Casual => SettingsStatus_casual,
                SettingsMode.Disabled => SettingsStatus_disabled,
                SettingsMode.Tampered => SettingsStatus_broken,
                _ => Construct(new SecretData(saveRound), value)
            };

            I.Log($"SaveData - {payload}");
            saveRound.ExtraKeyValues.SetOrAdd(saveRoundKey, payload);
        }

        public static (SettingsMode, int) LoadData(SaveRound saveRound)
        {
            string hash = saveRound.ExtraKeyValues.Find(x => x.Key == saveRoundKey)?.Value;
            if (hash == null)
            {
                hash = saveRound.ExtraKeyValues.Find(x => x.Key == oldSaveRoundKey)?.Value;
                if (hash == null)
                {
                    I.Log("LoadData - value was null");
                    return (SettingsMode.Casual, 100);
                }
                return (SettingsMode.Casual, 100);
            }
            return Interpret(hash, new SecretData(saveRound));
        }

        public static void ClearCurrentSave()
        {
            SaveGame game = I.WM.CurrentSave;
            game.ExtraKeyValues.SetOrAdd(saveRoundKey, SettingsStatus_casual);
            EnemyDifficultyMod.instance.clearCurrentSave.Update();
//            I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"), I.Xlat("enemydifficultymod_clearsave_text"));
        }

        public static string DescribeCurrentSave()
        {
            SaveGame game = I.WM.CurrentSave;
            string hash = game.ExtraKeyValues.Find(x => x.Key == saveRoundKey)?.Value;
            if (hash == null || game.LastPlayedRound == null)
            {
                return I.Xlat("enemydifficultymod_descript_nosave");
            }
            (SettingsMode mode, int percentage) = Interpret(hash, new SecretData(game.LastPlayedRound));
            return mode switch
            {
                SettingsMode.Casual => I.Xlat("enemydifficultymod_descript_casual"),
                SettingsMode.Disabled => I.Xlat("enemydifficultymod_descript_disabled"),
                SettingsMode.Tampered => I.Xlat("enemydifficultymod_descript_broken"),
                _ => I.Xlat("enemydifficultymod_descript_broken", LocParam.Create("percentage", percentage.ToString()))
            };
        }
    }
}
