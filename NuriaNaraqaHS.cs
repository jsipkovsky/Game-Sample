using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// ----- NURIA/NARAQA (CHILDREN OF CELIX) ----


public class NuriaNaraqaHS : ChildrenOfCelix
{
    //----------------------------------
    // ATTRIBUTES

    // Type of Attack Unit is currently immune to
    private string Immunity;

    // Skill selection Buttons
    GameObject btnPhysical;
    GameObject btnMagical;
    GameObject btnSpecial;

    // List of currently wrapped Units
    private List<NewUnit> wrappedUnits = new List<NewUnit>();

    // Intitialize Impact
    private Impact wrap;
    private ImpactValues value = new ImpactValues();
    private List<ImpactValues> values = new List<ImpactValues>();

    //----------------------------------
    // METHODS

    void Start() // Initialize 'God's Wrap' Impact & selection Buttons
    {
        //----------------------------------
        // God's wrap

        value.field = "HealthAct";
        value.value = 30;
        values.Add(value);

        wrap = new Impact
        {
            name = "God's Wrap",
            endTurn = 0,
            exception = 2,
            isDamage = true,
            OnDeathAction = true,
            trigger = this,
            isBuff = false,
            fieldsChanged = values
        };

        // Selection button (to be updated)
        btnPhysical = GameObject.Find("Skill Bar").transform.Find("Selection 1").gameObject;
        btnMagical = GameObject.Find("Skill Bar").transform.Find("Selection 2").gameObject;
        btnSpecial = GameObject.Find("Skill Bar").transform.Find("Selection 3").gameObject;
    }

    //----------------------------------
    // Game flow mechanics

    // Method for processing actions on Nuria's Turn end

    public override void UpdateAtEndTurn()
    {
        base.UpdateAtEndTurn();

        // Deactivate Selection buttons
        btnPhysical.SetActive(false);
        btnPhysical.GetComponentInChildren<Text>().text = "";
        btnMagical.gameObject.SetActive(false);
        btnMagical.GetComponentInChildren<Text>().text = "";
        btnSpecial.SetActive(false);
        btnSpecial.GetComponentInChildren<Text>().text = "";
    }

    //----------------------------------
    // Attack processing

    public override void Attack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        int damage = 0;

