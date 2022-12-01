using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new recovery item")]
public class RecoveryItem : ItemBase
{
    [Header("HP")]
    [SerializeField] int hpAmount;
    [SerializeField] bool restoreMaxHP;

    [Header("PP")]
    [SerializeField] int ppAmount;
    [SerializeField] bool restoreMaxPP;

    [Header("Status Conditions")]
    [SerializeField] ConditionID status;
    [SerializeField] bool recoverAllStatus;

    [Header("Revive")]
    [SerializeField] bool revive;
    [SerializeField] bool maxRevive;

    public override bool Use(Dragon dragon)
    {
        // Revive
        if(revive || maxRevive)
        {
            if (dragon.HP > 0)
                return false;

            if (revive)
                dragon.IncreaseHP(dragon.MaxHp / 2);
            else if (maxRevive)
                dragon.IncreaseHP(dragon.MaxHp);

            dragon.CureStatus();

            return true;
        }

        // No other items can be used on fainted dragon
        if (dragon.HP == 0)
            return false;

        // Restore HP
        if (hpAmount > 0)
        {
            if (dragon.HP == dragon.MaxHp)
                return false;

            dragon.IncreaseHP(hpAmount);
        }
        return true;
    }
}
