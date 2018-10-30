using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Destructive Harmony structure
public struct Harmony
{
    public bool IsOn;
    public int Deadline;
}

// ----- KASHEER (CHILDREN OF CELIX) ----


public class KasheerHS: ChildrenOfCelix
{
    //----------------------------------
    // ATTRIBUTES

    public bool HandIsActive;     // Indicator if Hand of Celix is active
    private float coeficient = 1; // Attack coenficient (standard/HoC)
    private Harmony Harmony;      // Destructive Harmony controller structure

    // Intitialize Impact values
    private ImpactValues value = new ImpactValues();
    private List<ImpactValues> values = new List<ImpactValues>();

    //----------------------------------
    // METHODS

    private void Start()
    {
        // Set 'Destructive Harmony' values
        value.field = "ActionPoints";
        value.value = -2;
        values.Add(value);
    }

    //----------------------------------
    // Game flow mechanics

    // Method for processing actions on Nuria's Turn end

    public override void UpdateAtEndTurn()
    {
        base.UpdateAtEndTurn();

        // Process Destructive Harmony efect if relevant
        if (Harmony.IsOn == true && Harmony.Deadline == controller.Turn)
        {
            Harmony.IsOn = false; // Turn off Harmony
            var damage = Mathf.RoundToInt(Health * 0.1f); 
            HealthAct -= damage;                         
            combatLog.LogSkill(" Destructive Harmony caused  " + damage + " damage to " + UnitName + ".");
            IsAliveCheck(true);

            if (this != null)
            {
                HarmonyImpact(this);
            }
            else
            {
                Debug.Log("Check if possible!");
            }
        }
    }

    //----------------------------------
    // Attack processing

    public override void Attack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        if (Harmony.IsOn == false)
        {
            // Calculate Damage
            int damage = 0;
            if (defender.IsImmune != true) // Target is not immune
            {
                if (HandIsActive == false) // Standard Attack
                {
                    coeficient = CalculateAttack(attacker, defender);
                }
                else // Hand of Celix Attack
                {
                    coeficient = HandOfCelixAttack(attacker, defender);
                }

                damage = Mathf.RoundToInt(coeficient * attacker.Damage);
            }
            else // Target is immune
            {
                if (HandIsActive == false)
                {
                    damage = defender.CalculateReduced(attacker, defender, false); // Standard Attack
                }
                else
                {
                    damage = defender.CalculateReduced(attacker, defender, true); // Hand of Celix Attack
                }
            }

            // Check if Attack is rear Attack
            if (attacker.RangeTo == 1)
            {
                if (RearAttack(attacker, defender) == true)
                {
                    Mathf.RoundToInt(damage * 1.3f); // TEST VALUE
                }
            }

            if (doDamage == true)
            {
                DrawLine(attacker.transform.position, defender.transform.position, Color.red, 0.5f);

                // Substract Action Points
                attacker.ActionsPointsAct -= attacker.AttackPrice;

                // Substract Health (defender)
                defender.HealthAct -= damage;
                combatLog.LogAttack(attacker.UnitName + " caused  " + damage + " damage to " + defender.UnitName + "."); // Log Attack

                // CounterAction (defender)
                if (defender.DoCounterAct == true)
                {
                    defender.CounterAction(defender);
                }

                // Is alive check (defender)
                defender.IsAliveCheck(false);

            }
            else
            {
                defender.ShowDamage(damage, CursorSetter.melee_spr); // Show Damage done if Unit entered
            }
        }
        else
        {
            // Process 'Destructive Harmony' Attack
            DestHarmonyAttack(attacker, defender, doDamage);
        }
    }

    // Calculate attack using Hand of Celix

    public float HandOfCelixAttack(NewUnit attacker, NewUnit defender)
    {
        float koef = 0; // initial coeficient

        // Create and sort Attack's List
        List<AttackStruc> attacks = new List<AttackStruc>();
        AttackStruc attack = new AttackStruc();

        attack.AttackMark = "PhysicalDefence";
        attack.AttackValue = attacker.PhysicalAttack;
        attacks.Add(attack);
        attack.AttackMark = "MagicalDefence";
        attack.AttackValue = attacker.MagicalAttack;
        attacks.Add(attack);
        attack.AttackMark = "SpecialDefence";
        attack.AttackValue = attacker.SpecialAttack;
        attacks.Add(attack);

        attacks = attacks.OrderByDescending(n => n.AttackValue).ToList();

        if (attacks[0].AttackValue > 0) // Primary Attack type
        {
            var defenceValue = 0; // ignore enemy Defense
            koef = koef + (attacks[0].AttackValue - defenceValue);
        }

        if (attacks[1].AttackValue > 0) // Secondary Attack type
        {
            var defenceValue = 0; // ignore enemy Defense
            koef = koef + ((attacks[1].AttackValue - defenceValue) / 2);
        }

        koef = 1 + (koef / 100);

        return koef;
    }

    // Process 'Destructive Harmony' Attack

    private void DestHarmonyAttack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        var damage = Mathf.RoundToInt(defender.Health * 0.25f);
        if (doDamage == false)
        {
            defender.ShowDamage(damage, CursorSetter.melee_spr); // Show Damage done if Unit entered
        }
        else
        {
            Harmony.IsOn = false;
            attacker.ActionsPointsAct -= attacker.AttackPrice;
            defender.HealthAct -= damage;
            combatLog.LogAttack(" Destructive Harmony caused  " + damage + " damage to " + defender.UnitName + ".");
            defender.IsAliveCheck(false);
            if (defender != null)
            {
                HarmonyImpact(defender);
            }
        }
    }

    //----------------------------------
    // Skills

    public override void ActivateSkill1()
    {
        if (UnitSkills[0].IsActivated == false)
        {
            UnitSkills[0].IsActivated = true;

            ActionsPointsAct = ActionsPointsAct - UnitSkills[0].skillPrice; // substract Action Points for switch 
            HandIsActive = true;

        } else
        {
            UnitSkills[0].IsActivated = false;

            ActionsPointsAct = ActionsPointsAct - UnitSkills[0].skillPrice; // substract Action Points for switch 
            HandIsActive = false;
        }
    }

    // Method for handle Skill 'Destructive Harmony'

    public override void ActivateSkill2()
    {
        if (UnitSkills[1].IsActivated == false)
        {
            UnitSkills[1].IsActivated = true;
            UnitSkills[1].isClickable = false;
            ActionsPointsAct -= UnitSkills[1].skillPrice; // substract Action Points for Skill
            Resource -= UnitSkills[1].resourcePrice;      // substract Fraction Resource for Skill
            Harmony.IsOn = true;
            Harmony.Deadline = controller.Turn + 1;
        }
    }

    // Method for applying 'Destructive Harmony' Impact

    private void HarmonyImpact(NewUnit target)
    {
        // Check if Impact will be 'used' in Turn of Skill execution
        var endTurn = controller.Turn + 1;
        if (target.PlayedInTurn == true) { endTurn = controller.Turn + 2; }

        // Create 'Destructive Harmony' Impact
        Impact harmony = new Impact
        {
            name = "Destructive Harmony",
            endTurn = endTurn,
            OnEndProcess = true,
            trigger = this,
            isBuff = false,
            fieldsChanged = values
        };
        target.ImpactHandle.ApplyNew(harmony); // Apply Impact (defender)
    }
}


