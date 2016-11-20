using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace MCEBuddy.Util
{
    public static class MathLib
    {
        /// <summary>
        /// Returns the closest number in the list based on the number passed
        /// </summary>
        /// <param name="list">List of numbers to check against</param>
        /// <param name="number">Number to compare</param>
        /// <returns>Closest number in list</returns>
        public static double GetClosestNumber(List<double> list, double number)
        {
                double closest = list.Aggregate((x, y) => Math.Abs(x - number) < Math.Abs(y - number) ? x : y);

                return closest;
        }

        /// <summary>
        /// Returns the closest number in the list based on the number passed
        /// </summary>
        /// <param name="list">List of numbers in string format to check against</param>
        /// <param name="number">Number to compare</param>
        /// <returns>Closest number in list</returns>
        public static string GetClosestNumber(List<string> list, double number)
        {
                string closest = list.Aggregate((x, y) => Math.Abs(double.Parse(x) - number) < Math.Abs(double.Parse(y) - number) ? x : y);

                return closest;
        }

        /// <summary>
        /// Rounds off to the nearest multiple (towards zero if midway through)
        /// </summary>
        /// <param name="number">Number to round off</param>
        /// <param name="toNearest">Nearest multiple to round off to</param>
        /// <returns></returns>
        public static int RoundOff(int number, int toNearest)
        {
            return ((int)((double)number / (double)toNearest)) * toNearest;
        }

        /// <summary>
        /// Evaluates a basic mathematical expression such as (28+4)*2 or 30000/1001 and returns the result
        /// Returns null if there is an error evaluating the expression
        /// </summary>
        /// <param name="expression">Expression to evaluate</param>
        /// <returns>Results</returns>
        public static double? EvaluateBasicExpression(string expression)
        {
            DataTable table = new DataTable();
            table.Columns.Add("expression", typeof(string), expression);
            DataRow row = table.NewRow();
            table.Rows.Add(row);
            try
            {
                return double.Parse((string)row["expression"]);
            }
            catch
            {
                return null;
            }
        }
    }
}
