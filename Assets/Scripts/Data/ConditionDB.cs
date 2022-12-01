using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;


            condition.Id = conditionId;
        }
    }
    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,  
            new Condition()
            {
                Name = "Poison",
                StartMessage = "has been poisoned",
                OnAfterTurn = (Dragon dragon) =>
                {
                    dragon.DecreaseHP(dragon.MaxHp / 8);
                    dragon.StatusChanges.Enqueue($"{dragon.Base.Name} hurt itself due to poison");
                }
            }
        },
        {
            ConditionID.brn,
            new Condition()
            {
                Name = "Burn",
                StartMessage = "has been burned",
                OnAfterTurn = (Dragon dragon) =>
                {
                    dragon.DecreaseHP(dragon.MaxHp / 16);
                    dragon.StatusChanges.Enqueue($"{dragon.Base.Name} hurt itself due to burn");
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Name = "Freeze",
                StartMessage = "has been frozen",
                OnBeforeMove = (Dragon dragon) =>
                {
                    if (Random.Range(1, 5) == 1)
                    { 
                        dragon.CureStatus();
                        dragon.StatusChanges.Enqueue($"{dragon.Base.Name} is not frozen anymore");
                        return true;
                    }
                    /*
                    dragon.UpdateHP(dragon.MaxHp / 12);
                    dragon.StatusChanges.Enqueue($"{dragon.Base.Name} is hurt due to being frozen");*/
                    return false;
                }
            }
        }
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
            return 1f;
        else if (condition.Id == ConditionID.frz)
            return 2f;
        else if (condition.Id == ConditionID.psn || condition.Id == ConditionID.brn)
            return 1.5f;

        return 1f;
    }
}

public enum ConditionID
{
    none, psn, brn, slp, par, frz
}