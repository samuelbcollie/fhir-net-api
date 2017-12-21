﻿using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification.Schema.Tags;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7.Fhir.Specification.Schema
{
    public abstract class Assertion
    {
        public static readonly Assertion Succeed = new Succeed();
        public static readonly Assertion Fail = new Fail();
        public static readonly Assertion Undecided = new Undecided();
        //TODO: Move Id here?
        /// <summary>
        /// Tags that this assertion would provide on success.
        /// </summary>
        /// <remarks>
        /// Is a list of SchemaTags, since the assertion (i.e. a slice) may provide multiple
        /// possible outcomes.
        /// </remarks>
        public abstract IEnumerable<SchemaTags> CollectTags();
    }

    /// <summary>
    /// Implemented by assertions that work on a single node (IElementNavigator)
    /// </summary>
    /// <remarks>
    /// Examples are fixed, binding, working on a single IElementNavigator.Value, and
    /// children, working on the children of a single IElementNavigator
    /// </remarks>
    public interface IMemberAssertion
    {
        SchemaTags Validate(IElementNavigator input, ValidationContext vc);
    }

    /// <summary>
    /// Implemented by assertions that work on groups of nodes
    /// </summary>
    /// <remarks>
    /// Examples are subgroups, ref, minItems, slice
    /// </remarks>
    public interface IGroupAssertion
    {
        SchemaTags Validate(IEnumerable<IElementNavigator> input, ValidationContext vc);
    }

    public class Schema : Assertion, IGroupAssertion
    {
        public readonly string Id;
        public readonly IEnumerable<Assertion> Assertions;

        public Schema(params Assertion[] assertions)
        {
            Assertions = assertions;
        }

        public Schema(IEnumerable<Assertion> assertions) => Assertions = assertions;

        public Schema(string id, params Assertion[] assertions) : this(assertions)
        {
            Id = id;
        }

        public Schema(string id, IEnumerable<Assertion> assertions) : this(assertions)
        {
            Id = id;
        }

        public override IEnumerable<SchemaTags> CollectTags()
            => Assertions
                .Aggregate(SchemaTags.Success.Collection, (sum, ass) => sum.Product(ass.CollectTags()));

        public SchemaTags Validate(IEnumerable<IElementNavigator> input, ValidationContext vc)
        {
            var multiAssertions = Assertions.OfType<IGroupAssertion>();
            var singleAssertions = Assertions.OfType<IMemberAssertion>();

            var multiResults = collect(multiAssertions
                .Select(assert => assert.Validate(input, vc)));

            var singleResults = collect(
                 from nav in input
                 from assert in singleAssertions
                 select assert.Validate(nav, vc));

            return multiResults + singleResults;

            SchemaTags collect(IEnumerable<SchemaTags> bunch) => bunch.Aggregate((sum, other) => sum += other);
        }
    }
}


/*****

  {   
    define: { identified subschemas which are not evaluated }

    ref: "external reference" 

    {
       // nested schema
    }

	minItems:
	maxItems:

    group { membership condition } : { constraints for this group }

    slice 
    {
        ordered: true/false

		// one or more groups defined by subschemas
		group { membership condition } : { constraints for this group }
		default: { constraints for this group } === group {} : {    }    // no default: {} : { fail }
    }

    children
    {
		"asdfdfs" : { constraints for this name }
		"asdfdfs" : { constraints for this name }
    }

	success { a tag block to execute }
	fail { a tag block to execute }
	undecided { a tag block to execute }
}

****/
