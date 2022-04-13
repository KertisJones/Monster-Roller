using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Roller : MonoBehaviour
{
    public enum RollType { Normal, Advantage, Disadvantage };

    public GameObject inputNumberOfAttacks;
    public GameObject inputTargetAC;
    public GameObject inputAttackModifier;
    public GameObject inputRollType;
    public GameObject inputDamageExpression;
    public GameObject inputAllowCrits;
    public GameObject inputGlancingBlow;

    public int defaultNumberOfAttacks = 1;
    public int defaultTargetAC = 10;
    public int defaultAttackModifier = 4;
    public RollType defaultRollType = RollType.Normal;
    public string defaultDamageExpression = "1d6+2";


    public bool defaultGlancingBlow = true;
    public bool defaultAllowCrits = true; // toggle to ignore crits with Adamantine armor

    public struct MultiAttackResultResult
    {
        public List<AttackResult> AttackResultResults;

        public MultiAttackResultResult(List<AttackResult> AttackResultResults)
        {
            this.AttackResultResults = AttackResultResults;
        }

        public int GetTotalDamageDealt(int targetAC)
        {
            int damage = 0;
            foreach (AttackResult AttackResult in this.AttackResultResults)
            {
                damage += AttackResult.Damage;
            }
            return damage;
        }
    }
    
    public struct AttackResult
    {
        public int Damage;
        public int BaseRoll;
        public int AttackModifier;
        //public int modifiedRoll;

        public AttackResult(int damage, int baseRoll, int AttackModifier)
        {
            this.Damage = damage;
            this.BaseRoll = baseRoll;
            this.AttackModifier = AttackModifier;
        }

        public int GetDamageDealt(int targetAC, bool allowGlancingBlowHouseRule)
        {
            bool hit = false;
            bool glancingBlow = false;
            // Normal Hit
            if (BaseRoll + AttackModifier >= targetAC) //TODO add dice expression to Attack mod for effects like bless
            {
                hit = true;
            }
            // Glancing Blow
            if (BaseRoll + AttackModifier == targetAC)
            {
                hit = true;
                if (allowGlancingBlowHouseRule)
                    glancingBlow = true;
            }
            // Crit success/fail
            if (BaseRoll == 1)
            {
                hit = false;
                glancingBlow = false;
            }
            else if (BaseRoll == 20)
            {
                hit = true;
                glancingBlow = false;
            }

            if (hit && glancingBlow)
                return Mathf.FloorToInt(Damage / 2);
            else if (hit)
                return Damage;
            else 
                return 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 70, 100, 30), "Roll Attack!"))
        {
            AttackResult AttackResult = Attack(GetAttackModifier(), GetRollType(), GetDamageExpression());
            Debug.Log(AttackResult.BaseRoll + " to hit, " + AttackResult.Damage + " (" + GetDamageExpression() + ") damage!");
        }

        if (GUI.Button(new Rect(150, 70, 150, 30), "Roll Multi-Attack!"))
        {
            int damage = MultiAttack(GetNumberOfAttacks(), GetTargetAC(), GetAttackModifier(), GetRollType(), GetDamageExpression());
            //Debug.Log(damage + " damage!");
        }
    }

    int MultiAttack(int numberOfAttacks, int targetAC, int AttackModifier, RollType rollType, string damageStr)
    {
        int hits = 0;
        int totalDamage = 0;
        string attackRollsString = "";
        string damageRollsString = "";
        for (int i = 0; i < numberOfAttacks; i++)
        {
            //int AttackDamage = Attack(AttackModifier, rollType, damageStr).GetDamageDealt(targetAC, allowGlancingBlowHouseRule); //todo
            AttackResult attack = Attack(AttackModifier, rollType, damageStr);
            int damage = attack.GetDamageDealt(targetAC, GetAllowGlancingBlow());
            int attackRoll = attack.BaseRoll + attack.AttackModifier;

            attackRollsString += ", " + attackRoll;
            

            totalDamage += damage;
            if (attackRoll >= targetAC)
            {
                hits += 1;
                damageRollsString += ", " + damage;
            }
        }
        Debug.Log(hits + " Attack out of " + numberOfAttacks + " hit" + attackRollsString + "; dealing " + totalDamage + " damage!" + damageRollsString);
        return totalDamage;
    }

    AttackResult Attack(int AttackModifier, RollType rollType, string damageStr)
    {
        int roll = Random.Range(1, 21);
        //bool hit = false;        
        bool crit = false;
        bool critFail = false;
        //bool glancingBlow = false;

        // Advantage/Disadvantage
        if (rollType == RollType.Advantage)
        {
            int roll2 = Random.Range(1, 21);
            roll = Mathf.Max(roll, roll2);
        }
        else if (rollType == RollType.Disadvantage)
        {
            int roll2 = Random.Range(1, 21);
            roll = Mathf.Min(roll, roll2);
        }

        /*// Normal Hit
        if (roll + AttackModifier >= targetAC) //TODO add dice expression to Attack mod for effects like bless
        {
            hit = true;
        }
        // Glancing Blow
        if (roll + AttackModifier == targetAC)
        {
            hit = true;
            if (allowGlancingBlowHouseRule)
                glancingBlow = true;
        }*/
        // Natural 20, Critical Hit
        if (roll == 20)
        {
            //hit = true;
            //glancingBlow = false;
            if (GetAllowCrits())
                crit = true;
        }
        // Natural 1, Critical Miss
        if (roll == 1)
        {
            critFail = true;
            //hit = false;
        }        

        int toHit = roll + AttackModifier;
        //Debug.Log(toHit + " to hit AC " + targetAC + ", Hit: " + hit + ", Crit: " + crit + ", Glancing: " + glancingBlow);

        int damage = 0;
        if (!critFail)
        {
            damage = Damage(damageStr, crit);//, glancingBlow);
        }
        return new AttackResult(damage, roll, AttackModifier);
    }

    int Damage(string damageStr, bool crit)//, bool glancingBlow)
    {
        string[] diceExpressions = damageStr.Split('+');

        int totalDamage = 0;
        foreach (string dieExpression in diceExpressions)
        {
            totalDamage += DamageHelper(dieExpression, crit);
        }
        if (totalDamage < 0)
            totalDamage = 0;
        /*if (glancingBlow)
            totalDamage = Mathf.FloorToInt(totalDamage / 2);*/

        return totalDamage;
    }

    int DamageHelper(string dieExpression, bool crit)
    {
        // dieExpression is empty, return 0
        if (dieExpression == "")
            return 0;

        // dieExpression is only a number, return that number
        int damage = 0;
        if (int.TryParse(dieExpression, out damage))
            return damage;

        // dieExpression contains a subtraction, use recursion to subtract each expression
        if (dieExpression.Contains("-"))
        {
            string[] subtractExpressions = dieExpression.Split('-'); //TODO subtraction doesn't work
            damage = DamageHelper(subtractExpressions[0], crit);
            for (int i = 1; i < subtractExpressions.Length; i++)
            {
                damage -= DamageHelper(subtractExpressions[i], false);
            }
            return damage;
        }

        // If none of the previous conditions are true, dieExpression should be formated as "xdy"
        if (dieExpression.Contains("d"))
        {
            string[] dieExpressionValues = dieExpression.Split('d');
            int numberOfDice = 0;
            int dieSize = 0;

            //Error Handling
            if (dieExpressionValues.Length < 2)
            {
                Debug.LogWarning("Unsupported dice format detected, please use the format 'xdy+z'. Invalid expression: " + dieExpression);
                return 0;
            }
            if (dieExpressionValues.Length > 2)
                Debug.LogWarning("Unsupported dice format detected, please use the format 'xdy+z'. Invalid expression: " + dieExpression);
            if (!int.TryParse(dieExpressionValues[0], out numberOfDice))
            {
                Debug.LogWarning("Unsupported dice format detected, please use the format 'xdy+z'. Invalid expression: " + dieExpression);
                return 0;
            }
            if (!int.TryParse(dieExpressionValues[1], out dieSize))
            {
                Debug.LogWarning("Unsupported dice format detected, please use the format 'xdy+z'. Invalid expression: " + dieExpression);
                return 0;
            }
            if (crit)
                numberOfDice *= 2;
            return RollDice(numberOfDice, dieSize); // xdy
        }

        Debug.LogWarning("Unsupported dice format detected, please use the format 'xdy+z'. Invalid expression: " + dieExpression);
        return 0;
    }

    int RollDice (int numberOfDice, int dieSize)
    {
        int totalDamage = 0;
        for (int i = 0; i < numberOfDice; i++)
        {
            totalDamage += Random.Range(1, dieSize + 1);
        }
        return totalDamage;
    }
    
    #region GetVariables
    int GetNumberOfAttacks()
    {
        if (inputNumberOfAttacks != null)
        {
            int numberOfAttacks = 0;
            if (int.TryParse(inputNumberOfAttacks.GetComponent<TMP_InputField>().text, out numberOfAttacks))
                return numberOfAttacks;
        }
        return defaultNumberOfAttacks;
    }

    int GetTargetAC()
    {
        if (inputTargetAC != null)
        {
            int targetAC = 0;
            if (int.TryParse(inputTargetAC.GetComponent<TMP_InputField>().text, out targetAC))
                return targetAC;
        }
        return defaultTargetAC;
    }

    int GetAttackModifier()
    {
        if (inputAttackModifier != null)
        {
            int AttackModifier = 0;
            if (int.TryParse(inputAttackModifier.GetComponent<TMP_InputField>().text, out AttackModifier))
                return AttackModifier;
        }
        return defaultAttackModifier;
    }

    RollType GetRollType() //TODO
    {
        if (inputRollType != null)
        {
            int rollTypeValue = inputRollType.GetComponent<TMP_Dropdown>().value;
            switch (rollTypeValue)
            {
                case 0:
                    return RollType.Normal;
                case 1:
                    return RollType.Advantage;
                case 2:
                    return RollType.Disadvantage;
                default:
                    return defaultRollType;
            }            
        }
        return defaultRollType;
    }

    string GetDamageExpression()
    {
        string damageExpression = defaultDamageExpression;
        if (inputDamageExpression != null)
            damageExpression = inputDamageExpression.GetComponent<TMP_InputField>().text;
        if (damageExpression == "")
            damageExpression = defaultDamageExpression;          
        return damageExpression;
    }

    bool GetAllowCrits()
    {
        if (inputAllowCrits != null)
        {
            return inputAllowCrits.GetComponent<Toggle>().isOn;
        }
        return defaultAllowCrits;
    }

    bool GetAllowGlancingBlow()
    {
        if (inputGlancingBlow != null)
        {
            return inputGlancingBlow.GetComponent<Toggle>().isOn;
        }
        return defaultGlancingBlow;
    }
    #endregion
}
