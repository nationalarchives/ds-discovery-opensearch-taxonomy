using Amazon;
using Amazon.Runtime;
using Newtonsoft.Json;
using OpenSearch.Client;
using OpenSearch.Client.JsonNetSerializer;
using OpenSearch.Net;
using OpenSearch.Net.Auth.AwsSigV4;
using System;
using System.Configuration;
using System.Linq;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    internal static class ConnectionSettingsProvider
    {
        private const string DATE_FORMAT = "yyyy-MM-dd";
        private const string ROLE_SESSION_NAME = "Elastic_Update";
        private const string AWS_ENV_CONFIG_EXCEPTION = "Unable to obtain one or more of the following AWS Components: Access Key, Secret Key, Role ARN, Region.  Please check your environment variables.";
        private const string AWS_UNKNOWN_REGION = "Unable to obtain AWS Region.  Please check the DISC_ELASTIC_UPDATE_AWS_REGION environment variable.";

        private static ConnectionSettings _connectionSettings;

        public static ConnectionSettings GetConnectionSettings(OpenSearchConnectionParameters cParams)
        {

            if (cParams.OpenSearchAwsParams?.UseAwsConnection ?? false)
            {
                string awsAccessKey = cParams.OpenSearchAwsParams.AccessKey;
                string awsSecretKey = cParams.OpenSearchAwsParams.SecretKey;
                string awsRoleArn = cParams.OpenSearchAwsParams.RoleArn;
                string strRegion = cParams.OpenSearchAwsParams.Region;

                if (new[] { awsAccessKey, awsSecretKey, awsRoleArn, strRegion }.Any(s => String.IsNullOrWhiteSpace(s)))
                {
                    throw new ConfigurationErrorsException(AWS_ENV_CONFIG_EXCEPTION);
                }

                AWSCredentials basicCredentials = new BasicAWSCredentials(accessKey: awsAccessKey, secretKey: awsSecretKey);
                AWSCredentials aWSAssumeRoleCredentials = new AssumeRoleAWSCredentials(basicCredentials, awsRoleArn, ROLE_SESSION_NAME);

                RegionEndpoint awsRegion = RegionEndpoint.GetBySystemName(strRegion);
                if (awsRegion.DisplayName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConfigurationErrorsException(AWS_UNKNOWN_REGION);
                }

                using (IConnection httpConnection = new AwsSigV4HttpConnection(aWSAssumeRoleCredentials, awsRegion))
                {
                    using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
                    {
                        _connectionSettings = new ConnectionSettings(pool, httpConnection,
                            (builtinJsonSerializerSettings, connectionSettingValues) =>
                                new JsonNetSerializer(
                                    builtinJsonSerializerSettings, connectionSettingValues,
                                    () => new JsonSerializerSettings
                                    {
                                        DateFormatString = DATE_FORMAT
                                    }
                                    )).DefaultIndex(cParams.IndexDatabase);
                    }
                }
            }
            else
            {
                using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
                {
                    _connectionSettings = new ConnectionSettings(pool,
                        (builtinJsonSerializerSettings, connectionSettingValues) =>
                            new JsonNetSerializer(
                                builtinJsonSerializerSettings, connectionSettingValues,
                                () => new JsonSerializerSettings
                                {
                                    DateFormatString = DATE_FORMAT
                                }
                                )).DefaultIndex(cParams.IndexDatabase);
                }
            }
            return _connectionSettings;

        }

        private sealed class Destructor
        {
            ~Destructor()
            {
                ((IDisposable)_connectionSettings)?.Dispose();
            }
        }
    }
}