        // Calculate final Damage
        if (defender.IsImmune != true)
        {
            float coeficient = CalculateAttack(attacker, defender);
            damage = Mathf.RoundToInt(coeficient * attacker.Damage);
        }
        else
        {
            damage = defender.CalculateReduced(attacker, defender, false);
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

            // Substract Health (defender)
            defender.HealthAct -= damage;
            combatLog.LogAttack(attacker.UnitName + " caused  " + damage + " damage to " + defender.UnitName + "."); // Log Attack

            // Substract Action Points
            attacker.ActionsPointsAct -= attacker.AttackPrice;

            if (wrappedUnits.Count != 3)
            {
                defender.ImpactHandle.ApplyNew(wrap);
                wrappedUnits.Add(defender);
            }
            else
            {
                wrappedUnits[0].ImpactHandle.RemoveImpactByName("God's Wrap");
                wrappedUnits.RemoveAt(0);

                defender.ImpactHandle.ApplyNew(wrap);
                wrappedUnits.Add(defender);
            }

            // CounterAction (defender)
            if (defender.DoCounterAct == true)
            {
                defender.CounterAction(defender);
            }

            // Is alive check (defender)
            defender.IsAliveCheck(false);

        } else
        {
            // Show Damage done if Unit entered
            defender.ShowDamage(damage, CursorSetter.melee_spr);
        }
    }

    // Method for processing Attack when some immunity is one

    public override int CalculateReduced(NewUnit attacker, NewUnit defender, bool ignoreDef)
    {
        var immunity = Immunity.Substring(0, 5); // To find relevant Attack type in List
        float attackSum = 0;                     // Initialize attack sum
        float damage = attacker.Damage;          // Attacker Damage
        float koef = 0;                          // Initialize coeficient

        // Create and sort Attack's List
        attack.AttackMark = "PhysicalDefence";
        attack.AttackValue = attacker.PhysicalAttack;
        attacks.Add(attack);
        attackSum += attacker.PhysicalAttack;
        attack.AttackMark = "MagicalDefence";
        attack.AttackValue = attacker.MagicalAttack;
        attacks.Add(attack);
        attackSum += attacker.MagicalAttack;
        attack.AttackMark = "SpecialDefence";
        attack.AttackValue = attacker.SpecialAttack;
        attacks.Add(attack);
        attackSum += attacker.SpecialAttack;

        // Sort Attacks to determine Primary and Secondary Attack
        attacks = attacks.OrderByDescending(n => n.AttackValue).ToList();

        // Calculate relevant Damage part
        var immunePart = attacks.FindLast(n => n.AttackMark.Contains(immunity)).AttackValue;
        if (immunePart != 0)
        {
            damage = damage * ((attackSum - immunePart) / attackSum);
        }

        if (attacks[0].AttackValue > 0 && !attacks[0].AttackMark.Contains(immunity)) // Primary Attack type
        {
            int defenceValue = 0;
            if (ignoreDef == false)
            {
                defenceValue = (int)defender.GetType().GetField(attacks[0].AttackMark).GetValue(defender);
            }
            koef = koef + (attacks[0].AttackValue - defenceValue) * 1.5f; // Extra damage (1.5f)
        }

        if (attacks[1].AttackValue > 0 && !attacks[1].AttackMark.Contains(immunity)) // Secondary Attack type
        {
            int defenceValue = 0;
            if (ignoreDef == false) 
            {
                defenceValue = (int)defender.GetType().GetField(attacks[1].AttackMark).GetValue(defender);
            }
            koef = koef + ((attacks[1].AttackValue - defenceValue) / 2) * 1.5f; // Extra damage (1.5f)
        }

        koef = 1 + (koef / 100);                  // Calculate coefficient
        damage = Mathf.RoundToInt(koef * damage); // Calculate final Damage

        return (int)damage;
    }

    //----------------------------------
    // Skills

    // Method for processing Immune type selection

    public override void ExecuteSelection(string selection)
    {
        //base.ExecuteSelection(selection);

        Immunity = selection; // Selected immunity
        IsImmune = true;      // Mark as Immune

        UnitSkills[0].isClickable = false; // Deactivate until immunity ends

        // Deactivate Selection buttons
        btnPhysical.SetActive(false);
        btnPhysical.GetComponentInChildren<Text>().text = "";
        btnMagical.gameObject.SetActive(false);
        btnMagical.GetComponentInChildren<Text>().text = "";
        btnSpecial.SetActive(false);
        btnSpecial.GetComponentInChildren<Text>().text = "";

        // Create Impact for immunity
        Impact immune = new Impact
        {
            name = selection,
            endTurn = controller.Turn + 3,
            trigger = this,
            OnExpireAction = true,
            isBuff = true,
        };

        UnitImpacts.Add(immune);

        IsImmobile = false;  // Allow movement
        unitAllowed.Clear(); // Allow Targets

        // Substract Action Points
        ActionsPointsAct -= UnitSkills[0].skillPrice;

        // Call update of paths, Targets, info bar
        base.ExecuteSelection(selection);
    }

    // Method for processing expired immunity

    public override void TargetImpactExpired(NewUnit unit)
    {
        Immunity = "";
        IsImmune = false;
        UnitSkills[0].isClickable = true;
        UnitSkills[0].IsActivated = false;
    }

    // Method for activate/deactivate 'Armor of Faith' Skill

    public override void ActivateSkill1()
    {
        if (UnitSkills[0].IsActivated == false)
        {
            UnitSkills[0].IsActivated = true; // Skill activated

            IsImmobile = true;     // disable movement
            unitAllowed.Add(this); // disable targets

            // Activate Selection buttons
            btnPhysical.SetActive(true);
            btnPhysical.GetComponentInChildren<Text>().text = "Physical Immune";
            btnMagical.gameObject.SetActive(true);
            btnMagical.GetComponentInChildren<Text>().text = "Magical Immune";
            btnSpecial.SetActive(true);
            btnSpecial.GetComponentInChildren<Text>().text = "Special Immune";
        }
        else
        {
            UnitSkills[0].IsActivated = false;

            IsImmobile = false;  // allow movement
            unitAllowed.Clear(); // allow targets

            // Deactivate Selection buttons
            btnPhysical.SetActive(false);
            btnPhysical.GetComponentInChildren<Text>().text = "";
            btnMagical.gameObject.SetActive(false);
            btnMagical.GetComponentInChildren<Text>().text = "";
            btnSpecial.SetActive(false);
            btnSpecial.GetComponentInChildren<Text>().text = "";
        }
    }
}
