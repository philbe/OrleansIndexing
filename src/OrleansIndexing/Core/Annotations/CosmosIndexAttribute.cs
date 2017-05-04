using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// The attribute for declaring the property fields of an
    /// indexed grain interface to have a "Cosmos Index", which
    /// is also known as "Initialized Index".
    /// 
    /// A "Cosmos Index" indexes all the grains that have been
    /// created during the lifetime of the application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CosmosIndexAttribute : IndexAttribute
    {
        /// <summary>
        /// The default constructor for CosmosIndex.
        /// </summary>
        public CosmosIndexAttribute() : this(false)
        {
        }

        /// <summary>
        /// The constructor for CosmosIndex.
        /// </summary>
        /// <param name="IsEager">Determines whether the index should be
        /// updated eagerly upon any change in the indexed grains. Otherwise,
        /// the update propagation happens lazily after applying the update
        /// to the grain itself.</param>
        public CosmosIndexAttribute(bool IsEager) : this(Indexing.IndexType.HashIndexSingleBucket, IsEager, false)
        {
        }

        /// <summary>
        /// The full-option constructor for CosmosIndex.
        /// </summary>
        /// <param name="type">The index type for the cosmos index</param>
        /// <param name="IsEager">Determines whether the index should be
        /// updated eagerly upon any change in the indexed grains. Otherwise,
        /// the update propagation happens lazily after applying the update
        /// to the grain itself.</param>
        /// <param name="IsUnique">Determines whether the index should maintain
        /// a uniqueness constraint.</param>
        /// <param name="MaxEntriesPerBucket">The maximum number of entries
        /// that should be stored in each bucket of a distributed index. This
        /// option is only considered if the index is a distributed index.
        /// Use -1 to declare no limit.</param>
        public CosmosIndexAttribute(IndexType type, bool IsEager = false, bool IsUnique = false, int MaxEntriesPerBucket = -1)
        {
            switch (type)
            {
                case Indexing.IndexType.HashIndexSingleBucket:
                    IndexType = typeof(CosmosHashIndexSingleBucket<,>);
                    break;
                case Indexing.IndexType.HashIndexPartitionedByKeyHash:
                    IndexType = typeof(CosmosHashIndexPartitionedPerKey<,>);
                    break;
                //Cosmos indexes partitioned by silo are not supported
                case Indexing.IndexType.HashIndexPartitionedBySilo:
                    throw new Exception("PartitionedBySilo indexes are not supported for Cosmos Indexes.");
                default:
                    IndexType = typeof(CosmosHashIndexSingleBucket<,>);
                    break;
            }
            this.IsEager = IsEager;
            this.IsUnique = IsUnique;
            this.MaxEntriesPerBucket = MaxEntriesPerBucket;
        }
    }
}
