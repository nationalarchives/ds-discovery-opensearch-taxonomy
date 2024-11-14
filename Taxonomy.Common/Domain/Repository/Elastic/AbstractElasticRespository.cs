using AutoMapper;
using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public class AbstractElasticRespository<T> where T : class
    {
        protected readonly IConnectElastic<T> _elasticConnection;
        protected readonly IMapper _mapper;
        public AbstractElasticRespository(IConnectElastic<T> elasticConnection, IMapper mapper)
        {
            _elasticConnection = elasticConnection;
            _mapper = mapper;
        }
    }
}
