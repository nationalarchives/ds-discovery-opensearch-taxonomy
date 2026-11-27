namespace NationalArchives.Taxonomy.Common.Mappers
{
    public interface IMapper<TSource,TTarget>
    {
        TTarget MapFrom(TSource input);
    }
}
