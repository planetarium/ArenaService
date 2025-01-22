using Bencodex.Types;

namespace ArenaService.Utils;

public class ActionParser
{
    public static (IValue? typeId, IValue? values) DeconstructActionPlainValue(
        IValue actionPlainValue
    )
    {
        if (actionPlainValue is not Dictionary actionPlainValueDict)
        {
            return (null, null);
        }

        var actionType = actionPlainValueDict.ContainsKey("type_id")
            ? actionPlainValueDict["type_id"]
            : null;
        var actionPlainValueInternal = actionPlainValueDict.ContainsKey("values")
            ? actionPlainValueDict["values"]
            : null;
        return (actionType, actionPlainValueInternal);
    }
}
