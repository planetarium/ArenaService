using System.Text.RegularExpressions;
using ArenaService.ActionValues;
using ArenaService.Extensions;
using Bencodex.Types;

namespace ArenaService.Utils;

public class BattleActionParser
{
    public static bool TryParseActionPayload(
        IValue plainValue,
        out BattleActionValue battleActionValue
    )
    {
        battleActionValue = null;

        var (actionType, actionValues) = ActionParser.DeconstructActionPlainValue(plainValue);

        var actionTypeStr = actionType switch
        {
            Integer integer => integer.ToString(),
            Text text => (string)text,
            _ => null
        };

        if (actionTypeStr is null || actionValues is null)
        {
            return false;
        }

        if (Regex.IsMatch(actionTypeStr, "^battle[0-9]*$"))
        {
            battleActionValue = ParseActionPayload((Dictionary)actionValues);
            return true;
        }

        return false;
    }

    public static BattleActionValue ParseActionPayload(Dictionary plainValue)
    {
        var myAvatarAddress = plainValue["maa"].ToAddress();
        var enemyAvatarAddress = plainValue["eaa"].ToAddress();
        var memo = plainValue["m"].ToString();
        var arenaProvider = plainValue["arp"].ToString();
        var chargeAp = plainValue["cha"].ToBoolean();
        var costumes = ((List)plainValue["cs"]).Select(e => e.ToGuid()).ToList();
        var equipments = ((List)plainValue["es"]).Select(e => e.ToGuid()).ToList();

        return new BattleActionValue
        {
            MyAvatarAddress = myAvatarAddress,
            EnemyAvatarAddress = enemyAvatarAddress,
            Memo = memo,
            ArenaProvider = arenaProvider,
            ChargeAp = chargeAp,
            Costumes = costumes,
            Equipments = equipments
        };
    }
}
