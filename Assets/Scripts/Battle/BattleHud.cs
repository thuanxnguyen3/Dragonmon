using DG.Tweening;
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
    [SerializeField] GameObject expBar;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color frzColor;

    Dragon _dragon;

    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Dragon dragon)
    {
        _dragon = dragon;

        nameText.text = dragon.Base.Name;
        SetLevel();
        hpBar.SetHP((float) dragon.HP / dragon.MaxHp);
        hpText.text = dragon.HP + "/" + dragon.MaxHp;
        SetExp();

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

    public void SetLevel()
    {
        levelText.text = "Lvl " + _dragon.Level;

    }

    public void SetExp()
    {
        if (expBar == null) return;

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
    }

    public IEnumerator SetExpSmooth(bool reset=false)
    {
        if (expBar == null) yield break;

        if(reset)
            expBar.transform.localScale = new Vector3(0, 1, 1);


        float normalizedExp = GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    float GetNormalizedExp()
    {
        int currLevelExp = _dragon.Base.GetExpForLevel(_dragon.Level);
        int nextLevelExp = _dragon.Base.GetExpForLevel(_dragon.Level + 1);

        float normalizedExp = (float)(_dragon.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
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
