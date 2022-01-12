using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Roller : MonoBehaviour
{
    public enum RollType { Normal, Advantage, Disadvantage };

    public GameObject inputNumberOfAttacks;
    public GameObject inputTargetAC;
    public GameObject inputAttackModifier;
    public GameObject inputRollType;
    public GameObject inputDamageExpression;

    public int defaultNumberOfAttacks = 1;
    public int defaultTargetAC = 10;
    public int defaultAttackModifier = 4;
    public RollType defaultRollType = RollType.Normal;
    public string defaultDamageExpression = "1d6+2";


    public bool allowGlancingBlowHouseRule = true;
    public bool allowCrits = true; // toggle to ignore crits with Adamantine armor

    public struct DieResults
    {
        public int Damage;
        public int BaseRoll;
        //public int modifiedRoll;

        public DieResults(int damage, int baseRoll)
        {
            this.Damage = damage;
            this.BaseRoll = baseRoll;
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
            DieResults attack = Attack(GetTargetAC(), GetAttackModifier(), GetRollType(), GetDamageExpression());
            Debug.Log(attack.BaseRoll + " to hit, " + attack.Damage + " (" + GetDamageExpression() + ") damage!");
        }

        if (GUI.Button(new Rect(150, 70, 150, 30), "Roll Multi-Attack!"))
        {
            int damage = MultiAttack(GetNumberOfAttacks(), GetTargetAC(), GetAttackModifier(), GetRollType(), GetDamageExpression());
            //Debug.Log(damage + " damage!");
        }
    }

    int MultiAttack(int numberOfAttacks, int targetAC, int attackModifier, RollType rollType, string damageStr)
    {
        int hits = 0;
        int totalDamage = 0;
        for (int i = 0; i < numberOfAttacks; i++)
        {
            int attackDamage = Attack(targetAC, attackModifier, rollType, damageStr).Damage; //todo
            totalDamage += attackDamage;
            if (attackDamage > 0)
                hits += 1;
        }
        Debug.Log(hits + " Attacks out of " + numberOfAttacks + " hit, dealing " + totalDamage + " damage!");
        return totalDamage;
    }

    DieResults Attack(int targetAC, int attackModifier, RollType rollType, string damageStr)
    {
        int roll = Random.Range(1, 21);
        bool hit = false;        
        bool crit = false;
        bool glancingBlow = false;

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
        // Normal Hit
        if (roll + attackModifier >= targetAC) //TODO add dice expression to attack mod for effects like bless
        {
            hit = true;
        }
        // Glancing Blow
        if (roll + attackModifier == targetAC)
        {
            hit = true;
            if (allowGlancingBlowHouseRule)
                glancingBlow = true;
        }
        // Natural 20, Critical Hit
        if (roll == 20)
        {
            hit = true;
            glancingBlow = false;
            if (allowCrits)
                crit = true;
        }
        // Natural 1, Critical Miss
        if (roll == 1)
        {
            hit = false;
        }        

        int toHit = roll + attackModifier;
        //Debug.Log(toHit + " to hit AC " + targetAC + ", Hit: " + hit + ", Crit: " + crit + ", Glancing: " + glancingBlow);

        int damage = 0;
        if (hit)
        {
            damage = Damage(damageStr, crit, glancingBlow);
        }
        return new DieResults(damage, roll);
    }

    int Damage(string damageStr, bool crit, bool glancingBlow)
    {
        string[] diceExpressions = damageStr.Split('+');

        int totalDamage = 0;
        foreach (string dieExpression in diceExpressions)
        {
            totalDamage += DamageHelper(dieExpression, crit);
        }
        if (totalDamage < 0)
            totalDamage = 0;
        if (glancingBlow)
            totalDamage = Mathf.FloorToInt(totalDamage / 2);

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
            int attackModifier = 0;
            if (int.TryParse(inputAttackModifier.GetComponent<TMP_InputField>().text, out attackModifier))
                return attackModifier;
        }
        return defaultAttackModifier;
    }

    RollType GetRollType() //TODO
    {
        /*if (inputRollType != null)
        {
            int rollType = 0;
            if (int.TryParse(inputRollType.GetComponent<TMP_InputField>().text, out rollType))
                return rollType;
        }*/
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
    #endregion
}
