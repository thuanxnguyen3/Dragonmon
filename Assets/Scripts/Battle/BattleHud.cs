using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Text hpText;

    Dragon _dragon;

    public void SetData(Dragon dragon)
    {
        _dragon = dragon;

        nameText.text = dragon.Base.Name;
        levelText.text = "Lvl " + dragon.Level;
        hpBar.SetHP((float) dragon.HP / dragon.MaxHp);
        hpText.text = dragon.HP + "/" + dragon.MaxHp;
    }

    public IEnumerator UpdateHP ()
    {
        yield return hpBar.SetHPSmooth((float)_dragon.HP / _dragon.MaxHp);
        hpText.text = _dragon.HP + "/" + _dragon.MaxHp;
    }
}
