namespace VRBuilder.Core.EntityOwners
{
    /// <summary>
    /// Sequence data which allow specifying the next element in the sequence.
    /// </summary>
    public interface IEntityNonLinearSequenceDataWithMode<TEntity> : IEntitySequenceDataWithMode<TEntity> where TEntity : IEntity
    {
        /// <summary>
        /// If not null, overrides the next entity in the sequence.
        /// </summary>
        TEntity OverrideNext { get; set; }
    }
}
