using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Text hpText;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color frzColor;

    Dragon _dragon;

    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Dragon dragon)
    {
        _dragon = dragon;

        nameText.text = dragon.Base.Name;
        levelText.text = "Lvl " + dragon.Level;
        hpBar.SetHP((float) dragon.HP / dragon.MaxHp);
        hpText.text = dragon.HP + "/" + dragon.MaxHp;

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColor },
            {ConditionID.brn, brnColor },
            {ConditionID.frz, frzColor },
        };

        SetStatusText();
        _dragon.OnStatusChanged += SetStatusText;
    }

    void SetStatusText()
    {
        if (_dragon.Status == null)
        {
            //dragon status is null
            statusText.text = "";
        }
        else
        {
            //dragon status is not null
            statusText.text = _dragon.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_dragon.Status.Id];
        }
    }

    public IEnumerator UpdateHP ()
    {
        if(_dragon.HpChanged)
        {
            yield return hpBar.SetHPSmooth((float)_dragon.HP / _dragon.MaxHp);
            hpText.text = _dragon.HP + "/" + _dragon.MaxHp;
            _dragon.HpChanged = false;
        }

    }
}
