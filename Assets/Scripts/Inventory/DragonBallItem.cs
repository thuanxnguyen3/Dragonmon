using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new dragonball")]
public class DragonBallItem : ItemBase
{
    [SerializeField] float catchRateModifier = 1;
    public override bool Use(Dragon dragon)
    {
        return true;
    }

    public float CatchRateModifier => catchRateModifier;
}
