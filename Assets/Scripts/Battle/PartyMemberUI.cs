using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Text hpText;

    [SerializeField] Color highlightedColor;

    Dragon _dragon;


    public void Init(Dragon dragon)
    {
        _dragon = dragon;
        UpdateData();

        _dragon.OnHPChanged += UpdateData;
        
    }

    void UpdateData()
    {
        nameText.text = _dragon.Base.Name;
        levelText.text = "Lvl " + _dragon.Level;
        hpBar.SetHP((float)_dragon.HP / _dragon.MaxHp);
        hpText.text = _dragon.HP + "/" + _dragon.MaxHp;
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            nameText.color = highlightedColor;
        else
            nameText.color = Color.black;
    }
}
