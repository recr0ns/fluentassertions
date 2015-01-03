using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using FluentAssertions.Common;

namespace FluentAssertions.Equivalency
{
    /// <summary>
    /// Represents the run-time behavior of a structural equivalency assertion.
    /// </summary>
    public abstract class EquivalencyAssertionOptionsBase<TSelf> : IEquivalencyAssertionOptions
        where TSelf : EquivalencyAssertionOptionsBase<TSelf>
    {
        #region Private Definitions

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<IMemberSelectionRule> selectionRules = new List<IMemberSelectionRule>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<IMemberMatchingRule> matchingRules = new List<IMemberMatchingRule>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<IEquivalencyStep> userEquivalencySteps = new List<IEquivalencyStep>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CyclicReferenceHandling cyclicReferenceHandling = CyclicReferenceHandling.ThrowException;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected readonly OrderingRuleCollection orderingRules = new OrderingRuleCollection();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isRecursive;

        private bool allowInfiniteRecursion;

        private EnumEquivalencyHandling enumEquivalencyHandling;

        private bool useRuntimeTyping;

        private bool includeProperties;

        #endregion

        internal EquivalencyAssertionOptionsBase()
        {
            AddMatchingRule(new MustMatchByNameRule());
            
            orderingRules.Add(new ByteArrayOrderingRule());
        }

        /// <summary>
        /// Creates an instance of the equivalency assertions options based on defaults previously configured by the caller.
        /// </summary>
        protected EquivalencyAssertionOptionsBase(IEquivalencyAssertionOptions defaults)
        {
            allowInfiniteRecursion = defaults.AllowInfiniteRecursion;
            isRecursive = defaults.IsRecursive;
            cyclicReferenceHandling = defaults.CyclicReferenceHandling;
            allowInfiniteRecursion = defaults.AllowInfiniteRecursion;
            enumEquivalencyHandling = defaults.EnumEquivalencyHandling;
            useRuntimeTyping = defaults.UseRuntimeTyping;
            includeProperties = defaults.IncludeProperties;

            selectionRules.AddRange(defaults.SelectionRules);
            userEquivalencySteps.AddRange(defaults.UserEquivalencySteps);
            matchingRules.AddRange(defaults.MatchingRules);
            orderingRules = new OrderingRuleCollection(defaults.OrderingRules);
        }

        /// <summary>
        /// Gets an ordered collection of selection rules that define what members are included.
        /// </summary>
        IEnumerable<IMemberSelectionRule> IEquivalencyAssertionOptions.SelectionRules
        {
            get { return selectionRules; }
        }

        /// <summary>
        /// Gets an ordered collection of matching rules that determine which subject members are matched with which
        /// expectation members.
        /// </summary>
        IEnumerable<IMemberMatchingRule> IEquivalencyAssertionOptions.MatchingRules
        {
            get { return matchingRules; }
        }

        /// <summary>
        /// Gets an ordered collection of Equivalency steps how a subject is comparted with the expectation.
        /// </summary>
        IEnumerable<IEquivalencyStep> IEquivalencyAssertionOptions.UserEquivalencySteps
        {
            get { return userEquivalencySteps; }
        }

        /// <summary>
        /// Gets an ordered collection of rules that determine whether or not the order of collections is important. By default,
        /// ordering is irrelevant.
        /// </summary>
        OrderingRuleCollection IEquivalencyAssertionOptions.OrderingRules
        {
            get { return orderingRules; }
        }

        /// <summary>
        /// Gets value indicating whether the equality check will include nested collections and complex types.
        /// </summary>
        bool IEquivalencyAssertionOptions.IsRecursive
        {
            get { return isRecursive; }
        }

        bool IEquivalencyAssertionOptions.AllowInfiniteRecursion
        {
            get { return allowInfiniteRecursion; }
        }

        /// <summary>
        /// Gets value indicating how cyclic references should be handled. By default, it will throw an exception.
        /// </summary>
        CyclicReferenceHandling IEquivalencyAssertionOptions.CyclicReferenceHandling
        {
            get { return cyclicReferenceHandling; }
        }

        EnumEquivalencyHandling IEquivalencyAssertionOptions.EnumEquivalencyHandling
        {
            get { return enumEquivalencyHandling; }
        }

        bool IEquivalencyAssertionOptions.UseRuntimeTyping
        {
            get { return useRuntimeTyping; }
        }

        bool IEquivalencyAssertionOptions.IncludeProperties
        {
            get { return includeProperties; }
        }

        /// <summary>
        /// Causes inclusion of only public properties of the subject as far as they are defined on the declared type. 
        /// </summary>
        public TSelf IncludingAllDeclaredProperties()
        {
            RespectDeclaredType();

            includeProperties = true;

            ReconfigureSelectionRules();

            return (TSelf)this;
        }

        /// <summary>
        ///  Causes inclusion of only public properties of the subject based on its run-time type rather than its declared type.
        /// </summary>
        public TSelf IncludingAllRuntimeProperties()
        {
            RespectRuntimeType();

            includeProperties = true;

            ReconfigureSelectionRules();

            return (TSelf)this;
        }

        private void RespectRuntimeType()
        {
            useRuntimeTyping = true;
        }

        private void RespectDeclaredType()
        {
            useRuntimeTyping = false;
        }

        /// <summary>
        /// Excludes a (nested) property based on a predicate from the structural equality check.
        /// </summary>
        public TSelf Excluding(Expression<Func<ISubjectInfo, bool>> predicate)
        {
            AddSelectionRule(new ExcludeMemberByPredicateSelectionRule(predicate));
            return (TSelf)this;
        }

        /// <summary>
        /// Tries to match the members of the subject with equally named members on the expectation. Ignores those 
        /// members that don't exist on the expectation and previously registered matching rules.
        /// </summary>
        public TSelf ExcludingMissingMembers()
        {
            ClearMatchingRules();
            matchingRules.Add(new TryMatchByNameRule());
            return (TSelf)this;
        }

        /// <summary>
        /// Tries to match the properties of the subject with equally named properties on the expectation. Ignores those 
        /// properties that don't exist on the expectation and previously registered matching rules.
        /// </summary>
        [Obsolete]
        public TSelf ExcludingMissingProperties()
        {
            return ExcludingMissingMembers();
        }

        /// <summary>
        /// Requires the expectation to have members which are equally named to members on the subject.
        /// </summary>
        /// <returns></returns>
        public TSelf ThrowingOnMissingMembers()
        {
            ClearMatchingRules();
            matchingRules.Add(new MustMatchByNameRule());
            return (TSelf)this;
        }
        /// <summary>
        /// Requires the expectation to have properties which are equally named to properties on the subject.
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public TSelf ThrowingOnMissingProperties()
        {
            return ThrowingOnMissingMembers();
        }

        /// <param name="action">
        /// The assertion to execute when the predicate is met.
        /// </param>
        public Restriction<TProperty> Using<TProperty>(Action<IAssertionContext<TProperty>> action)
        {
            return new Restriction<TProperty>((TSelf)this, action);
        }

        /// <summary>
        /// Causes the structural equality check to include nested collections and complex types.
        /// </summary>
        public TSelf IncludingNestedObjects()
        {
            isRecursive = true;
            return (TSelf)this;
        }

        /// <summary>
        /// Causes the structural equality check to exclude nested collections and complex types.
        /// </summary>
        /// <remarks>
        /// Behaves similarly to the old property assertions API.
        /// </remarks>
        public TSelf ExcludingNestedObjects()
        {
            isRecursive = false;
            return (TSelf)this;
        }

        /// <summary>
        /// Causes the structural equality check to ignore any cyclic references.
        /// </summary>
        /// <remarks>
        /// By default, cyclic references within the object graph will cause an exception to be thrown.
        /// </remarks>
        public TSelf IgnoringCyclicReferences()
        {
            cyclicReferenceHandling = CyclicReferenceHandling.Ignore;
            return (TSelf)this;
        }


        /// <summary>
        /// Disables limitations on recursion depth when the structural equality check is configured to include nested objects
        /// </summary>
        public TSelf AllowingInfiniteRecursion()
        {
            allowInfiniteRecursion = true;
            return (TSelf)this;
        }

        /// <summary>
        /// Clears all selection rules, including those that were added by default.
        /// </summary>
        public void WithoutSelectionRules()
        {
            ClearSelectionRules();
        }

        /// <summary>
        /// Clears all matching rules, including those that were added by default.
        /// </summary>
        public void WithoutMatchingRules()
        {
            ClearMatchingRules();
        }

        /// <summary>
        /// Adds a selection rule to the ones already added by default, and which is evaluated after all existing rules.
        /// </summary>
        public TSelf Using(IMemberSelectionRule selectionRule)
        {
            return AddSelectionRule(selectionRule);
        }

        /// <summary>
        /// Adds a matching rule to the ones already added by default, and which is evaluated before all existing rules.
        /// </summary>
        public TSelf Using(IMemberMatchingRule matchingRule)
        {
            return AddMatchingRule(matchingRule);
        }

        /// <summary>
        /// Adds a selection rule to the ones already added by default, and which is evaluated after all existing rules.
        /// </summary>
        [Obsolete]
        public TSelf Using(ISelectionRule selectionRule)
        {
            return AddSelectionRule(new ObsoleteSelectionRuleAdapter(selectionRule));
        }

        /// <summary>
        /// Adds a matching rule to the ones already added by default, and which is evaluated before all existing rules.
        /// </summary>
        [Obsolete]
        public TSelf Using(IMatchingRule matchingRule)
        {
            return AddMatchingRule(new ObsoleteMatchingRuleAdapter(matchingRule));
        }

        /// <summary>
        /// Adds a matching rule to the ones already added by default, and which is evaluated before all existing rules.
        /// NOTE: These matching rules do not apply to the root object.
        /// </summary>
        public TSelf Using(IAssertionRule assertionRule)
        {
            return AddAssertionRule(assertionRule);
        }

        /// <summary>
        /// Adds a matching rule to the ones already added by default, and which is evaluated before all existing rules.
        /// </summary>
        public TSelf Using(IEquivalencyStep equivalencyStep)
        {
            return AddEquivalencyStep(equivalencyStep);
        }

        /// <summary>
        /// Causes all collections to be compared in the order in which the items appear in the expectation.
        /// </summary>
        public TSelf WithStrictOrdering()
        {
            orderingRules.Add(new MatchAllOrderingRule());
            return (TSelf)this;
        }

        /// <summary>
        /// Causes the collection identified by the provided <paramref name="predicate"/> to be compared in the order 
        /// in which the items appear in the expectation.
        /// </summary>
        public TSelf WithStrictOrderingFor(Expression<Func<ISubjectInfo, bool>> predicate)
        {
            orderingRules.Add(new PredicateBasedOrderingRule(predicate));
            return (TSelf)this;
        }

        /// <summary>
        /// Causes to compare Enum properties using the result of their ToString method.
        /// </summary>
        /// <remarks>
        /// By default, enums are compared by value.
        /// </remarks>
        public TSelf ComparingEnumsByName()
        {
            enumEquivalencyHandling = EnumEquivalencyHandling.ByName;
            return (TSelf) this;
        }

        /// <summary>
        /// Causes to compare Enum members using their underlying value only.
        /// </summary>
        /// <remarks>
        /// This is the default.
        /// </remarks>
        public TSelf ComparingEnumsByValue()
        {
            enumEquivalencyHandling = EnumEquivalencyHandling.ByValue;
            return (TSelf) this;
        }

        #region Non-fluent API

        protected void RemoveSelectionRule<T>() where T : IMemberSelectionRule
        {
            foreach (T selectionRule in selectionRules.OfType<T>().ToArray())
            {
                selectionRules.Remove(selectionRule);
            }
        }

        protected void RemoveStandardSelectionRules()
        {
            RemoveSelectionRule<AllPublicPropertiesSelectionRule>();

            RespectDeclaredType();
            includeProperties = true;
        }

        private void ClearSelectionRules()
        {
            selectionRules.Clear();

            RespectDeclaredType();
            includeProperties = true;
        }

        private void ClearMatchingRules()
        {
            matchingRules.Clear();
        }

        protected TSelf AddSelectionRule(IMemberSelectionRule selectionRule)
        {
            selectionRules.Add(selectionRule);
            return (TSelf) this;
        }

        private TSelf AddMatchingRule(IMemberMatchingRule matchingRule)
        {
            matchingRules.Insert(0, matchingRule);
            return (TSelf) this;
        }

        private TSelf AddAssertionRule(IAssertionRule assertionRule)
        {
            AddEquivalencyStep(new AssertionRuleEquivalencyStepAdaptor(assertionRule));
            return (TSelf) this;
        }

        private TSelf AddEquivalencyStep(IEquivalencyStep equivalencyStep)
        {
            userEquivalencySteps.Insert(0, equivalencyStep);
            return (TSelf) this;
        }

        private void ReconfigureSelectionRules()
        {
            selectionRules.Clear();

            selectionRules.AddRange(CreateSelectionRules());
        }

        private IEnumerable<IMemberSelectionRule> CreateSelectionRules()
        {
            if (includeProperties)
            {
                yield return new AllPublicPropertiesSelectionRule();
            }
        }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("- Use {0} types and members", useRuntimeTyping ? "runtime" : "declared"));

