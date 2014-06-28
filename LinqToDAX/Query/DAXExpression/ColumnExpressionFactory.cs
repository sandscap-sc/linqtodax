// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColumnExpressionFactory.cs" company="Dealogic">
//   see LICENCE.md
// </copyright>
// <summary>
//   Factory that creates Column expressions
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace LinqToDAX.Query.DAXExpression
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Factory that creates Column expressions
    /// </summary>
    internal class ColumnExpressionFactory
    {
        /// <summary>
        /// Counter that keeps track of measures created, 
        /// useful to ensure that measure names are actually unique,
        ///  by appending the counter value to the names
        /// </summary>
        private static int _measureCounter;

        /// <summary>
        /// Reference to parent binder object
        /// </summary>
        private readonly TabularQueryBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnExpressionFactory"/> class. 
        /// </summary>
        /// <param name="binder">parent binder object</param>
        public ColumnExpressionFactory(TabularQueryBinder binder)
        {
            _binder = binder;
        }

        /// <summary>
        /// Determine the runtime type of the member and hence the column to be created
        /// </summary>
        /// <param name="member">reflected member that is to be a column</param>
        /// <returns>runtime type</returns>
        internal static Type GetColumnType(MemberInfo member)
        {
            var pi = member as PropertyInfo;
            if (pi != null)
            {
                return pi.PropertyType;
            }

            // TODO: default type is string, but maybe we should throw here 
            return typeof(string);
        }

        /// <summary>
        /// Creates a column expression from a member expression
        /// </summary>
        /// <param name="expression">member expression</param>
        /// <returns>column expression</returns>
        internal Expression CreateColumnExpression(MemberExpression expression)
        {
            if (expression.Expression.NodeType == ExpressionType.Constant)
            {
                return expression;
            }

            var mapping =
                expression.Member.GetCustomAttribute(typeof(TabularMappingAttribute)) as TabularMappingAttribute;
            if (mapping == null)
            {
                throw new TabularException("Invalid lookup arguments");
            }
           
            string columnName = expression.Member.Name;
            Type columnType = GetColumnType(expression.Member);
            string dbname = mapping.ColumnName;
            string table = mapping.TableName;
            return new ColumnExpression(columnType, columnName, dbname, table);
        }

        /// <summary>
        /// Creates a measure expression
        /// </summary>
        /// <param name="node">method call node</param>
        /// <returns>a measure expression corresponding to the node</returns>
        internal Expression CreateMeasureExpression(MethodCallExpression node)
        {
            _measureCounter++;
            Expression c = _binder.Visit(node.Arguments[0]);
            var col = (ColumnExpression)c;
            var table = col.TableName;

            // TODO: Add a level of indirection here as well see XAggregations
            string dbname = node.Method.Name.ToUpper() + "(" + col.DbName + ")";
           
            string name = string.Format("[{0}Of{1}{2}]", node.Method.Name, col.Name, _measureCounter);
            if (node.Arguments.Count() == 1)
            {
                return new MeasureExpression(
                    node.Method.ReturnType,
                    name,
                    dbname,
                    name,
                    table);
            }

            Expression filter;
            switch (node.Arguments[1].NodeType)
            {
                case ExpressionType.Parameter:
                    filter = TableFactory.GetTableExpression(node.Arguments[1]);
                    break;
                default:
                    filter = _binder.Visit(node.Arguments[1]);
                    break;
            }

            return new MeasureExpression(node.Method.ReturnType, (ExpressionType)DaxExpressionType.Measure,  name, dbname, name, table, filter);
        }

        /// <summary>
        /// Creates an aggregation like SUMX
        /// </summary>
        /// <param name="aggregationType">aggregation type, that describes the actual DAX function </param>
        /// <param name="node">method call node</param>
        /// <returns>aggregation expression</returns>
        internal Expression CreateXAggregation(AggregationType aggregationType, MethodCallExpression node)
        {
            _measureCounter++;
            var table = (ProjectionExpression)_binder.Visit(node.Arguments[0]);
            var lambda = (LambdaExpression)node.Arguments[1];
            var init = lambda.Body as MemberExpression;
            if (init == null)
            {
                throw new TabularException("Aggregation argument should refer to a member");
            }

            MemberInfo member = init.Member;
            string tableName = string.Empty;

            var finder = new Finder<ConstantExpression>();
            finder.Visit(node);
            if (finder.Found)
            {
                var t = finder.First.Value as IQueryable;
                if(t != null)
                {
                    var tableArgument = t.ElementType;
                    var att = tableArgument.GetCustomAttribute<TabularTableMappingAttribute>();
                    tableName = att.TableName;
                }
            }
           
            var projector = table.Projector as NewExpression;
            var column = (ColumnExpression)FindColumnExpression(projector, member);
            var type = column.Type;
            if (column.Type.IsValueType && !column.Type.IsGenericType)
            {
                try
                {
                    type = typeof(Nullable<>).MakeGenericType(new[] { column.Type });
                }
                catch (ArgumentException)
                {
                    // nothing to do here 
                }
            }

            if (aggregationType == AggregationType.Count || aggregationType == AggregationType.Rank || aggregationType == AggregationType.ReverseRank)
            {
                type = typeof(long?);
            }

            return new XAggregationExpression(aggregationType, table, column, "[" + aggregationType + _measureCounter + "]", tableName, type);
        }

        /// <summary>
        /// Helper function to identify column expressions in a projector corresponding to a member 
        /// </summary>
        /// <param name="projector">projection expression where we look for a particular member</param>
        /// <param name="member">reflected member information</param>
        /// <returns>Column expression for the member</returns>
        internal Expression FindColumnExpression(Expression projector, MemberInfo member)
        {
            switch (projector.NodeType)
            {
                case ExpressionType.New:
                    var newExpression = projector as NewExpression;
                    if (newExpression == null)
                    {
                        throw new TabularException("Expected a new expression from Tabular query");
                    }

                    int index = newExpression.Members.IndexOf(member);
                    if (index < 0)
                    {
                        throw new TabularException("Member should have refered to a column");
                    }

                    var column = newExpression.Arguments[index] as ColumnExpression;
                    if (column == null)
                    {
                        throw new TabularException("Member should have refered to a column");
                    }

                    return column;
                case ExpressionType.MemberInit:
                    var initExpression = projector as MemberInitExpression;
                    if (initExpression == null)
                    {
                        throw new TabularException("Expected a initialisation expression from Tabular query");
                    }

                    MemberBinding candidate = initExpression.Bindings.FirstOrDefault(m => m.Member.Name == member.Name);
                    if (candidate != null)
                    {
                        switch (candidate.BindingType)
                        {
                            case MemberBindingType.Assignment:
                                return ((MemberAssignment)candidate).Expression;
                            case MemberBindingType.ListBinding:
                                throw new NotImplementedException("List initialisation is not supported yet");
                            case MemberBindingType.MemberBinding:
                                throw new NotImplementedException("Recursive initialisation is not supported yet");
                        }
                    }

                    break;
            }

            return projector;
        }

        /// <summary>
        /// Creates a LOOKUP call in DAX
        /// </summary>
        /// <param name="node">method call corresponding to lookup</param>
        /// <returns>Lookup expression</returns>
        internal Expression CreateLookupExpression(MethodCallExpression node)
        {
            Type t = node.Method.ReturnType;
            ColumnExpression target =
                CreateColumnExpression((MemberExpression)node.Arguments[0]) as ColumnExpression;
            var dict = (NewArrayExpression)node.Arguments[1];
            
            // the arguments count should be even in order to be able to create a lookup in them
            if (dict.Expressions.Count < 2 || dict.Expressions.Count() % 2 != 0)
            {
                throw new TabularException("Invalid number of lookup Arguments, should be even number");
            }

            var lookupList = new List<Tuple<ColumnExpression, ColumnExpression>>();
            for (int counter = 0; counter < dict.Expressions.Count; counter += 2)
            {
                ColumnExpression c1 =
                    CreateColumnExpression((MemberExpression)dict.Expressions[counter]) as ColumnExpression;
                ColumnExpression c2 =
                    CreateColumnExpression((MemberExpression)dict.Expressions[counter + 1]) as ColumnExpression;
                if (c1 != null && c2 != null)
                {
                    lookupList.Add(Tuple.Create(c1, c2));
                }
            }
            if (target != null)
            {
                // TODO: Might need more indiretion here
                string dbstr = "LOOKUPVALUE(" + target.DbName + "," +
                               lookupList.Select(pair => pair.Item1.DbName + "," + pair.Item2.DbName)
                                   .Aggregate((x, y) => x + "," + y) + ")";

                return new LookupExpression(t, target, lookupList, dbstr);
            }
            throw new TabularException("Invalid lookup arguments");
        }
    }
}