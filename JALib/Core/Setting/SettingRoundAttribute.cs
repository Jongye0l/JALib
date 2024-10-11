namespace JALib.Core.Setting;

[AttributeUsage(AttributeTargets.Field)]
public class SettingRoundAttribute : Attribute {
    public int Round;

    public SettingRoundAttribute(int round) {
        Round = round;
    }
}