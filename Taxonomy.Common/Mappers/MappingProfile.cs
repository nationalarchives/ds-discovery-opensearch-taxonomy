using AutoMapper;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using NationalArchives.Taxonomy.Common.DataObjects.Mongo;
using NationalArchives.Taxonomy.Common.Domain;

namespace NationalArchives.Taxonomy.Common.Mappers
{
    class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<OpenSearchRecordAssetView, InformationAssetView>()
                .ForMember(dest => dest.CoveringDates, opt => opt.MapFrom(source => source.COVERING_DATES))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(source => source.DESCRIPTION))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(source => source.TITLE))
                //.ForMember(dest => dest.Score, opt => opt.MapFrom(source => source.Score))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(source => source.SOURCE.ToString()))
                .ForMember(dest => dest.CoveringDates, opt => opt.MapFrom(source => source.COVERING_DATES))
                .ForMember(dest => dest.CatDocRef, opt => opt.MapFrom(source => source.CATALOGUE_REFERENCE))
                .ForMember(dest => dest.CorpBodys, opt => opt.MapFrom(source => source.CORPORATE_BODY))
                .ForMember(dest => dest.Person_FullName, opt => opt.MapFrom(source => source.PERSON_FULL_NAME))
                .ForMember(dest => dest.Place_Name, opt => opt.MapFrom(source => source.PLACE_NAME))
                .ForMember(dest => dest.Series, opt => opt.MapFrom(source => source.SERIES_CODE))
                .ForMember(dest => dest.Subjects, opt => opt.MapFrom(source => source.SUBJECT))
                .ForMember(dest => dest.DocReference, opt => opt.MapFrom(source => source.ID))
                .ForMember(dest => dest.ContextDescription, opt => opt.MapFrom(source => source.CONTEXT)).IncludeAllDerived()
                .Include<OpenSearchRecordAssetView, InformationAssetViewWithScore>().ReverseMap();

            CreateMap<OpenSearchRecordAssetView, InformationAssetViewWithScore>()
                .ForMember(dest => dest.Score, opt => opt.MapFrom(source => source.Score)).ReverseMap();

            CreateMap<CategoryFromOpenSearch, Category>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(source => source.ID))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(source => source.title))
                .ForMember(dest => dest.Query, opt => opt.MapFrom(source => source.query_text))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(source => source.SC))
                .ForMember(dest => dest.Lock, opt => opt.MapFrom(source => source.locked)).ReverseMap();

            CreateMap<CategoryFromMongo, Category>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(source => source.CIAID))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(source => source.Title))
                .ForMember(dest => dest.Query, opt => opt.MapFrom(source => source.QueryText))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(source => source.SC))
                .ReverseMap();
        }
    }
}
                                      