using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicEquationParser {

    // Only for &, =, <=, >=, <, >
    public static bool EvaluateEquation(string equation, Parameter[] parameters, ExpressionParser parser)
    {
        string[] equationParts = equation.Split('&');

        for (int i = 0; i < equationParts.Length; i++)
        {
            string[] operands;
            string operation;

            if (equationParts[i].Contains("="))
            {
                if (equationParts[i].Contains("<"))
                {
                    operation = "<=";
                }
                else if (equationParts[i].Contains(">"))
                {
                    operation = ">=";
                }
                else
                {
                    operation = "=";

                }
            } else if (equationParts[i].Contains("<"))
            {
                operation = "<";

            } else if (equationParts[i].Contains(">"))
            {
                operation = ">";
            }
            else
            {
                Debug.LogError("Evaluate Equation Error: Not invalid equation");
                return false;
            }

            operands = equationParts[i].Split(new string[] { operation }, StringSplitOptions.None);
            Expression[] expressions = new Expression[operands.Length];

            for (int j = 0; j < operands.Length; j++)
            {
                expressions[j] = parser.EvaluateExpression(operands[j]);
                for (int k = 0; k < parameters.Length; k++)
                {
                    if (expressions[j].Parameters.ContainsKey(parameters[k].Name))
                    {
                        expressions[j].Parameters[parameters[k].Name].Value = parameters[k].Value;
                    }
                }
            }

            switch (operation)
            {
                case "=":
                    return expressions[0].Value == expressions[1].Value;
                case ">":
                    return expressions[0].Value > expressions[1].Value;
                case "<":
                    return expressions[0].Value < expressions[1].Value;
                case ">=":
                    return expressions[0].Value >= expressions[1].Value;
                case "<=":
                    return expressions[0].Value <= expressions[1].Value;
            }
        }
        return false;
    }
}
