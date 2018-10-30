// ----- GARUMI (BLOODSWORN) -----


public class GarumiHS : Bloodsworn
{
    //----------------------------------
    // ATTRIBUTES

    private NewUnit tauntedUnit; // Taunted Unit (only enemy possible - both sided)
    private bool WallOn;         // Check 'Wall of Iron' is activated/deactivated

    //----------------------------------
    // METHODS

    //----------------------------------
    // Game Flow mechanics

    public override void UpdateAtStartTurn()
    {
        if (tauntedUnit != null)
        {
            unitAllowed.Add(tauntedUnit);
            IsImmobile = true;
        }
        BloodswornTurnStart();
        base.UpdateAtStartTurn();
    }

    // Method for clearing Taunted Unit when Target dies

    public override void TargetDied(NewUnit unit)
    {
        tauntedUnit = null;
        FatigueLock = false;
        UnitSkills[0].isClickable = true;
    }

    //----------------------------------
    // Skills

    // Method for clearing Taunted Unit when Impact expires

    public override void TargetImpactExpired(NewUnit unit)
    {
        if (tauntedUnit != null) // = 'Issue a Challenge' expired
        {
            tauntedUnit = null;
            FatigueLock = false;
        }
        else // = 'Wall of Iron' expired
        {
            IsImmune = false;
        }
        // Allow both Skills
        UnitSkills[0].isClickable = true;
        UnitSkills[1].isClickable = true;
    }

    // Method for execute Skill Issue a Challenge

    public override void ExecuteSkill(NewUnit receiver, Cell targetCell = null)
    {
        // Check if Impact will be 'used' in Turn of Skill execution
        var endTurn = 0;
        if (receiver.PlayedInTurn == true)
        {
            endTurn = controller.Turn + 5;

        } else
        {
            endTurn = controller.Turn + 4;
        }

        // Update Target
        Impact taunted = new Impact
        {
            name = "Issue a Challenge",
            endTurn = endTurn,
            OnEndProcess = true,
            exception = 1,
            isDamage = false,
            OnDeathAction = true,
            OnExpireAction = true,
            trigger = this,
            isBuff = false

        };
        // Disable both Skills
        UnitSkills[0].isClickable = false;
        UnitSkills[1].isClickable = false;

        receiver.ImpactHandle.ApplyNew(taunted);
        tauntedUnit = receiver;
        controller.SkillRange = 0;
        BloodswornProgress(); // add Fatigue for this Skill
        FatigueLock = true;   // disable Fatigue deduction
    }

    // Method for handle Skill Issue a Challenge

    public override void ActivateSkill1()
    {
        if (UnitSkills[0].IsActivated == false)
        {
            UnitSkills[0].IsActivated = true;
            controller.SkillRange = UnitSkills[0].SkillRange; // Set Skill Range
            ActionMode = 1; // SKILL-GRID
        }
        else
        {
            UnitSkills[0].IsActivated = false;
            controller.SkillRange = 0; // Reset Skill Range
            ActionMode = 0; // STANDARD 
        }
    }

    // Method for handle Skill 'Wall of Iron'

    public override void ActivateSkill2()
    {
        if (UnitSkills[1].IsActivated == false)
        {
            UnitSkills[1].IsActivated = true;

            // Disable both Skills
            UnitSkills[0].isClickable = false;
            UnitSkills[1].isClickable = false;

            // Process 'Wall of Iron'
            WallOfIron();
        }
    }

    // Method for applying 'Wall of Iron'

    private void WallOfIron()
    {
        ActionsPointsAct -= UnitSkills[1].skillPrice;
        BloodswornProgress(); // add Fatigue for this Skill
        IsImmune = true;

        // Create 'Wall of Iron' Impact
        Impact wall = new Impact
        {
            name = "Wall of Iron",
            endTurn = controller.Turn + 2,
            OnEndProcess = false,
            OnExpireAction = true,
            trigger = this,
            isBuff = true,
        };
        ImpactHandle.ApplyNew(wall); // Apply Impact (Garumi)
    }
}
