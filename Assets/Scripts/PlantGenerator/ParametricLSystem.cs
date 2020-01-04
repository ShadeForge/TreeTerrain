using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public class ParametricLSystem
{
    public class Rule
    {
        public string key;
        public string rValue;
        public string condition;
        public List<Expression> expressions;

        public Rule(string key, string rValue, string condition = "")
        {
            this.key = key;
            this.rValue = rValue;
            this.condition = condition;
            expressions = new List<Expression>();
        }
    }

    public Dictionary<string, List<Rule>> rules;
    public ExpressionParser parser;

    public string axiom;
    public int iterations;

    public ParametricLSystem()
    {
        rules = new Dictionary<string, List<Rule>>();
        parser = new ExpressionParser();
        if (!ParseRules(ref rules))
        {
            Debug.LogError("Parsing Error: Could not parse rules");
        }
    }

    private bool ParseRule(string key, string rValue, out Rule rule, string condition = "")
    {
        rule = new Rule(key, rValue);
        Vector2Int[] ranges;
        if (!ParseTermRanges(rule.rValue, out ranges))
        {
            Debug.LogError("Rule: '" + rule.key + " = " + rule.rValue + "'");
            return false;
        }

        rule.expressions.Clear();

        for (int i = 0; i < ranges.Length; i++)
        {
            rule.condition = condition;
            try
            {
                String term = rule.rValue.Substring(ranges[i].x, ranges[i].y - ranges[i].x);
                rule.expressions.Add(parser.EvaluateExpression(term));
            }
            catch (ExpressionParser.ParseException exception)
            {
                Debug.LogError("Expression Parsing Error: " + exception.Message + "\n" +
                               "Rule: '" + rule.key + " = " + rule.rValue + "'");
                return false;
            }
        }
        
        return true;
    }

    private bool ParseRules(ref Dictionary<string, List<Rule>> rules)
    {
        List<string> keys = new List<string>(rules.Keys);
        List<List<Rule>> ruleList = new List<List<Rule>>(rules.Values);

        rules.Clear();

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            List<Rule> rule = ruleList[i];
            for (int j = 0; j < rule.Count; j++)
            {
                Rule r = rule[j];
                if (!ParseRule(key, r.rValue, out r, r.condition))
                {
                    Debug.LogError("Parsing Error: Could not parse rules");
                    return false;
                }

                if (!rules.ContainsKey(key))
                {
                    rules.Add(key, new List<Rule>());
                }
                rules[key].Add(r);
            }
        }

        return true;
    }

    public bool AddRule(string key, string rValue, string condition = "")
    {
        Rule rule;

        key = key.Replace(" ", "");
        rValue = rValue.Replace(" ", "");
        condition = condition.Replace(" ", "");

        if (!ParseRule(key, rValue, out rule, condition))
        {
            Debug.LogError("Parsing Error: Could not parse rule on Add Rule");
            return false;
        }

        if (rules.ContainsKey(key))
        {
            rules[key].Add(rule);
        }
        else
        {
            rules.Add(key, new List<Rule>() {rule});
        }

        return true;
    }

    public bool EditRule(string key, int index, string rValue, string condition = "")
    {
        key = key.Replace(" ", "");
        rValue = rValue.Replace(" ", "");
        condition = condition.Replace(" ", "");

        if (rules.ContainsKey(key))
        {
            Rule rule;

            if (!ParseRule(key, rValue, out rule, condition))
            {
                Debug.LogError("Parsing Error: Could not parse rule on Add Rule");
                return false;
            }

            rules[key][index] = rule;
        }
        else
        {
            Debug.LogError("Dictionary Error: Key doesn't exists\n" +
                           "Rule: '" + key + " = " + rValue + "'");
            return false;
        }
        return true;
    }

    public bool DeleteRule(string key, int i)
    {
        if (rules[key].Count <= 1)
        {
            return rules.Remove(key);
        }
        else
        {
            rules[key].RemoveAt(i);
            return true;
        }
    }

    public bool GenerateSequence(out string result)
    {
        result = "";
        if (!GenerateSequence(axiom, out result, iterations))
        {
            return false;
        }
        return true;
    }

    public bool GenerateSequence(string sequence, out string result)
    {
        int nextStep = 0;
        result = "";

        for (int i = 0; i < sequence.Length; i+= nextStep)
        {
            string current = sequence[i].ToString();

            List<KeyValuePair<string, List<Rule>>> keyPairs = new List<KeyValuePair<string, List<Rule>>>(
                rules.Where(x => x.Key.StartsWith(current.ToString())));

            if (i + 1 < sequence.Length && sequence[i + 1] == '(')
            {
                if (keyPairs.Count == 1)
                {
                    if (keyPairs[0].Value.Count > 0)
                    {
                        string parameterStr = sequence.Substring(i + 1);
                        int lastClosingBracketIndex = -1;
                        string parameterNameStr;
                        string[] parameterNames = null;

                        if (!IndexOfLastClosingBracket(parameterStr, out lastClosingBracketIndex))
                        {
                            return false;
                        }

                        parameterStr = parameterStr.Remove(lastClosingBracketIndex).Remove(0, 1);
                        parameterStr = EvaluateParameterExpressions(parameterStr);
                        string[] parameters = parameterStr.Split(',');
                        
                        Rule rule = null;

                        if (keyPairs[0].Value.Count == 1)
                        {
                            rule = keyPairs[0].Value[0];
                            parameterNameStr = rule.key.Substring(2);
                            parameterNameStr = parameterNameStr.Remove(parameterNameStr.Length - 1);
                            parameterNames = parameterNameStr.Split(',');
                        }
                        else
                        {
                            bool success = false;

                            foreach (Rule r in keyPairs[0].Value)
                            {
                                parameterNameStr = r.key.Substring(2);
                                parameterNameStr = parameterNameStr.Remove(parameterNameStr.Length - 1);
                                parameterNames = parameterNameStr.Split(',');
                                Parameter[] param = new Parameter[parameters.Length];

                                for (int j = 0; j < param.Length; j++)
                                {
                                    param[j] = new Parameter(parameterNames[j]);
                                    param[j].Value = Double.Parse(parameters[j]);
                                }

                                if (r.condition == "")
                                {
                                    Debug.LogError("Parsing Error: overloaded rule has no condition");
                                    return false;
                                }

                                if (LogicEquationParser.EvaluateEquation(r.condition, param, parser))
                                {
                                    rule = r;
                                    success = true;
                                    break;
                                }
                            }

                            if (!success)
                            {
                                Debug.LogError("Parsing Error: No condition of any rule with '" + current +
                                               "(...)' was successful");
                                return false;
                            }
                        }

                        List<Expression> expressions = rule.expressions;
                        List<double> expressionResults = new List<double>();



                        if (parameterNames.Length != parameters.Length)
                        {
                            Debug.LogError("Parsing Error: parameter count does not fit the rule parameters count\n" +
                                           "Parameters found: " + parameters.Length + ", Parameters needed: " +
                                           parameterNames.Length + "\n" +
                                           "Rule: '" + rule.key + "=" + rule.rValue +
                                           "'");
                            return false;
                        }

                        foreach (Expression expression in expressions)
                        {
                            for (int j = 0; j < parameters.Length; j++)
                            {
                                double parse;
                                if (Double.TryParse(parameters[j], out parse))
                                {
                                    if (expression.Parameters.ContainsKey(parameterNames[j]))
                                    {
                                        expression.Parameters[parameterNames[j]].Value = parse;
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Parsing Error: Could not parse value to double\n " +
                                                   "Rule: '" + rule.key + "=" +
                                                   rule.rValue + "'\n" +
                                                   "Parameter: '" + parameterNames[j] + "' with value '" +
                                                   parameters[j] + "'");
                                    return false;
                                }
                            }
                            expressionResults.Add(expression.Value);
                        }

                        Vector2Int[] ranges;
                        string rValue = rule.rValue;

                        if (!ParseTermRanges(rValue, out ranges))
                        {
                            Debug.LogError("Rule: '" + rule.key + "=" + rule.rValue +
                                           "'");
                            return false;
                        }

                        for (int j = ranges.Length - 1; j >= 0; j--)
                        {
                            rValue = rValue.Remove(ranges[j].x, ranges[j].y - ranges[j].x);
                            rValue = rValue.Insert(ranges[j].x, expressionResults[j].ToString());
                        }

                        sequence = sequence.Remove(i, lastClosingBracketIndex + 2);
                        sequence = sequence.Insert(i, rValue);
                        nextStep = rValue.Length;
                    }
                    else
                    {
                        Debug.LogError("Parsing Error: Empty rule list");
                    }
                }
                else
                {
                    int lastClosingBracketIndex;
                    if (!IndexOfLastClosingBracket(sequence.Substring(i), out lastClosingBracketIndex))
                    {
                        return false;
                    }

                    string innerBrackets = sequence.Substring(i, lastClosingBracketIndex).Substring(2);
                    innerBrackets = EvaluateParameterExpressions(innerBrackets);
                    sequence = sequence.Remove(i + 2, lastClosingBracketIndex - 2);
                    sequence = sequence.Insert(i + 2, innerBrackets);
                    nextStep = innerBrackets.Length + 3;
                }
            }
            else if (keyPairs.Count == 1)
            {

                if (keyPairs[0].Value.Count != 1)
                {
                    Debug.LogError("Parsing Error: Parsing Error: Multiple rules with same key and no conditions or parameters is not allowed");
                    return false;
                }

                KeyValuePair<string, List<Rule>> ruleKeyValuePair = keyPairs[0];
                string rValue = ruleKeyValuePair.Value[0].rValue;

                sequence = sequence.Remove(i, 1);
                sequence = sequence.Insert(i, rValue);
                nextStep = rValue.Length;
            }
            else if (keyPairs.Count > 1)
            {
                Debug.LogError("Parsing Error: Multiple rules with same key and no conditions or parameters is not allowed");
                return false;
            }
            else
            {
                nextStep = 1;
            }
        }

        result = sequence;
        return true;
    }

    private string EvaluateParameterExpressions(string parameterStr)
    {
        string result = "";
        string[] parameters = parameterStr.Split(',');

        for (int i = 0; i < parameters.Length; i++)
        {
            result += parser.Evaluate(parameters[i]).ToString() + ",";
        }

        result = result.Remove(result.Length - 1);

        return result;
    }

    public static bool IndexOfLastClosingBracket(string str, out int index)
    {
        int bracketCounter = 0;
        index = -1;

        for (int i = 0; i < str.Length; i++)
        {
            switch (str[i])
            {
                case '(':
                    bracketCounter++;
                    break;
                case ')':
                    bracketCounter--;
                    if (bracketCounter < 0)
                    {
                        Debug.LogError("Parsing Error: open bracket is missing");
                        return false;
                    } else if (bracketCounter == 0)
                    {
                        index = i;
                        return true;
                    }
                    break;
            }
        }
        Debug.LogError("Parsing Error: Could not find last closing bracket");
        return false;
    } 

    public static bool ParseTermRanges(string sequence, out Vector2Int[] ranges)
    {
        ranges = new Vector2Int[1];
        List<Vector2Int> rangesList = new List<Vector2Int>();
        Vector2Int currentRange = Vector2Int.left;
        int currentBracketDepth = 0;

        for (int j = 0; j < sequence.Length; j++)
        {
            if (sequence[j] == '(')
            {
                currentBracketDepth++;
                if (currentBracketDepth == 1)
                {
                    currentRange = new Vector2Int(j + 1, 0);
                }
            }
            else if (sequence[j] == ')')
            {
                currentBracketDepth--;
                if (currentBracketDepth == 0)
                {
                    currentRange.y = j;
                    rangesList.Add(currentRange);
                    currentRange = Vector2Int.left;
                }
            }
            else if (sequence[j] == ',')
            {
                if (currentBracketDepth == 1)
                {
                    currentRange.y = j;
                    rangesList.Add(currentRange);
                    currentRange = new Vector2Int(j + 1, 0);
                }
            }
        }

        if (currentBracketDepth != 0)
        {
            Debug.LogError(
                "Parsing Error: Brackets are not closed or count of open and close brackets are not even");
            return false;
        }

        ranges = rangesList.ToArray();
        return true;
    }

    public bool GenerateSequence(string sequence, out string result, int iterations)
    {
        ParseRules(ref rules);
        result = sequence;

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            if (!GenerateSequence(result, out result))
            {
                Debug.LogError("Parsing Error: Could not generate sequence.");
                return false;
            }
        }
        
        return true;
    }
}
