// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "DAX")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "DAX",
        Scope = "namespace", Target = "LinqToDAX.Query")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "DAX",
        Scope = "namespace", Target = "LinqToDAX")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "expressiontype",
        Scope = "member",
        Target = "LinqToDAX.Query.DaxExpression.#.ctor(System.Linq.Expressions.ExpressionType,System.Type)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "LinqToDAX.Query.TabularQueryExtensions.#Generate`3(System.Linq.IQueryable`1<!!1>,System.Linq.IQueryable`1<!!2>,System.Linq.Expressions.Expression`1<System.Func`3<!!1,!!2,!!0>>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "LinqToDAX.Query.TabularQueryExtensions.#Generate`3(System.Linq.IQueryable`1<!!1>,System.Linq.IQueryable`1<!!2>,System.Linq.Expressions.Expression`1<System.Func`3<!!1,!!2,!!0>>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "column", Scope = "member", Target = "LinqToDAX.Query.TabularQueryExtensions.#Average`1(!!0)")]
