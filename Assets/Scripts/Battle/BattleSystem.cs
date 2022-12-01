using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, Bag, PartyScreen, BattleOver}

public enum BattleAction { Move, SwitchDragon, UseItem, Run}
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] GameObject dragonballSprite;
    [SerializeField] InventoryUI inventoryUI;

    //public event Action<bool> OnBattleOver;

    BattleState state;
    int currentAction;
    int currentMove;

    DragonParty playerParty;
    Dragon enemyDragon;

    public void StartBattle(DragonParty playerParty, Dragon enemyDragon)
    {
        this.playerParty = playerParty;
        this.enemyDragon = enemyDragon;
        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Setup(playerParty.GetHealthyDragon());
        enemyUnit.Setup(enemyDragon);

        partyScreen.Init();

        dialogBox.SetMoveNames(playerUnit.Dragon.Moves);

        yield return dialogBox.TypeDialog($"A wild {enemyUnit.Dragon.Base.Name} appeared.");

        ActionSelection();
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose an action");
        dialogBox.EnableActionSelector(true);
    }

    void OpenBag()
    {
        state = BattleState.Bag;
        inventoryUI.gameObject.SetActive(true);

    }

    void OpenPartyScreen()
    {
        partyScreen.CalledFrom = state;
        state = BattleState.PartyScreen;
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if(playerAction == BattleAction.Move)
        {
            playerUnit.Dragon.CurrentMove  = playerUnit.Dragon.Moves[currentMove];
            enemyUnit.Dragon.CurrentMove = enemyUnit.Dragon.GetRandomMove();

            //Check who goes first but this does not matter right now

            var firstUnit = playerUnit;
            var secondUnit = enemyUnit;

            var secondDragon = secondUnit.Dragon;

            //First turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Dragon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            if (secondDragon.HP > 0)
            {
                //Second turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Dragon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }

        } 
        else
        {
            if(playerAction == BattleAction.SwitchDragon)
            {
                var selectedDragon = partyScreen.SelectedMember;
                state = BattleState.Busy;
                yield return SwitchDragon(selectedDragon);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                // This is handled from item screen, so do nothing and skip to enemy move
                dialogBox.EnableActionSelector(false);
            }

            //Enemy Turn
            var enemyMove = enemyUnit.Dragon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }

        if (state != BattleState.BattleOver)
            ActionSelection();
    }


    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Dragon.OnBeforeMove();

        if(!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Dragon);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Dragon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Dragon.Base.Name} used {move.Base.Name}");

        sourceUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(1f);
        targetUnit.PlayHitAnimation();

        if (move.Base.Category == MoveCategory.Status)
        {
            yield return RunMoveEffects(move, sourceUnit.Dragon, targetUnit.Dragon);
        }
        else
        {
            var damageDetails = targetUnit.Dragon.TakeDamage(move, sourceUnit.Dragon);
            yield return targetUnit.Hud.WaitForHPUpdate();
            yield return ShowDamageDetails(damageDetails);
        }


        if (targetUnit.Dragon.HP <= 0)
        {
            yield return HandleDragonFainted(targetUnit);
        }

        
    }

    IEnumerator RunMoveEffects(Move move, Dragon source, Dragon target)
    {
        var effects = move.Base.Effects;

        //Stat Boosting
        if (effects.Boosts != null)
        {
            if (move.Base.Target == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        //Status Condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // Statues like burn or psn will hurt the dragon after the turn
        sourceUnit.Dragon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Dragon);
        yield return sourceUnit.Hud.WaitForHPUpdate();

        if (sourceUnit.Dragon.HP <= 0)
        {
            yield return HandleDragonFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    IEnumerator ShowStatusChanges(Dragon dragon)
    {
        while (dragon.StatusChanges.Count > 0)
        {
            var message = dragon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandleDragonFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Dragon.Base.Name} Fainted");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);
        
        if (!faintedUnit.IsPlayerUnit)
        {
            //Exp gain
            int expYield = faintedUnit.Dragon.Base.ExpYield;
            int enemyLevel = faintedUnit.Dragon.Level;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel) / 5);
            playerUnit.Dragon.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Dragon.Base.Name} gained {expGain} EXP. Points!");
            yield return playerUnit.Hud.SetExpSmooth();

            //Check Level Up
            while (playerUnit.Dragon.CheckForlevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Dragon.Base.Name} grew to LV. {playerUnit.Dragon.Level}!");
                
                yield return playerUnit.Hud.SetExpSmooth(true);

            }

            yield return new WaitForSeconds(1f);
        }

        CheckForBattleOver(faintedUnit);
        
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if(faintedUnit.IsPlayerUnit)
        {
            var nextDragon = playerParty.GetHealthyDragon();
            if (nextDragon != null)
            {
                OpenPartyScreen();
            }
            else
            {
                //game over
                state = BattleState.BattleOver;

            }
        } 
        else
        {
            // game over
            state = BattleState.BattleOver;

        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit!");

        if(damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's super effective!");
        else if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's not very effective!");


    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if(state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if (state == BattleState.Bag)
        {
            Action onBack = () =>
            {
                inventoryUI.gameObject.SetActive(false);
                state = BattleState.ActionSelection;
            };

            Action<ItemBase> onItemUsed = (ItemBase usedItem) =>
            {
                StartCoroutine(OnItemUsed(usedItem));
            };

            inventoryUI.HandleUpdate(onBack, onItemUsed);
        }

    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                // Fight
                MoveSelection();
            } 
            else if (currentAction == 1)
            {
                // Bag
                OpenBag();
                // StartCoroutine(RunTurns(BattleAction.UseItem));
            }
            else if (currentAction == 2)
            {
                // Dragon
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                // Run
                Application.Quit();
            }
        }
    }

    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Dragon.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Dragon.Moves[currentMove]);

        if(Input.GetKeyDown(KeyCode.Z))
        {
            var move = playerUnit.Dragon.Moves[currentMove];
            if (move.PP == 0) return;

            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));

        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        Action onSelected = () =>
        {
            var selectedMember = partyScreen.SelectedMember;
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted dragon");
                return;
            }
            if (selectedMember == playerUnit.Dragon)
            {
                partyScreen.SetMessageText("You can't switch with the same dragon");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.ActionSelection)
            {
                StartCoroutine(RunTurns(BattleAction.SwitchDragon));
            }
            else
            {
                state = BattleState.MoveSelection;

                dialogBox.EnableActionSelector(false);

                StartCoroutine(SwitchDragon(selectedMember));
            }
            dialogBox.EnableActionSelector(false);

            partyScreen.CalledFrom = null;
        };

        Action onBack = () =>
        {
            partyScreen.gameObject.SetActive(false);
            ActionSelection();
            partyScreen.CalledFrom = null;
        };

        partyScreen.HandleUpdate(onSelected, onBack);
        

    }

    IEnumerator SwitchDragon(Dragon newDragon)
    {

        if (playerUnit.Dragon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Dragon.Base.Name}");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

        }

        playerUnit.Setup(newDragon);
        dialogBox.SetMoveNames(newDragon.Moves);

        yield return dialogBox.TypeDialog($"Go {newDragon.Base.Name}!");

        state = BattleState.RunningTurn;
      
        //StartCoroutine(EnemyMove());
    }

    IEnumerator OnItemUsed(ItemBase usedItem)
    {
        state = BattleState.Busy;
        inventoryUI.gameObject.SetActive(false);

        if (usedItem is DragonBallItem)
        {
            yield return ThrowDragonball((DragonBallItem)usedItem);
        }

        StartCoroutine(RunTurns(BattleAction.UseItem));
    }

    IEnumerator ThrowDragonball(DragonBallItem dragonBallItem)
    {
        state = BattleState.Busy;
        dialogBox.EnableActionSelector(false);

        yield return dialogBox.TypeDialog($"Player used {dragonBallItem.Name.ToUpper()}!");
        var dragonballObj = Instantiate(dragonballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var dragonball = dragonballObj.GetComponent<SpriteRenderer>();
        dragonball.sprite = dragonBallItem.Icon;

        //Animations
        yield return dragonball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 1.5f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return dragonball.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchDragon(enemyUnit.Dragon, dragonBallItem);

        for(int i = 0; i < Mathf.Min(shakeCount, 3); i++)
        {
            yield return new WaitForSeconds(0.5f);
            yield return dragonball.transform.DOPunchRotation(new Vector3(0, 0, 20f), 0.8f).WaitForCompletion();
        }

        if (shakeCount == 4)
        {
            // Dragon is caught
            yield return dialogBox.TypeDialog($"Gotcha! {enemyUnit.Dragon.Base.Name} was caught");
            yield return dragonball.DOFade(0, 1.5f).WaitForCompletion();
            yield return dialogBox.TypeDialog($"{enemyUnit.Dragon.Base.Name} has been added to your party");


            Destroy(dragonball);
            state = BattleState.BattleOver;
            yield return new WaitForSeconds(5f);
            Application.Quit();
        } 
        else
        {
            // Dragon broke out
            yield return new WaitForSeconds(1f);
            dragonball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if (shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Dragon.Base.Name} broke free");
            else
                yield return dialogBox.TypeDialog($"Almost caught it");

            Destroy(dragonball);
            state = BattleState.RunningTurn; 

        }
    }

    int TryToCatchDragon(Dragon dragon, DragonBallItem dragonBallItem)
    {
        float a = (3 * dragon.MaxHp - 2 * dragon.HP) * dragon.Base.CatchRate * dragonBallItem.CatchRateModifier * ConditionDB.GetStatusBonus(dragon.Status) / (3 * dragon.MaxHp);

        if (a >= 255)
            return 4;

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;

            ++shakeCount;
        }

        return shakeCount;
    }
}