            foreach (var rule in selectionRules)
            {
                builder.AppendLine("- " + rule);
            }

            foreach (var rule in matchingRules)
            {
                builder.AppendLine("- " + rule);
            }

            foreach (var step in userEquivalencySteps)
            {
                builder.AppendLine("- " + step);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Defines additional overrides when used with <see cref="EquivalencyAssertionOptions.When"/>
        /// </summary>
        public class Restriction<TMember>
        {
            private readonly Action<IAssertionContext<TMember>> action;
            private readonly TSelf options;

            public Restriction(TSelf options, Action<IAssertionContext<TMember>> action)
            {
                this.options = options;
                this.action = action;
            }

            /// <summary>
            /// Allows overriding the way structural equality is applied to (nested) objects of tyoe <typeparamref name="TMemberType"/>
            /// </summary>
            public TSelf WhenTypeIs<TMemberType>()
            {
                When(info => info.RuntimeType.IsSameOrInherits(typeof(TMemberType)));
                return options;
            }

            /// <summary>
            /// Allows overriding the way structural equality is applied to particular members.
            /// </summary>
            /// <param name="predicate">
            /// A predicate based on the <see cref="ISubjectInfo"/> of the subject that is used to identify the property for which the
            /// override applies.
            /// </param>
            public TSelf When(Expression<Func<ISubjectInfo, bool>> predicate)
            {
                options.Using(new AssertionRule<TMember>(predicate, action));
                return options;
            }
        }
    }
}