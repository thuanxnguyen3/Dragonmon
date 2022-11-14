using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;

    public void SetData(Dragon dragon)
    {
        nameText.text = dragon.Base.Name;
        levelText.text = "Lvl " + dragon.Level;
        hpBar.SetHP((float) dragon.HP / dragon.MaxHp);
    }
}
