using System;
using System.CodeDom;
using System.Collections.Generic;

namespace IronAHK.Scripting
{
    partial class Parser
    {
        const string raw = "raw";

        bool IsOptimisableExpression(CodeExpression expr)
        {
            if (!(expr is CodeMethodInvokeExpression))
                return false;

            var invoke = (CodeMethodInvokeExpression)expr;
            return invoke.Method.MethodName == InternalMethods.Operate.MethodName && invoke.Parameters.Count == 3;
        }

        CodeExpression OptimiseExpression(CodeExpression expr)
        {
            if (!IsOptimisableExpression(expr))
                return expr;

            var invoke = (CodeMethodInvokeExpression)expr;

            for (int i = 1; i < 3; i++)
                invoke.Parameters[i] = OptimiseExpression(invoke.Parameters[i]);

            if (invoke.Parameters[1] is CodePrimitiveExpression && invoke.Parameters[2] is CodePrimitiveExpression)
            {
                object result = null;

                try
                {
                    result = Script.Operate((Script.Operator)invoke.Parameters[0].UserData[raw],
                        ((CodePrimitiveExpression)invoke.Parameters[1]).Value,
                        ((CodePrimitiveExpression)invoke.Parameters[2]).Value);

                    if (result is double)
                        result = (decimal)(double)result;
                    else if (result is int)
                        result = (decimal)(int)result;
                }
                catch (Exception) { }

                return new CodePrimitiveExpression(result);
            }

            return invoke;
        }

        CodeExpression OptimiseLoneExpression(CodeExpression expr)
        {
            if (expr is CodeBinaryOperatorExpression && ((CodeBinaryOperatorExpression)expr).Operator == CodeBinaryOperatorType.Assign)
            {
                var assign = (CodeBinaryOperatorExpression)expr;
                assign.Right = OptimiseExpression(assign.Right);
                return assign;
            }

            if (!IsOptimisableExpression(expr))
                return expr;

            var invoke = (CodeMethodInvokeExpression)expr;

            for (int i = 1; i < 3; i++)
                invoke.Parameters[i] = OptimiseExpression(invoke.Parameters[i]);

            bool left = invoke.Parameters[1] is CodePrimitiveExpression, right = invoke.Parameters[2] is CodeExpression;

            if (!left && !right)
                return null;

            if (left)
                return invoke.Parameters[2];

            if (right)
                return invoke.Parameters[1];

            return expr;
        }
    }
}